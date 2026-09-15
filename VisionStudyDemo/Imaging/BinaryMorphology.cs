namespace VisionStudyDemo.Imaging;

public enum MorphologyOperation { None, Open, Close }

/// <summary>照片二值图的开/闭运算。输入只读，输出保持原尺寸。</summary>
public static class BinaryMorphology
{
    public static byte[,] Apply(byte[,] binary, MorphologyOperation operation, int size)
    {
        ArgumentNullException.ThrowIfNull(binary);
        if (size < 3 || size > 15 || size % 2 == 0)
            throw new ArgumentOutOfRangeException(nameof(size), "边长必须是 3～15 的奇数。");
        if (!Enum.IsDefined(operation)) throw new ArgumentOutOfRangeException(nameof(operation));
        if (operation == MorphologyOperation.None) return (byte[,])binary.Clone();
        int height = binary.GetLength(0), width = binary.GetLength(1);
        int radius = size / 2;
        // 先补一圈黑色工作区，让膨胀有向外扩展的空间。
        // 若每步直接裁掉 ROI 外的数据，闭运算可能错误地收缩贴边目标。
        byte[,] work = new byte[checked(height + radius * 2), checked(width + radius * 2)];
        for (int x=0; x<height; x++)
            for (int y=0; y<width; y++) work[x+radius,y+radius]=binary[x,y];

        // 连续 radius 次 3×3 操作，等效于一次 size×size 方形邻域操作。
        // 开：先腐蚀后膨胀；闭：先膨胀后腐蚀。第二阶段读取第一阶段结果。
        for (int i=0; i<radius; i++)
            work = operation == MorphologyOperation.Open
                ? GrayImageProcessor.Erode3x3(work) : GrayImageProcessor.Dilate3x3(work);
        for (int i=0; i<radius; i++)
            work = operation == MorphologyOperation.Open
                ? GrayImageProcessor.Dilate3x3(work) : GrayImageProcessor.Erode3x3(work);
        return GrayImageProcessor.Crop(work,radius,radius,width,height);
    }
}
