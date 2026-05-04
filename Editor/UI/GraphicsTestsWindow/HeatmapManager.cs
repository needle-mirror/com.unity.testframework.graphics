using System.IO;
using System.IO.Compression;
using UnityEngine;
using UnityEngine.TestTools.Graphics;

namespace UnityEditor.TestTools.Graphics.UI
{
    /// <summary>
    /// Abstracts file system access for testability.
    /// </summary>
    interface IFileSystem
    {
        bool FileExists(string path);
        Stream OpenRead(string path);
    }

    /// <summary>
    /// Default file system implementation using System.IO.
    /// </summary>
    class DefaultFileSystem : IFileSystem
    {
        public bool FileExists(string path) => File.Exists(path);

        public Stream OpenRead(string path) => new FileStream(path, FileMode.Open, FileAccess.Read);
    }

    /// <summary>
    /// Loads and generates heatmap textures from compressed per-pixel delta files.
    /// </summary>
    class HeatmapManager
    {
        static readonly string k_CacheFolder = Path.Combine("Library", "ReferenceImageDeltas");

        readonly IFileSystem m_FileSystem;

        internal HeatmapManager()
            : this(new DefaultFileSystem()) { }

        internal HeatmapManager(IFileSystem fileSystem)
        {
            m_FileSystem = fileSystem;
        }

        /// <summary>
        /// Loads a heatmap texture from a compressed delta file for the given test.
        /// Returns null if the delta file does not exist or contains invalid data.
        /// </summary>
        internal Texture2D LoadHeatmap(string testName, HeatmapColorScheme scheme = HeatmapColorScheme.BlueYellow)
        {
            var path = Path.Combine(k_CacheFolder, testName + ".delta.gz");
            if (!m_FileSystem.FileExists(path))
            {
                GraphicsTestLogger.DebugLog($"Heatmap delta file not found: {path}");
                return null;
            }

            try
            {
                using var fs = m_FileSystem.OpenRead(path);
                using var gzip = new GZipStream(fs, CompressionMode.Decompress);
                using var br = new BinaryReader(gzip);

                var width = br.ReadInt32();
                var height = br.ReadInt32();
                var pixelCount = width * height;

                if (width <= 0 || height <= 0)
                {
                    GraphicsTestLogger.DebugLog($"Invalid heatmap dimensions ({width}x{height}) in delta file for '{testName}'");
                    return null;
                }

                var deltas = new float[pixelCount];
                var maxDelta = 0f;
                for (var i = 0; i < pixelCount; i++)
                {
                    deltas[i] = br.ReadSingle();
                    if (deltas[i] > maxDelta)
                        maxDelta = deltas[i];
                }

                var heatmap = new Texture2D(width, height, TextureFormat.RGBA32, false, true)
                {
                    name = testName + "_Heatmap",
                };

                var pixels = new Color[pixelCount];
                for (var i = 0; i < pixelCount; i++)
                {
                    var normalized = maxDelta > 0f ? deltas[i] / maxDelta : 0f;
                    pixels[i] = GetHeatColor(normalized, scheme);
                }

                heatmap.SetPixels(pixels);
                heatmap.Apply();
                return heatmap;
            }
            catch (System.Exception e)
            {
                GraphicsTestLogger.Log(LogType.Warning, $"Failed to read heatmap delta file for '{testName}': {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Maps a normalized [0,1] delta value to a color for the given scheme.
        /// </summary>
        internal static Color GetHeatColor(float value, HeatmapColorScheme scheme)
        {
            return scheme switch
            {
                HeatmapColorScheme.GreenRed => Color.Lerp(Color.green, Color.red, value),
                HeatmapColorScheme.BlueYellow => Color.Lerp(Color.blue, Color.yellow, value),
                HeatmapColorScheme.Grayscale => new Color(value, value, value, 1f),
                _ => throw new System.ArgumentOutOfRangeException(
                    nameof(scheme),
                    scheme,
                    "Unknown heatmap color scheme"
                ),
            };
        }

        /// <summary>
        /// Creates a solid-color heatmap texture representing 0-delta.
        /// </summary>
        internal Texture2D EmptyTexture(int? width, int? height, HeatmapColorScheme scheme)
        {
            var w = width ?? 1;
            var h = height ?? 1;

            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            var baseColor = GetHeatColor(0, scheme);
            var pixels = new Color[w * h];
            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = baseColor;
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }
    }
}
