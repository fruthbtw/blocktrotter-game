using UnityEditor;
using UnityEngine;

namespace Blocktrotter.EditorTools
{
    /// <summary>
    /// Import rules for block textures in Assets/_Game/Art/Themes: readable on the CPU
    /// (BlockSkins samples them when painting block faces), uncompressed so they can
    /// be sampled, mip-mapped so a 512 px texture shrinks cleanly to ~128 px per cell,
    /// and capped at 512 px to keep memory small (~1 MB each).
    /// </summary>
    public sealed class ThemeArtImporter : AssetPostprocessor
    {
        public const string Folder = "Assets/_Game/Art/Themes/";

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Folder)) return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Default;
            importer.isReadable = true;
            importer.mipmapEnabled = true;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.maxTextureSize = 512;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaSource = TextureImporterAlphaSource.None;
        }
    }
}
