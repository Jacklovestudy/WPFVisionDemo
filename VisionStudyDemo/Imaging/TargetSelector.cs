namespace VisionStudyDemo.Imaging;

/// <summary>预期行列与连通域使用同一坐标系；距离单位为原图像素。</summary>
public record TargetCriteria(int MinArea, double MinRatio, double MaxRatio, double MinFill,
    double ExpectedRow, double ExpectedCol, double MaxDistance);

public record TargetSelection(ConnectedRegion? Region, int ShapeCount, int DistanceCount,
    double Ratio, double Fill, double Distance);

public static class TargetSelector
{
    /// <summary>先筛面积和形状，再筛距离，最后选最近的；同距离保留扫描先遇到的。</summary>
    public static TargetSelection Select(IEnumerable<ConnectedRegion> regions, TargetCriteria criteria)
    {
        ArgumentNullException.ThrowIfNull(regions);
        ArgumentNullException.ThrowIfNull(criteria);
        double[] numbers = { criteria.MinRatio, criteria.MaxRatio, criteria.MinFill,
            criteria.ExpectedRow, criteria.ExpectedCol, criteria.MaxDistance };
        if (numbers.Any(v => !double.IsFinite(v) || v < 0 || v > 1e9) ||
            criteria.MinArea < 1 || criteria.MinRatio <= 0 || criteria.MaxRatio < criteria.MinRatio || criteria.MinFill > 1)
            throw new ArgumentException("筛选参数无效。");

        ConnectedRegion? selected = null;
        int shapeCount = 0, distanceCount = 0;
        double bestSquared = double.PositiveInfinity, bestRatio = 0, bestFill = 0;
        foreach (var region in regions)
        {
            if (region.Area < criteria.MinArea) continue;
            int width = region.Width, height = region.Height;
            double ratio = (double)width / height;
            double fill = region.Area / ((double)width * height);
            if (ratio < criteria.MinRatio || ratio > criteria.MaxRatio || fill < criteria.MinFill)
                continue;
            shapeCount++;
            double rowDiff = region.CenterRow - criteria.ExpectedRow;
            double colDiff = region.CenterCol - criteria.ExpectedCol;
            double squared = rowDiff * rowDiff + colDiff * colDiff;
            // 只比较远近不必开方。等于最大距离也合格。
            if (squared > criteria.MaxDistance * criteria.MaxDistance) continue;
            distanceCount++;
            if (squared >= bestSquared) continue;
            selected = region;
            bestSquared = squared;
            bestRatio = ratio;
            bestFill = fill;
        }
        // 没有合格区域就返回 null，不重新选择不合格的最大区域。
        return new(selected, shapeCount, distanceCount, bestRatio, bestFill,
            selected == null ? 0 : Math.Sqrt(bestSquared));
    }
}
