using VisionStudyDemo.Imaging;

int checks = 0;
void Equal<T>(IEnumerable<T> actual, params T[] expected)
{
    if (!actual.SequenceEqual(expected))
        throw new Exception($"Expected [{string.Join(",", expected)}], got [{string.Join(",", actual)}]");
    checks++;
}

byte[,] sample =
{
    { 220, 220, 220, 220, 0 },
    { 220, 30, 30, 30, 220 },
    { 220, 30, 30, 30, 220 },
    { 220, 220, 220, 220, 220 }
};
byte[,] small = { { 0, 30, 220 }, { 100, 155, 255 } };
Equal<byte>(GrayImageProcessor.Flatten(small), (byte)0, 30, 220, 100, 155, 255);
Equal<byte>(GrayImageProcessor.AdjustBrightness(small, 100), (byte)100, 130, 255, 200, 255, 255);
Equal<byte>(GrayImageProcessor.AdjustBrightness(small, -100), (byte)0, 0, 120, 0, 55, 155);
Equal<byte>(GrayImageProcessor.Invert(small), (byte)255, 225, 35, 155, 100, 0);
Equal<byte>(GrayImageProcessor.Flatten(small), (byte)0, 30, 220, 100, 155, 255); // 原图未修改
Equal<byte>(GrayImageProcessor.Threshold(new byte[,] { { 29, 30, 31 } }, 30), (byte)255, 0, 0);
byte[] binary = GrayImageProcessor.Threshold(sample, 100);
Equal(new[] { GrayImageProcessor.CountForeground(binary) }, 7);
Equal(GrayImageProcessor.GetFourConnectedAreas(binary, 5, 4), 1, 6);
Equal(GrayImageProcessor.GetFourConnectedAreas(binary, 5, 4), 1, 6); // 重复调用无残留状态
Equal(GrayImageProcessor.GetFourConnectedAreas(GrayImageProcessor.Threshold(sample, 30), 5, 4), 1);
Equal(GrayImageProcessor.GetFourConnectedAreas(new byte[] { 255, 0, 0, 255 }, 2, 2), 1, 1);
Equal(GrayImageProcessor.GetFourConnectedAreas(new byte[] { 255, 255, 255, 255 }, 2, 2), 4);
Equal(GrayImageProcessor.GetFourConnectedAreas(new byte[6], 3, 2), Array.Empty<int>());
Equal(GrayImageProcessor.GetFourConnectedAreas(new byte[] { 255, 0, 255 }, 1, 3), 1, 1);
var regions = GrayImageProcessor.GetFourConnectedRegions(binary, 5, 4);
Equal(regions.Select(region => region.Area), 1, 6);
Equal(regions[0].Pixels, (0, 4));
Equal(regions[1].Pixels.OrderBy(p => p.Row).ThenBy(p => p.Col),
    (1, 1), (1, 2), (1, 3), (2, 1), (2, 2), (2, 3));
Equal(new[] { regions.SelectMany(r => r.Pixels).Distinct().Count() }, 7);
byte[] filtered = GrayImageProcessor.FilterByArea(binary, 5, 4, 2);
Equal(new[] { GrayImageProcessor.CountForeground(filtered) }, 6);
Equal<byte>(filtered.Where((_, index) => index == 4), 0); // 右上角被去除
Equal(GrayImageProcessor.GetFourConnectedAreas(filtered, 5, 4), 6);
Equal<byte>(GrayImageProcessor.FilterByArea(binary, 5, 4, 1), binary);
Equal<byte>(GrayImageProcessor.FilterByArea(binary, 5, 4, 6), filtered); // 包含等于
Equal(new[] { GrayImageProcessor.CountForeground(GrayImageProcessor.FilterByArea(binary, 5, 4, 7)) }, 0);
Equal(new[] { GrayImageProcessor.CountForeground(binary) }, 7); // 筛选不修改输入
Equal(GrayImageProcessor.GetFourConnectedRegions(new byte[6], 3, 2).Select(r => r.Area), Array.Empty<int>());
Console.WriteLine($"PASS: {checks} algorithm checks.");
