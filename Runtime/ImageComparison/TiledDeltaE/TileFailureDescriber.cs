using System;
using System.Collections.Generic;

namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// Describes the tiles that exceed a tiled deltaE threshold, for the failure message of any
    /// algorithm that scores tiles.
    /// </summary>
    static class TileFailureDescriber
    {
        const int k_MaxReportedTiles = 10;

        /// <summary>
        /// The failure text for a tiled deltaE comparison: the worst score, then up to ten tiles over
        /// <paramref name="threshold"/>, worst first, each with its column, row and pixel range. Tools
        /// parse this text, so every algorithm that scores tiles reports through this method.
        /// </summary>
        /// <remarks>
        /// Call only when <see cref="TileScores.MaxScore"/> exceeds <paramref name="threshold"/>. With no
        /// per-tile scores to list, the text is a single line naming only the worst score.
        /// </remarks>
        /// <param name="threshold">The per-tile deltaE threshold. Tiles scoring strictly above it are listed.</param>
        /// <param name="tileScores">The per-tile scores and geometry the tiled reduction produced.</param>
        internal static string Describe(float threshold, TileScores tileScores)
        {
            if (!tileScores.HasGrid)
            {
                return $"Max tile deltaE deviation: '{tileScores.MaxScore:G}'";
            }

            var failingTileIndices = new List<int>();
            for (var i = 0; i < tileScores.Scores.Length; i++)
            {
                if (tileScores.Scores[i] > threshold)
                {
                    failingTileIndices.Add(i);
                }
            }

            failingTileIndices.Sort((a, b) => tileScores.Scores[b].CompareTo(tileScores.Scores[a]));

            var lines = new List<string>
            {
                $"Max tile deltaE deviation: '{tileScores.MaxScore:G}' in {failingTileIndices.Count} of "
                    + $"{tileScores.Scores.Length} tiles over threshold '{threshold:G}':"
            };

            var shownCount = Math.Min(k_MaxReportedTiles, failingTileIndices.Count);
            for (var i = 0; i < shownCount; i++)
            {
                var tileIndex = failingTileIndices[i];
                var column = tileIndex % tileScores.TilesPerAxis;
                var row = tileIndex / tileScores.TilesPerAxis;
                var originX = Math.Min(column * tileScores.TileWidth, tileScores.ImageWidth - tileScores.TileWidth);
                var originY = Math.Min(row * tileScores.TileHeight, tileScores.ImageHeight - tileScores.TileHeight);

                lines.Add(
                    $"  tile ({column}, {row}) score {tileScores.Scores[tileIndex]:F6}, "
                        + $"pixels x {originX}-{originX + tileScores.TileWidth - 1}, "
                        + $"y {originY}-{originY + tileScores.TileHeight - 1}");
            }

            if (failingTileIndices.Count > k_MaxReportedTiles)
            {
                lines.Add($"  ... and {failingTileIndices.Count - k_MaxReportedTiles} more tiles over threshold");
            }

            return string.Join(Environment.NewLine, lines);
        }
    }
}
