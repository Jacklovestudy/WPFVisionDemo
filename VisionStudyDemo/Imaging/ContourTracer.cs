namespace VisionStudyDemo.Imaging;

/// <summary>
/// 沿像素方格的外侧追踪单个四连通区域，返回按顺序排列的网格顶点。
/// Row/Col 是像素边界坐标（可以等于图片高/宽），不是像素中心索引。
/// </summary>
public static class ContourTracer
{
    public static List<(int Row, int Col)> TraceOuterContour(ConnectedRegion region)
    {
        ArgumentNullException.ThrowIfNull(region);
        if (region.Area == 0) return new();
        var pixels = region.Pixels.ToHashSet();
        // 方向编号顺时针排列：右 0、下 1、左 2、上 3。
        // 每条边的白色像素位于行进方向的右手侧。
        var edges = new HashSet<(int Row, int Col, int Direction)>();
        foreach (var (r, c) in pixels)
        {
            if (!pixels.Contains((r-1,c))) edges.Add((r,c,0));
            if (!pixels.Contains((r,c+1))) edges.Add((r,c+1,1));
            if (!pixels.Contains((r+1,c))) edges.Add((r+1,c+1,2));
            if (!pixels.Contains((r,c-1))) edges.Add((r+1,c,3));
        }
        // 最上方、最左侧白点的上边一定在外边界，不会从孔洞开始。
        var first = pixels.OrderBy(p => p.Row).ThenBy(p => p.Col).First();
        var start = (Row: first.Row, Col: first.Col, Direction: 0);
        var current = start;
        var path = new List<(int Row, int Col)> { first };
        var visited = new HashSet<(int Row, int Col, int Direction)>();
        int[] dr = { 0,1,0,-1 }, dc = { 1,0,-1,0 };
        int[] turns = { 1,0,3,2 }; // 右转、直行、左转、回头，固定检查顺序。
        while (visited.Add(current))
        {
            int row = current.Row + dr[current.Direction];
            int col = current.Col + dc[current.Direction];
            path.Add((row,col));
            bool found = false;
            foreach (int turn in turns)
            {
                int direction = (current.Direction + turn) % 4;
                var next = (Row: row, Col: col, Direction: direction);
                if (!edges.Contains(next)) continue;
                // 比较位置和方向，不是见到任意访问过的点就结束。
                if (next == start) return path;
                current = next;
                found = true;
                break;
            }
            if (!found) break;
        }
        throw new InvalidOperationException("轮廓未能闭合，请检查输入是否为单个四连通区域。");
    }
}
