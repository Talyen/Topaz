"""Bake static environment paintovers onto unique atlases for private Unity review."""
import json
import sys
from pathlib import Path
import numpy as np
from PIL import Image
from bake_projection import project, raster


def sample_palette(texture, uv):
    if texture is None:
        return np.tile([.48, .5, .45], (len(uv), 1))
    h, w = texture.shape[:2]
    x = np.clip((uv[:, 0] * w).astype(int), 0, w-1)
    y = np.clip(((1-uv[:, 1]) * h).astype(int), 0, h-1)
    return texture[y, x]


def sample_paint(paint, coordinates):
    # Preserve the native generated resolution and interpolate instead of baking large nearest-neighbor blocks.
    h,w=paint.shape[:2];xy=coordinates*np.array([w,h])-.5
    lo=np.floor(xy).astype(int);fraction=xy-lo
    x0=np.clip(lo[:,0],0,w-1);x1=np.clip(lo[:,0]+1,0,w-1)
    y0=np.clip(lo[:,1],0,h-1);y1=np.clip(lo[:,1]+1,0,h-1)
    a=paint[y0,x0]*(1-fraction[:,0,None])+paint[y0,x1]*fraction[:,0,None]
    b=paint[y1,x0]*(1-fraction[:,0,None])+paint[y1,x1]*fraction[:,0,None]
    return a*(1-fraction[:,1,None])+b*fraction[:,1,None]

def paint_mask(paint):
    return (np.ptp(paint, axis=-1) > .045) | (np.abs(paint.mean(-1)-.5) > .18)


