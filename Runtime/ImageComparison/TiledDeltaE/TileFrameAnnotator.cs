namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// Frames the pixels immediately outside every tile over the tiled threshold, so the triage
    /// question - one hot tile, or many scattered ones - can be read off the diff image instead of
    /// decoded from a list of coordinates. Solid magenta would look like real diff data, since the
    /// diff image is itself built from per-pixel colour differences; dashing and a colour outside the
    /// deltaE gradient keep the annotation from being mistaken for it.
    /// </summary>
    static class TileFrameAnnotator
    {
        internal static readonly Color32 FrameColor = new Color32(255, 0, 255, 255);

        /// <summary>
        /// Draws a dashed magenta frame around every tile scoring over <paramref name="threshold"/>.
        /// Never touches a pixel within a failing tile's own rectangle: the frame is drawn one pixel
        /// outside it, and an edge that would fall outside the image is left open rather than clamped
        /// inward, which would draw it over pixels the tile does not own.
        /// </summary>
        /// <param name="diffPixels">The diff image, indexed by <c>y * tileScores.ImageWidth + x</c>. Modified in place.</param>
        /// <param name="tileScores">The per-tile scores and geometry the tiled reduction produced.</param>
        /// <param name="threshold">The per-tile deltaE threshold; tiles scoring at or below it are left untouched.</param>
        internal static void Annotate(Color32[] diffPixels, TileScores tileScores, float threshold)
        {
            if (!tileScores.HasGrid)
            {
                return;
            }

            for (var tileIndex = 0; tileIndex < tileScores.Scores.Length; tileIndex++)
            {
                if (tileScores.Scores[tileIndex] <= threshold)
                {
                    continue;
                }

                var column = tileIndex % tileScores.TilesPerAxis;
                var row = tileIndex / tileScores.TilesPerAxis;
                var originX = Mathf.Min(column * tileScores.TileWidth, tileScores.ImageWidth - tileScores.TileWidth);
                var originY = Mathf.Min(row * tileScores.TileHeight, tileScores.ImageHeight - tileScores.TileHeight);

                DrawDashedTileFrame(diffPixels, tileScores.ImageWidth, tileScores.ImageHeight, originX, originY, tileScores.TileWidth, tileScores.TileHeight);
            }
        }

        // Draws just outside [x0, x0 + tileWidth) by [y0, y0 + tileHeight): never on a pixel the
        // rectangle itself covers, so the annotation can never obscure the evidence it points at.
        static void DrawDashedTileFrame(Color32[] pixels, int width, int height, int x0, int y0, int tileWidth, int tileHeight)
        {
            var left = x0 - 1;
            var right = x0 + tileWidth;
            var bottom = y0 - 1;
            var top = y0 + tileHeight;

            DrawDashedHorizontalLine(pixels, width, height, left, right, bottom);
            DrawDashedHorizontalLine(pixels, width, height, left, right, top);
            DrawDashedVerticalLine(pixels, width, height, bottom, top, left);
            DrawDashedVerticalLine(pixels, width, height, bottom, top, right);
        }

        static void DrawDashedHorizontalLine(Color32[] pixels, int width, int height, int xStart, int xEnd, int y)
        {
            if (y < 0 || y >= height)
            {
                return;
            }

            for (var x = xStart; x <= xEnd; x += 2)
            {
                if (x < 0 || x >= width)
                {
                    continue;
                }

                pixels[y * width + x] = FrameColor;
            }
        }

        static void DrawDashedVerticalLine(Color32[] pixels, int width, int height, int yStart, int yEnd, int x)
        {
            if (x < 0 || x >= width)
            {
                return;
            }

            for (var y = yStart; y <= yEnd; y += 2)
            {
                if (y < 0 || y >= height)
                {
                    continue;
                }

                pixels[y * width + x] = FrameColor;
            }
        }
    }
}
