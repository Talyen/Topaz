using System;
using System.Collections.Generic;
using System.IO;
using Topaz.LoopStudy;
using UnityEditor;
using UnityEngine;

namespace Topaz.Editor
{
    /// <summary>Small original journal pictograms; armor art stays independent of body models.</summary>
    public static class EquipmentIconSetup
    {
        public const string Folder = "Assets/Topaz/UI/Art/EquipmentIcons";
        const int Size = 96;
        static readonly Color Ink = new Color32(201, 144, 86, 255);

        public static void Generate()
        {
            if (!AssetDatabase.IsValidFolder(Folder))
                AssetDatabase.CreateFolder("Assets/Topaz/UI/Art", "EquipmentIcons");
            foreach (EquipmentSlot slot in EquipmentState.Slots)
            {
                Texture2D canvas = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
                var clear = new Color[Size * Size];
                canvas.SetPixels(clear);
                foreach (Vector2[] polygon in Shapes(slot)) Fill(canvas, polygon);
                canvas.Apply();
                string path = Path(slot);
                try { File.WriteAllBytes(path, canvas.EncodeToPNG()); }
                finally { UnityEngine.Object.DestroyImmediate(canvas); }
                AssetDatabase.ImportAsset(path);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }
        }

        public static string Path(EquipmentSlot slot) => Folder + "/" +
            slot.ToString().ToLowerInvariant() + ".png";

        static IEnumerable<Vector2[]> Shapes(EquipmentSlot slot)
        {
            switch (slot)
            {
                case EquipmentSlot.Weapon:
                    yield return Poly(46, 15, 50, 15, 55, 63, 41, 63);
                    yield return Poly(28, 61, 68, 61, 66, 69, 30, 69);
                    yield return Poly(45, 67, 51, 67, 51, 82, 45, 82);
                    yield return Poly(39, 81, 57, 81, 57, 87, 39, 87);
                    break;
                case EquipmentSlot.Tool:
                    yield return Poly(57, 15, 64, 18, 38, 83, 31, 80);
                    yield return Poly(34, 21, 61, 15, 74, 22, 61, 42, 43, 40);
                    break;
                case EquipmentSlot.Offhand:
                    yield return Poly(48, 13, 77, 25, 72, 62, 48, 85, 24, 62, 19, 25);
                    yield return Poly(48, 24, 65, 31, 62, 57, 48, 72, 34, 57, 31, 31);
                    break;
                case EquipmentSlot.Head:
                    yield return Poly(18, 55, 23, 34, 35, 22, 61, 22, 73, 34, 78, 55);
                    yield return Poly(17, 54, 79, 54, 77, 68, 19, 68);
                    yield return Poly(20, 69, 34, 69, 30, 79, 22, 77);
                    yield return Poly(62, 69, 76, 69, 74, 77, 66, 79);
                    break;
                case EquipmentSlot.Body:
                    yield return Poly(29, 18, 42, 15, 48, 25, 54, 15, 67, 18,
                        78, 38, 68, 44, 64, 37, 67, 83, 29, 83, 32, 37, 28, 44, 18, 38);
                    break;
                case EquipmentSlot.Hands:
                    yield return Poly(26, 22, 37, 22, 39, 48, 44, 46, 43, 19,
                        53, 19, 54, 47, 59, 47, 61, 26, 70, 28, 68, 64,
                        58, 81, 36, 81, 25, 64);
                    break;
                case EquipmentSlot.Boots:
                    yield return Poly(29, 17, 61, 17, 59, 61, 76, 68, 80, 80,
                        71, 84, 21, 84, 18, 71, 28, 59);
                    break;
            }
        }

        static Vector2[] Poly(params int[] xy)
        {
            var points = new Vector2[xy.Length / 2];
            for (int i = 0; i < points.Length; i++)
                points[i] = new Vector2(xy[i * 2], xy[i * 2 + 1]);
            return points;
        }

        static void Fill(Texture2D image, Vector2[] points)
        {
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                {
                    bool inside = false;
                    for (int i = 0, j = points.Length - 1; i < points.Length; j = i++)
                    {
                        Vector2 a = points[i];
                        Vector2 b = points[j];
                        if ((a.y > y) != (b.y > y) &&
                            x < (b.x - a.x) * (y - a.y) / (b.y - a.y) + a.x)
                            inside = !inside;
                    }
                    if (inside) image.SetPixel(x, y, Ink);
                }
        }
    }
}
