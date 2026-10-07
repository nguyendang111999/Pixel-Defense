using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.TextCore.Text;

namespace PixelDefense.EditorTools
{
    /// <summary>
    /// Copies two freely licensed fonts that ship with Unity (Roboto Black, Apache-2.0, from com.unity.searcher;
    /// Inter SemiBold, OFL, from the Editor) into the project and creates dynamic SDF font assets with enough
    /// padding for chunky outlines.
    /// </summary>
    internal static class FontBaker
    {
        public const string DisplayAsset = AssetPaths.UiFonts + "/RobotoBlack.asset";
        public const string BodyAsset = AssetPaths.UiFonts + "/InterSemiBold.asset";
        private const string DisplayTtf = AssetPaths.UiFonts + "/Roboto-Black.ttf";
        private const string BodyTtf = AssetPaths.UiFonts + "/Inter-SemiBold.ttf";

        public static void BakeAll()
        {
            Directory.CreateDirectory(AssetPaths.UiFonts);
            var searcher = UnityEditor.PackageManager.PackageInfo.FindForAssetPath("Packages/com.unity.searcher/package.json");
            string roboto = searcher != null
                ? Path.Combine(searcher.resolvedPath, "Editor/Resources/FlatSkin/Font/Roboto-Black.ttf")
                : null;
            string inter = Path.Combine(EditorApplication.applicationContentsPath, "Resources/Fonts/Inter-SemiBold.ttf");

            CopyIfMissing(roboto, DisplayTtf);
            CopyIfMissing(inter, BodyTtf);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            CreateFontAsset(DisplayTtf, DisplayAsset);
            CreateFontAsset(BodyTtf, BodyAsset);
            AssetDatabase.SaveAssets();
        }

        private static void CopyIfMissing(string source, string destination)
        {
            if (File.Exists(destination))
            {
                return;
            }

            if (string.IsNullOrEmpty(source) || !File.Exists(source))
            {
                Debug.LogError("Font source not found: " + source);
                return;
            }
            File.Copy(source, destination);
        }

        private static void CreateFontAsset(string fontPath, string assetPath)
        {
            if (AssetDatabase.LoadAssetAtPath<FontAsset>(assetPath) != null)
            {
                return;
            }

            var font = AssetDatabase.LoadAssetAtPath<Font>(fontPath);
            if (font == null)
            {
                Debug.LogError("Font not imported: " + fontPath);
                return;
            }

            FontAsset asset = FontAsset.CreateFontAsset(font, 96, 14, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
            asset.name = Path.GetFileNameWithoutExtension(assetPath);
            AssetDatabase.CreateAsset(asset, assetPath);
            asset.atlasTextures[0].name = asset.name + " Atlas";
            AssetDatabase.AddObjectToAsset(asset.atlasTextures[0], asset);
            asset.material.name = asset.name + " Material";
            AssetDatabase.AddObjectToAsset(asset.material, asset);
            EditorUtility.SetDirty(asset);
        }
    }
}
