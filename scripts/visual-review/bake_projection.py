"""Calibrated, visibility-gated multi-view albedo bake. No AI image editing here."""
import json,sys
from pathlib import Path
import numpy as np
from PIL import Image

def raster(tri,size):
    q=np.asarray(tri,float)*size-.5
    lo=np.maximum(np.floor(q.min(0)).astype(int),0);hi=np.minimum(np.ceil(q.max(0)).astype(int),size-1)
    if np.any(hi<lo):return None
    x,y=np.meshgrid(np.arange(lo[0],hi[0]+1),np.arange(lo[1],hi[1]+1));x=x.ravel();y=y.ravel()
    a,b,c=q;den=(b[1]-c[1])*(a[0]-c[0])+(c[0]-b[0])*(a[1]-c[1])
    if abs(den)<1e-10:return None
    u=((b[1]-c[1])*(x-c[0])+(c[0]-b[0])*(y-c[1]))/den;v=((c[1]-a[1])*(x-c[0])+(a[0]-c[0])*(y-c[1]))/den;w=1-u-v
    valid=(u>=-1e-6)&(v>=-1e-6)&(w>=-1e-6)
    return x[valid],y[valid],np.stack([u[valid],v[valid],w[valid]],axis=1)

def project(points,view):
    p=points-np.asarray(view['center']);scale=view['scale']*2
    uv=np.stack([np.sum(p*np.asarray(view['right']),axis=-1)/scale+.5,.5-np.sum(p*np.asarray(view['up']),axis=-1)/scale],axis=-1)
    return uv,np.sum((points-np.asarray(view['position']))*np.asarray(view['forward']),axis=-1)

