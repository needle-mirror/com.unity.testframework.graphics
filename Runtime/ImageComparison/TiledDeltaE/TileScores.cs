namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// The result of reducing an image's per-pixel deltaE excess over a grid of tiles.
    /// </summary>
    readonly struct TileScores
    {
        /// <summary>
        /// The average deltaE excess within each tile, in row-major order from the bottom-left tile.
        /// The grid is square, so the row length is the square root of this array's length. Empty
        /// when no tiled reduction was performed.
        /// </summary>
        public float[] Scores { get; init; }

        /// <summary>
        /// The highest value in <see cref="Scores"/>, or zero when no tiled reduction was performed.
        /// </summary>
        public float MaxScore { get; init; }

        /// <summary>
        /// The index into <see cref="Scores"/> of the highest-scoring tile, or -1 when no tiled
        /// reduction was performed.
        /// </summary>
        public int WorstTileIndex { get; init; }

        /// <summary>
        /// Whether there is a grid to read: per-tile scores and a tile count. False for
        /// <see cref="None"/>, and for a score handed over without the grid behind it.
        /// </summary>
        public bool HasGrid => Scores != null && Scores.Length > 0 && TilesPerAxis > 0;

        /// <summary>
        /// The highest-scoring tile as an x-y coordinate counted from the bottom-left tile, decoded
        /// from <see cref="WorstTileIndex"/>. (-1, -1) when there is no grid to place a tile in; see
        /// <see cref="HasGrid"/>.
        /// </summary>
        public Vector2Int WorstTile =>
            HasGrid && WorstTileIndex >= 0
                ? new Vector2Int(WorstTileIndex % TilesPerAxis, WorstTileIndex / TilesPerAxis)
                : new Vector2Int(-1, -1);

        /// <summary>
        /// The number of tiles along each axis, already clamped to the image resolution. Zero when
        /// no tiled reduction was performed.
        /// </summary>
        public int TilesPerAxis { get; init; }

        /// <summary>
        /// The width in pixels of every tile. Zero when no tiled reduction was performed.
        /// </summary>
        public int TileWidth { get; init; }

        /// <summary>
        /// The height in pixels of every tile. Zero when no tiled reduction was performed.
        /// </summary>
        public int TileHeight { get; init; }

        /// <summary>
        /// The width in pixels of the image the reduction ran over. Needed alongside
        /// <see cref="TileWidth"/> to locate the last column of tiles, which is pulled back to keep
        /// full size rather than shrinking. Zero when no tiled reduction was performed.
        /// </summary>
        public int ImageWidth { get; init; }

        /// <summary>
        /// The height in pixels of the image the reduction ran over. Needed alongside
        /// <see cref="TileHeight"/> to locate the last row of tiles, which is pulled back to keep
        /// full size rather than shrinking. Zero when no tiled reduction was performed.
        /// </summary>
        public int ImageHeight { get; init; }

        /// <summary>
        /// The value used when the tiled reduction did not run.
        /// </summary>
        public static TileScores None => new() { Scores = System.Array.Empty<float>(), MaxScore = 0f, WorstTileIndex = -1 };
    }
}
