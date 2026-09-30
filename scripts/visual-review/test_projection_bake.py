"""Focused calibration checks; visual quality is reviewed in Unity."""
import unittest
import numpy as np
from bake_projection import project, raster

class CalibrationTests(unittest.TestCase):
    def setUp(self):
        self.view = dict(center=[0, 1, 0], position=[0, 1, 6],
                         right=[-1, 0, 0], up=[0, 1, 0], forward=[0, 0, -1], scale=2)

    def test_camera_center_and_metric_depth(self):
        uv, depth = project(np.array([[0., 1., 0.]]), self.view)
        np.testing.assert_allclose(uv, [[.5, .5]])
        np.testing.assert_allclose(depth, [6])

    def test_image_origin_and_camera_basis(self):
        uv, _ = project(np.array([[1., 2., 0.], [-1., 0., 0.]]), self.view)
        np.testing.assert_allclose(uv, [[.25, .25], [.75, .75]])

    def test_visibility_depth_separates_front_and_back(self):
        _, depth = project(np.array([[0., 1., 1.], [0., 1., -1.]]), self.view)
        self.assertGreater(abs(depth[0] - depth[1]), .024)

    def test_barycentrics_reconstruct_interior_pixels(self):
        tri = np.array([[.1, .1], [.9, .1], [.1, .9]])
        x, y, weights = raster(tri, 32)
        reconstructed = np.einsum('ij,jk->ik', weights, tri)
        np.testing.assert_allclose(reconstructed, np.stack([(x+.5)/32, (y+.5)/32], axis=1))
        self.assertTrue(np.all(weights >= -1e-6))
        np.testing.assert_allclose(weights.sum(1), 1)

    def test_winding_does_not_change_raster_coverage(self):
        tri = [[.1, .1], [.8, .2], [.4, .9]]
        a = raster(tri, 32); b = raster(tri[::-1], 32)
        self.assertEqual(set(zip(a[0], a[1])), set(zip(b[0], b[1])))

    def test_degenerate_and_off_image_triangles_are_skipped(self):
        self.assertIsNone(raster([[.1, .1]] * 3, 32))
        self.assertIsNone(raster([[2, 2], [3, 2], [2, 3]], 32))

if __name__ == '__main__':
    unittest.main()