def bake(directory,size=2048):
    p=Path(directory);data=json.loads((p/'source.json').read_text());atlas=np.array(json.loads((p/'atlas-uv.json').read_text())['cornerUv']).reshape(-1,3,2);atlas[:,:,1]=1-atlas[:,:,1]
    triangles=[];source_uv=[];textures=[];texture_ids=[]
    for n,part in enumerate(data['parts']):
        verts=np.array(part['vertices']).reshape(-1,3);t=np.array(part['triangles']).reshape(-1,3);uv=np.array(part['uv']).reshape(-1,2)
        triangles.extend(verts[t]);source_uv.extend(uv[t]);texture_ids.extend([n]*len(t));textures.append(np.array(Image.open(part['texture']).convert('RGB'))/255.)
    triangles=np.array(triangles);source_uv=np.array(source_uv);
    if len(atlas)!=len(triangles):raise ValueError('Atlas triangle count differs from the source mesh.')
    if not np.isfinite(triangles).all() or not np.isfinite(atlas).all():raise ValueError('Non-finite source geometry or UVs.')
    norm=np.cross(triangles[:,1]-triangles[:,0],triangles[:,2]-triangles[:,0]);norm/=np.maximum(np.linalg.norm(norm,axis=1,keepdims=True),1e-8)
    sheet=np.array(Image.open(p/'painted-six-views.png').convert('RGB').resize((1536,1024)))/255.;views=data['views'];depths=[];paints=[];posed_triangles=None
    if (p/'posed-source.json').exists() and (p/'painted-hidden-pose.png').exists():
        posed=json.loads((p/'posed-source.json').read_text())
        expected=[(q['name'],len(q['vertices']),q['triangles']) for q in data['parts']]
        actual=[(q['name'],len(q['vertices']),q['triangles']) for q in posed['parts']]
        if actual!=expected:raise ValueError('Posed source topology/order differs from the rest source; correspondence is unsafe.')
        posed_triangles=np.concatenate([np.asarray(q['vertices']).reshape(-1,3)[np.asarray(q['triangles']).reshape(-1,3)] for q in posed['parts']]);views=views+posed['views']
        posed_sheet=np.asarray(Image.open(p/'painted-hidden-pose.png').convert('RGB').resize((1024,1024)))/255.
    view_triangles=[triangles if k<6 else posed_triangles for k in range(len(views))]
    def with_occluders(surface_triangles, definition):
        hidden=[np.asarray(q['vertices']).reshape(-1,3)[np.asarray(q['triangles']).reshape(-1,3)] for q in definition.get('occluders',[])]
        return np.concatenate([surface_triangles]+hidden) if hidden else surface_triangles
    depth_triangles=[with_occluders(vt,data if k<6 else posed) for k,vt in enumerate(view_triangles)]
    view_normals=[]
    for vt in view_triangles:
        vn=np.cross(vt[:,1]-vt[:,0],vt[:,2]-vt[:,0]);vn/=np.maximum(np.linalg.norm(vn,axis=1,keepdims=True),1e-8);view_normals.append(vn)
    for k,v in enumerate(views):
        depth=np.full((512,512),np.inf)
        paint=sheet[k//3*512:(k//3+1)*512,k%3*512:(k%3+1)*512] if k<6 else posed_sheet[(k-6)//2*512:((k-6)//2+1)*512,(k-6)%2*512:((k-6)%2+1)*512]
        paints.append(paint);projected,z=project(depth_triangles[k],v)
        for t,uv,d in zip(depth_triangles[k],projected,z):
            r=raster(uv,512)
            if r is None:continue
            x,y,b=r;np.minimum.at(depth,(y,x),np.sum(b*d,axis=1))
        depths.append(depth)
    result=np.zeros((size,size,3),np.float32);mask=np.zeros((size,size),bool);covered=np.zeros_like(mask);front=np.zeros_like(mask);rest=np.zeros_like(mask);view_counts=[0]*len(views);pose_added=0;island_count=0
    for j,(t,uv,original,texid) in enumerate(zip(triangles,atlas,source_uv,texture_ids)):
        r=raster(uv,size)
        if r is None:continue
        x,y,b=r
        if len(x)==0:continue
        mask[y,x]=True;points=np.einsum("ij,jk->ik",b,t);original_uv=np.einsum("ij,jk->ik",b,original);tex=textures[texid];tx=np.clip((original_uv[:,0]*tex.shape[1]).astype(int),0,tex.shape[1]-1);ty=np.clip(((1-original_uv[:,1])*tex.shape[0]).astype(int),0,tex.shape[0]-1);fallback=tex[ty,tx]
        weighted=np.zeros((len(x),3));total=np.zeros(len(x))
        for k,v in enumerate(views):
            if k==6:rest[y[total>0],x[total>0]]=True
            facing=max(0,float(np.sum(view_normals[k][j]*-np.asarray(v['forward']))))
            if facing<.12:continue
            projection_points=points if k<6 else np.einsum("ij,jk->ik",b,posed_triangles[j]);puv,pz=project(projection_points,v);px=np.clip((puv[:,0]*512).astype(int),0,511);py=np.clip((puv[:,1]*512).astype(int),0,511)
            valid=(puv[:,0]>=0)&(puv[:,0]<1)&(puv[:,1]>=0)&(puv[:,1]<1)&(np.abs(pz-depths[k][py,px])<.024)
            color=paints[k][py,px];sat=color.max(1)-color.min(1);valid&=(sat>.055)|(np.abs(color.mean(1)-.5)>.14)
            if k>=6:valid&=total==0;pose_added+=int(valid.sum())
            weight=valid*facing**4;weighted+=color*weight[:,None];total+=weight;view_counts[k]+=int(valid.sum())
            if k==0:front[y[valid],x[valid]]=True
        valid=total>0;covered[y[valid],x[valid]]=True;result[y,x]=fallback;result[y[valid],x[valid]]=weighted[valid]/total[valid,None]
    # Eight pixels of island dilation for mip/filter safety, never paint over other islands.
    padded=mask.copy()
    for _ in range(8):
        new=padded.copy()
        for dy,dx in [(1,0),(-1,0),(0,1),(0,-1)]:
            neighbor=np.roll(padded,(dy,dx),(0,1));take=~new&neighbor;result[take]=np.roll(result,(dy,dx),(0,1))[take];new|=take
        padded=new
    Image.fromarray(np.uint8(np.clip(result,0,1)*255)).save(p/'baked-albedo.png');Image.fromarray(np.uint8(covered)*255).save(p/'coverage.png')
    report={'atlasSize':size,'parts':len(data['parts']),'sourceVertices':sum(len(q['vertices'])//3 for q in data['parts']),'triangles':len(triangles),'uniqueCorners':len(triangles)*3,'occupiedTexels':int(mask.sum()),'atlasOccupancyFraction':float(mask.sum()/(size*size)),'projectedTexelFraction':float(covered.sum()/mask.sum()),'frontOnlyTexelFraction':float(front.sum()/mask.sum()),'restPoseTexelFraction':float(rest.sum()/mask.sum()) if len(views)>6 else float(covered.sum()/mask.sum()),'fallbackTexelFraction':float((mask&~covered).sum()/mask.sum()),'viewContributions':view_counts,'poseFilledTexels':pose_added,'poseRule':'Posed views fill previously uncovered texels only; preserve primary rest-pose design','visibilityDepthToleranceMeters':.024,'dilationPixels':8,'note':'Texture-space coverage of this atlas, not proof of every anatomically hidden surface or animation quality.'}
    (p/'bake-report.json').write_text(json.dumps(report,indent=2));print(json.dumps(report,indent=2))
if __name__=='__main__':bake(sys.argv[1])
