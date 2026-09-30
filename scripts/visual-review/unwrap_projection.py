import bpy,bmesh,json,sys,math
from pathlib import Path
p=Path(sys.argv[sys.argv.index('--')+1]);data=json.loads((p/'source.json').read_text())
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
verts=[];faces=[]
for part in data['parts']:
 offset=len(verts);a=part['vertices'];verts.extend(zip(a[0::3],a[1::3],a[2::3]));t=part['triangles'];faces.extend(tuple(offset+x for x in t[i:i+3]) for i in range(0,len(t),3))
m=bpy.data.meshes.new('Projection atlas');m.from_pydata(verts,[],faces);m.update();bm=bmesh.new();bm.from_mesh(m);bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=.000001);bm.to_mesh(m);bm.free();m.update();o=bpy.data.objects.new('Projection atlas',m);bpy.context.collection.objects.link(o);bpy.context.view_layer.objects.active=o;o.select_set(True)
bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project(angle_limit=math.radians(66),island_margin=.001,area_weight=.5);bpy.ops.object.mode_set(mode='OBJECT')
uv=m.uv_layers.active.data
from mathutils.kdtree import KDTree
kd=KDTree(len(m.vertices))
for v in m.vertices:kd.insert(v.co,v.index)
kd.balance();mapped=[kd.find(v)[1] for v in verts]
lookup={}
for f in m.polygons:
 ids=[m.loops[idx].vertex_index for idx in f.loop_indices]
 lookup[tuple(sorted(ids))]={m.loops[idx].vertex_index:tuple(uv[idx].uv) for idx in f.loop_indices}
corners=[]
for f in faces:
 ids=[mapped[idx] for idx in f];mapping=lookup[tuple(sorted(ids))]
 corners.extend(float(x) for c in ids for x in mapping[c])
(p/'atlas-uv.json').write_text(json.dumps({'cornerUv':corners,'triangles':len(faces),'vertices':len(verts)}))
print('Unique atlas:',len(faces),'triangles;',len(corners)//2,'corners')
