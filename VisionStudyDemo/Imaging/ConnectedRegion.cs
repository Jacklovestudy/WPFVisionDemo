namespace VisionStudyDemo.Imaging
{
    /// <summary>一个连通域：保存这一块的所有像素位置。</summary>
    public class ConnectedRegion
    {
        // Row 对应学习代码中的 x（行），Col 对应 y（列）。
        public List<(int Row, int Col)> Pixels { get; } = new();

        // 面积就是像素数量，无需再维护一个可能不一致的计数。
        public int Area => Pixels.Count;


        // 空区域没有位置，用 -1 表示
        public int MinRow => Area == 0 ? -1 : Pixels.Min(p => p.Row);
        public int MaxRow => Area == 0 ? -1 : Pixels.Max(p => p.Row);

        public int MinCol => Area == 0 ? -1 : Pixels.Min(p => p.Col);
        public int MaxCol => Area == 0 ? -1 : Pixels.Max(p => p.Col);

        // 包含首尾像素，因此加 1
        public int Width => Area == 0 ? 0 : MaxCol - MinCol + 1;
        public int Height => Area == 0 ? 0 : MaxRow - MinRow + 1;


        // 质心按像素索引计算，结果为 double
        public double CenterRow =>
            Area == 0 ? -1 : Pixels.Average(p => p.Row);

        public double CenterCol =>
            Area == 0 ? -1 : Pixels.Average(p => p.Col);
    }
}
