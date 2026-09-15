using VisionStudyDemo.Imaging;

int checks = 0;
ConnectedRegion RectangleRegion(int row, int col, int width, int height)
{
    var region = new ConnectedRegion();
    for (int r = row; r < row + height; r++)
        for (int c = col; c < col + width; c++) region.Pixels.Add((r,c));
    return region;
}
var nearTarget = RectangleRegion(2,2,2,4);
var largeWrong = RectangleRegion(0,10,8,2);
var farTarget = RectangleRegion(20,2,2,4);
var filter = new TargetCriteria(8,0.4,0.7,1,3.5,2.5,10);
var selection = TargetSelector.Select(new[] { largeWrong,farTarget,nearTarget }, filter);
if (!ReferenceEquals(selection.Region, nearTarget) || selection.ShapeCount != 2 || selection.DistanceCount != 1)
    throw new Exception("Selection must filter before choosing nearest");
checks++;
if (TargetSelector.Select(new[] { largeWrong,farTarget },filter).Region != null)
    throw new Exception("No match must not fall back to largest");
checks++;
if (TargetSelector.Select(new[] { nearTarget }, filter with { ExpectedRow = 13.5 }).Region == null)
    throw new Exception("Distance equal to maximum must pass");
checks++;
void Equal<T>(IEnumerable<T> actual, params T[] expected)
{
    if (!actual.SequenceEqual(expected))
        throw new Exception($"Expected [{string.Join(",", expected)}], got [{string.Join(",", actual)}]");
    checks++;
}

var singleRegion = new ConnectedRegion();
singleRegion.Pixels.Add((2,3));
Equal(ContourTracer.TraceOuterContour(singleRegion), (2,3),(2,4),(3,4),(3,3),(2,3));
var ringRegion = new ConnectedRegion();
for (int r = 0; r < 3; r++)
    for (int c = 0; c < 3; c++)
        if (r != 1 || c != 1) ringRegion.Pixels.Add((r,c));
var ringContour = ContourTracer.TraceOuterContour(ringRegion);
Equal(new[] { ringContour.Count, ringContour.Distinct().Count() }, 13,12);
Equal(new[] { ringContour[0], ringContour[^1] }, (0,0),(0,0));
var concave = new ConnectedRegion();
concave.Pixels.AddRange(new[] { (0,0),(1,0),(1,1) });
Equal(new[] { ContourTracer.TraceOuterContour(concave).Count }, 9);
Equal(ContourTracer.TraceOuterContour(new ConnectedRegion()), Array.Empty<(int,int)>());

// 右/下差分：两个方向、阈值相等、单像素和末行末列都要正确。
Equal<byte>(GrayImageProcessor.Flatten(GrayImageProcessor.DetectEdges(new byte[,] { { 100,110 }, { 200,200 } }, 50)), 255,255,0,0);
Equal<byte>(GrayImageProcessor.Flatten(GrayImageProcessor.DetectEdges(new byte[,] { { 200,40,90 } }, 50)), 255,0,0);
Equal<byte>(GrayImageProcessor.Flatten(GrayImageProcessor.DetectEdges(new byte[,] { { 200 }, { 40 }, { 90 } }, 50)), 255,0,0);
Equal<byte>(GrayImageProcessor.Flatten(GrayImageProcessor.DetectEdges(new byte[,] { { 255 } }, 50)), 0);

