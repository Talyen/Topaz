"""Verify projection occlusion and source correspondence, not subjective art choices."""
import json
import tempfile
import unittest
from pathlib import Path
import numpy as np
from PIL import Image
from bake_environment import bake_asset

class EnvironmentBakeTests(unittest.TestCase):
    def make_source(self, folder, bad_atlas=False):
        verts=[-.5,-.5,0, .5,-.5,0, 0,.5,0, -.5,-.5,.4, .5,-.5,.4, 0,.5,.4]
        part=dict(name='mesh#0',vertices=verts,uv=[0,0]*6,triangles=[0,1,2,3,4,5],texture=None,colors=[])
        view=dict(center=[0,0,0],position=[0,0,3],right=[1,0,0],up=[0,1,0],forward=[0,0,-1],scale=1)
        (folder/'source.json').write_text(json.dumps(dict(parts=[part],views=[view],family='stone',rootName='occlusion control',viewSize=32)))
        uv=[.05,.05,.45,.05,.25,.9, .55,.05,.95,.05,.75,.9]
        if bad_atlas: uv=uv[:6]
        (folder/'atlas-uv.json').write_text(json.dumps(dict(cornerUv=uv)))
        return np.tile([.18,.45,.2],(32,32,1))

    def test_front_surface_blocks_projection_onto_hidden_surface(self):
        with tempfile.TemporaryDirectory() as directory:
            p=Path(directory);paint=self.make_source(p);report=bake_asset(p,[paint],size=64)
            coverage=np.asarray(Image.open(p/'coverage.png'))
            self.assertEqual(int(coverage[:,:32].sum()),0)
            self.assertGreater(int(coverage[:,32:].sum()),0)
            self.assertEqual(int(coverage[40,48]),255)
            # Conservative pixel-edge rejection may reduce the visible half slightly.
            self.assertGreater(report['projectedFraction'],.45)
            self.assertLessEqual(report['projectedFraction'],.5)
            # Hidden texels retain generated paint fill, while coverage truth remains zero.
            output=np.asarray(Image.open(p/'baked-albedo.png'))
            self.assertGreater(output[40,16,1],90)
            self.assertGreater(report['generatedFillFraction'],0)

    def test_changed_triangle_count_rejects_stale_atlas(self):
        with tempfile.TemporaryDirectory() as directory:
            p=Path(directory);paint=self.make_source(p,bad_atlas=True)
            with self.assertRaisesRegex(ValueError,'correspondence'):
                bake_asset(p,[paint],size=32)

if __name__=='__main__':
    unittest.main()