def bake_asset(folder, paints, size=2048):
    folder = Path(folder)
    data = json.loads((folder/'source.json').read_text())
    views = data['views']; resolution = data['viewSize']
    triangles = []; originals = []; source_colors = []; textures = []; part_ids = []
    for n, part in enumerate(data['parts']):
        v = np.asarray(part['vertices']).reshape(-1, 3)
        t = np.asarray(part['triangles']).reshape(-1, 3)
        uv = np.asarray(part['uv']).reshape(-1, 2) if part['uv'] else np.zeros((len(v), 2))
        colors = np.asarray(part['colors']).reshape(-1, 3) if part.get('colors') else np.ones((len(v), 3))
        triangles.extend(v[t]); originals.extend(uv[t]); source_colors.extend(colors[t]); part_ids.extend([n]*len(t))
        path = part.get('texture'); textures.append(np.asarray(Image.open(path).convert('RGB'))/255 if path else None)
    triangles = np.asarray(triangles); originals = np.asarray(originals); source_colors = np.asarray(source_colors)
    if data['family'] in ['terrain', 'water']:
        atlas, _ = project(triangles, views[0]); atlas[:, :, 1] = 1-atlas[:, :, 1]
        (folder/'atlas-uv.json').write_text(json.dumps(dict(cornerUv=atlas.ravel().tolist(),triangles=len(triangles))))
    else:
        atlas = np.asarray(json.loads((folder/'atlas-uv.json').read_text())['cornerUv']).reshape(-1, 3, 2)
    if len(atlas) != len(triangles):
        raise ValueError('UV correspondence does not match source triangles')
    atlas = atlas.copy(); atlas[:, :, 1] = 1-atlas[:, :, 1]
    normal = np.cross(triangles[:, 1]-triangles[:, 0], triangles[:, 2]-triangles[:, 0])
    normal /= np.maximum(np.linalg.norm(normal,axis=1,keepdims=True), 1e-10)
    depths = []
    for view in views:
        uv, z = project(triangles, view); depth = np.full((resolution, resolution), np.inf)
        for triangle_uv, triangle_z in zip(uv, z):
            r = raster(triangle_uv, resolution)
            if r is not None:
                x, y, b = r; np.minimum.at(depth, (y, x), np.sum(b*triangle_z,axis=1))
        depths.append(depth)
    paint_bank = np.concatenate([paint[paint_mask(paint)] for paint in paints])
    if not len(paint_bank):
        raise ValueError('No generated surface paint found')
    # Hidden texels receive palette-matched generated paint, not untextured source colors.
    # This fill is approximate; report it separately from calibrated projection coverage.
    bank = paint_bank[::max(1,len(paint_bank)//4096)]
    color_lookup = {}
    def generated_fill(original, points):
        keys = np.clip((original*16).astype(int),0,16)
        output = np.zeros_like(original)
        for key in np.unique(keys,axis=0):
            k = tuple(key); chosen = np.all(keys==key,axis=1)
            if k not in color_lookup:
                distance = np.sum((bank-key/16)**2,axis=1)
                color_lookup[k] = bank[np.argsort(distance)[:24]]
            candidates = color_lookup[k]
            cell = np.floor(points[chosen]*5).astype(np.int64)
            hashes = np.mod(cell[:,0]*73856093 ^ cell[:,1]*19349663 ^ cell[:,2]*83492791,len(candidates))
            output[chosen] = candidates[hashes]
        return output
    output = np.zeros((size,size,3),np.float32); occupied = np.zeros((size,size),bool); covered = np.zeros_like(occupied)
    tolerance = max(.02, max(v['scale'] for v in views)*.006)
    for i,(t,uv,original,col,n) in enumerate(zip(triangles,atlas,originals,source_colors,part_ids)):
        r = raster(uv,size)
        if r is None: continue
        x,y,b = r
        if not len(x): continue
        points = np.einsum('ij,jk->ik',b,t)
        fallback = sample_palette(textures[n],np.einsum('ij,jk->ik',b,original))*np.einsum('ij,jk->ik',b,col)
        total = np.zeros(len(x)); painted = np.zeros((len(x),3))
        for k,view in enumerate(views):
            facing = max(0,np.sum(normal[i]*-np.asarray(view['forward'])))
            if facing < .08: continue
            projected,z = project(points,view)
            px=np.clip((projected[:,0]*resolution).astype(int),0,resolution-1);py=np.clip((projected[:,1]*resolution).astype(int),0,resolution-1)
            color = sample_paint(paints[k],projected)
            valid = np.all((projected>=0)&(projected<1),axis=1)&(np.abs(z-depths[k][py,px])<tolerance)&paint_mask(color)
            weight = valid*facing**4; total += weight; painted += color*weight[:,None]
        output[y,x]=generated_fill(fallback,points);occupied[y,x]=True
        valid=total>0;output[y[valid],x[valid]]=painted[valid]/total[valid,None];covered[y[valid],x[valid]]=True
    padded = occupied.copy()
    for _ in range(8):
        new = padded.copy()
        for shift in [(1,0),(-1,0),(0,1),(0,-1)]:
            neighbor = np.roll(padded,shift,(0,1));take=~new&neighbor
            output[take]=np.roll(output,shift,(0,1))[take];new |= take
        padded=new
    Image.fromarray(np.uint8(np.clip(output,0,1)*255)).save(folder/'baked-albedo.png')
    Image.fromarray(np.uint8(covered)*255).save(folder/'coverage.png')
    report=dict(rootName=data['rootName'],family=data['family'],triangles=len(triangles),atlasSize=size,
                occupiedTexels=int(occupied.sum()),projectedFraction=float(covered.sum()/occupied.sum()),
                generatedFillFraction=float((occupied&~covered).sum()/occupied.sum()),depthToleranceMeters=tolerance,
                note='Generated fill is approximate palette-matched surface paint; it is not a calibrated unseen-surface reconstruction.')
    (folder/'bake-report.json').write_text(json.dumps(report,indent=2));return report


def bake_all(directory):
    p=Path(directory);assets=json.loads((p/'assets.json').read_text());reports=[]
    for family in ['trees','stone','structures','terrain','water']:
        rows=[a for a in assets if a['family']==family];cols=3 if family in ['trees','stone','structures'] else 1
        sheet=np.asarray(Image.open(p/f'{family}-painted.png').convert('RGB'))/255
        height,width=sheet.shape[:2]
        for row,asset in enumerate(rows):
            paints=[sheet[round(row*height/len(rows)):round((row+1)*height/len(rows)),round(k*width/cols):round((k+1)*width/cols)] for k in range(asset['views'])]
            report=bake_asset(p/asset['id'],paints);reports.append(dict(id=asset['id'],**report))
            print(asset['id'],f"{report['projectedFraction']:.1%} calibrated coverage",flush=True)
    (p/'bake-summary.json').write_text(json.dumps(reports,indent=2))

if __name__=='__main__':
    bake_all(sys.argv[1])