// 均值滤波：中心除以 9、边缘除以 6、角落除以 4；输入不能被覆盖。
byte[,] dot = new byte[5,5];
dot[2,2] = 255;
var expanded = GrayImageProcessor.Dilate3x3(dot);
Equal(new[] { GrayImageProcessor.CountForeground(GrayImageProcessor.Flatten(expanded)) }, 9);
Equal<byte>(GrayImageProcessor.Flatten(GrayImageProcessor.Erode3x3(expanded)), GrayImageProcessor.Flatten(dot));
Equal(new[] { GrayImageProcessor.CountForeground(GrayImageProcessor.Flatten(dot)) }, 1);
Equal<byte>(GrayImageProcessor.Flatten(GrayImageProcessor.Erode3x3(new byte[,] { { 255 } })), 0);
Equal<byte>(GrayImageProcessor.Flatten(GrayImageProcessor.Dilate3x3(new byte[,] { { 0 } })), 0);
byte[,] impulse = { { 10, 10, 10 }, { 10, 100, 10 }, { 10, 10, 10 } };
// 中值：孤立亮点消失，边界偶数个取中间两值平均，不修改原数组。
Equal<byte>(GrayImageProcessor.Flatten(GrayImageProcessor.MedianFilter3x3(impulse)), 10,10,10,10,10,10,10,10,10);
Equal<byte>(GrayImageProcessor.Flatten(GrayImageProcessor.MedianFilter3x3(new byte[,] { { 10,20 }, { 40,50 } })), 30,30,30,30);
Equal<byte>(GrayImageProcessor.Flatten(GrayImageProcessor.MedianFilter3x3(new byte[,] { { 10,20,30 }, { 40,50,60 }, { 70,80,90 } })), 30,35,40,45,50,55,60,65,70);
Equal<byte>(GrayImageProcessor.Flatten(GrayImageProcessor.MedianFilter3x3(new byte[,] { { 255 } })), 255);
Equal<byte>(GrayImageProcessor.Flatten(GrayImageProcessor.MedianFilter3x3(new byte[,] { { 0,31,90 } })), 15,31,60);
Equal<byte>(GrayImageProcessor.Flatten(GrayImageProcessor.MedianFilter3x3(new byte[,] { { 100,100,100 }, { 100,0,100 }, { 100,100,100 } })), 100,100,100,100,100,100,100,100,100);
Equal<byte>(GrayImageProcessor.Flatten(GrayImageProcessor.MeanFilter3x3(impulse)),
    32, 25, 32, 25, 20, 25, 32, 25, 32);
Equal<byte>(GrayImageProcessor.Flatten(impulse), 10, 10, 10, 10, 100, 10, 10, 10, 10);
Equal<byte>(GrayImageProcessor.Flatten(GrayImageProcessor.MeanFilter3x3(new byte[,] { { 30 } })), 30);
Equal<byte>(GrayImageProcessor.Flatten(GrayImageProcessor.MeanFilter3x3(new byte[,] { { 0, 30, 90 } })), 15, 40, 60);

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
byte[,] morphSample = new byte[11,11];
for (int r=3;r<=7;r++) for(int c=3;c<=7;c++) morphSample[r,c]=255;
morphSample[1,1]=255;
var opened = BinaryMorphology.Apply(morphSample, MorphologyOperation.Open, 3);
Equal(new[] { GrayImageProcessor.CountForeground(GrayImageProcessor.Flatten(opened)) },25);
morphSample[1,1]=0; morphSample[5,5]=0;
var closed = BinaryMorphology.Apply(morphSample, MorphologyOperation.Close, 3);
Equal(new[] { GrayImageProcessor.CountForeground(GrayImageProcessor.Flatten(closed)) },25);
Equal(new[] { (int)morphSample[5,5] },0); // 输入不变。
Equal<byte>(GrayImageProcessor.Flatten(BinaryMorphology.Apply(new byte[,] {{255}}, MorphologyOperation.Close, 3)),255); // 边界补黑扩展，闭运算不会截掉原边缘。
Equal<byte>(GrayImageProcessor.Flatten(BinaryMorphology.Apply(new byte[,] {{255}}, MorphologyOperation.Open, 3)),0);
Equal(new[] { GrayImageProcessor.CountForeground(GrayImageProcessor.Flatten(BinaryMorphology.Apply(closed, MorphologyOperation.Open, 5))) },25);
bool invalidSize=false;
try { BinaryMorphology.Apply(closed,MorphologyOperation.Close,4); } catch(ArgumentOutOfRangeException) { invalidSize=true; }
Equal(new[] { invalidSize },true);
Console.WriteLine($"PASS: {checks} algorithm checks.");
