import sys,json,runpy
from pathlib import Path
root=Path(sys.argv[sys.argv.index('--')+1]);script=Path('scripts/visual-review/unwrap_projection.py').resolve()
for asset in json.loads((root/'assets.json').read_text()):
 if asset['family'] in ['terrain','water']:continue
 sys.argv=['blender','--',str(root/asset['id'])]
 print('UNWRAP',asset['id'],flush=True);runpy.run_path(str(script),run_name='__main__')
