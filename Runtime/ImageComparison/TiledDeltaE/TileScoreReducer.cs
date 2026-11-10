namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// Reduces an image's per-pixel deltaE excess over a grid of equally sized tiles, scoring the
    /// image by its worst tile. The whole-frame average divides by the entire pixel count, so a
    /// difference confined to a small area is diluted in proportion to how small it is; reducing
    /// per tile removes that dilution.
    /// </summary>
    static class TileScoreReducer
    {
        /// <summary>
        /// Computes the average excess within each tile.
        /// </summary>
        /// <param name="perPixelExcess">Per-pixel deltaE above the per-pixel threshold, indexed by
        /// <c>y * width + x</c>.</param>
        /// <param name="width">Image width in pixels.</param>
        /// <param name="height">Image height in pixels.</param>
        /// <param name="tilesPerAxis">Requested number of tiles along each axis. Clamped to at least
        /// one and to no more than the smaller image dimension, so that no tile is empty.</param>
        /// <returns>The per-tile scores, the highest of them, and its index.</returns>
        internal static TileScores Reduce(float[] perPixelExcess, int width, int height, int tilesPerAxis)
        {
            if (perPixelExcess == null || width <= 0 || height <= 0)
            {
                return TileScores.None;
            }

            // More tiles than pixels along an axis would leave tiles empty, and an empty tile has no
            // meaningful average.
            var axisCount = Mathf.Clamp(tilesPerAxis, 1, Mathf.Min(width, height));

            // Tiles are sized up rather than down, and the last tile in a row or column starts early
            // enough to keep that size. Letting the final tile be smaller would make it divide by
            // fewer pixels, scoring higher on identical noise and planting a hotspot on two edges of
            // every image.
            var tileWidth = Mathf.CeilToInt((float)width / axisCount);
            var tileHeight = Mathf.CeilToInt((float)height / axisCount);

            var scores = new float[axisCount * axisCount];
            var maxScore = float.NegativeInfinity;
            var worstTileIndex = 0;

            for (var tileY = 0; tileY < axisCount; tileY++)
            {
                var originY = Mathf.Min(tileY * tileHeight, height - tileHeight);

                for (var tileX = 0; tileX < axisCount; tileX++)
                {
                    var originX = Mathf.Min(tileX * tileWidth, width - tileWidth);

                    var sum = 0f;
                    for (var y = originY; y < originY + tileHeight; y++)
                    {
                        var rowStart = y * width;
                        for (var x = originX; x < originX + tileWidth; x++)
                        {
                            sum += perPixelExcess[rowStart + x];
                        }
                    }

                    var score = sum / (tileWidth * tileHeight);
                    var tileIndex = tileY * axisCount + tileX;
                    scores[tileIndex] = score;

                    if (score > maxScore)
                    {
                        maxScore = score;
                        worstTileIndex = tileIndex;
                    }
                }
            }

            return new TileScores
            {
                Scores = scores,
                MaxScore = maxScore,
                WorstTileIndex = worstTileIndex,
                TilesPerAxis = axisCount,
                TileWidth = tileWidth,
                TileHeight = tileHeight,
                ImageWidth = width,
                ImageHeight = height,
            };
        }
    }
}
