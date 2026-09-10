# 实验代码阅读顺序

## 先看 ViewModel 中的实验入口

打开 `VisionStudyDemo/ViewModels/MainWindowViewModel.cs`，从 `TestAsync` 开始。

它先调用 `CreateLessonImage` 复制原图，并把实验图右上角设为黑色，再调用一个实验方法。
默认是 `ShowConnectedComponents(gray, 100)`，点击测试按钮显示全部连通域的位置、尺寸和面积，并用红框标出面积最大的一块。也可切换为 `ShowAreaFiltered(gray, 100, 2)` 去除小点并框出筛选后最大的一块。

| 实验方法 | 功能 |
|---|---|
| `ShowOriginal(gray)` | 显示灰度图、宽高与指定像素 |
| `ShowBrightness(gray, 100)` | 变亮；传入 -100 则变暗 |
| `ShowInverted(gray)` | 灰度反转 |
| `ShowBinary(gray, 100)` | 二值化并统计白色像素 |
| `ShowConnectedComponents(gray, 100)` | 二值化、前景计数、四邻域连通域统计 |
| `ShowAreaFiltered(gray, 100, 2)` | 保留面积大于等于 2 的块，并显示结果 |
| `DisplayImage(pixels, gray)` | 公共显示方法，将数据转成 WPF BitmapSource |
| `ShowLargestRegionBox` | 选择最大连通域，同面积时选择先发现的块 |
| `ShowRegionBox` | 将图像行列与宽高换算为显示位置和尺寸 |
| `ShowRegionCentroid` | 在同一连通域的质心位置叠加直径为 8 的红点 |
| `ClearRegionBox` | 清除旧框，避免无目标或切换实验时残留 |

复习时，在 `TestAsync` 中把当前实验调用替换为对应的方法即可，每次只保留一个。

## 再看每个算法的实现

打开 `VisionStudyDemo/Imaging/GrayImageProcessor.cs`。处理方法返回新的像素数组，保留输入原图。

| 算法方法 | 输入 → 输出 |
|---|---|
| `Flatten` | 二维灰度数组 → 按行展开的一维数组 |
| `AdjustBrightness` | 灰度数组、偏移量 → 截断到 0～255 的新像素数组 |
| `Invert` | 灰度数组 → 255 减去原灰度 |
| `Threshold` | 灰度数组、阈值 → 严格小于阈值为 255，否则为 0 |
| `CountForeground` | 二值图 → 白色像素数 |
| `GetFourConnectedAreas` | 二值图、宽高 → 每个连通域的面积列表 |
| `GetFourConnectedRegions` | 二值图、宽高 → 连通域列表，每块包含坐标和面积 |
| `SearchFourConnectedRegion` | 白色起点、访问记录 → 一块完整的 ConnectedRegion（内部方法） |
| `FilterByArea` | 二值图、宽高、最小面积 → 筛选后的新二值图 |

当前命名约定：`x` 是行、`y` 是列；二维数组使用 `[x, y]`，一维数组使用 `[x * width + y]`。
这与常见图像坐标中 X 表示横向、Y 表示纵向的命名不同，读其他库的 API 时需要转换理解。

## 连通域的两个层次

`GetFourConnectedRegions` 用两层 for 扫描全图。遇到没访问过的白色起点，调用 `SearchFourConnectedRegion`。只需要面积时仍可使用 `GetFourConnectedAreas`。

`SearchFourConnectedRegion` 用队列搜索整块：取出一个像素，存入当前块的 Pixels，再检查四个邻居，发现合格邻居就标记并入队。队列为空时返回这一块。

`ConnectedRegion` 定义在 `VisionStudyDemo/Imaging/ConnectedRegion.cs`。它的 `Pixels` 保存坐标，`Area` 直接取 `Pixels.Count`。

`FilterByArea` 创建全黑输出图；对面积大于等于最小面积的块，遍历它的坐标并在输出中涂白。小块不涂白，就被去掉了。输入图不变。

访问标记 `visited` 由外层创建，所有块共用；每次重新统计会创建新的访问记录。
图像颜色来自 `pixels`，访问状态来自 `visited`，两者职责不同。

当前样例预期：阈值 100 时，前景像素 7，四邻域连通域 2，面积依次为 1、6。
阈值 30 时，前景像素 1，四邻域连通域 1，面积为 1。

## 运行算法检查

红框由四个属性绑定到 Canvas 上的 Rectangle：`BoxLeft`、`BoxTop`、`BoxWidth`、`BoxHeight`。
红点使用同一 Canvas 的 Ellipse，绑定 `DotLeft`、`DotTop`、`DotDiameter`、`DotVisibility`。没有目标、筛选清空或切换实验时，红框与红点一起清除。

质心按所有像素索引平均得到，不能用外接矩形中心代替。显示转换保持“左边缘 + 宽度的一半”的写法：

```csharp
double centerLeft = region.CenterCol * pixelWidth + pixelWidth / 2;
double centerTop = region.CenterRow * pixelHeight + pixelHeight / 2;
DotLeft = centerLeft - DotDiameter / 2;
DotTop = centerTop - DotDiameter / 2;
```

前两行求质心在界面上的位置；后两行由圆心和直径反求圆点外接方框左上角。当前样例圆心为 (200,160)，直径 8，圆点左上角为 (196,156)。
列索引决定 Left，行索引决定 Top。显示尺寸统一使用 `ImageDisplayWidth=400`、`ImageDisplayHeight=320`，XAML 和坐标换算共用这两个值；当前图片使用 Stretch=Fill。
5×4 样例中间区域的显示框为 Left=80、Top=80、Width=240、Height=160（WPF 布局单位）。红框是叠加层，不修改像素数组。

WPF 绑定检查（独立输出目录，避免正在运行的主程序占用文件）：

```powershell
dotnet run --project tests/VisionUi.Checks/VisionUi.Checks.csproj --artifacts-path "$env:TEMP/WPFVisionDemo-box-checks"
```

在仓库根目录执行：

```powershell
dotnet run --project tests/VisionAlgorithms.Checks/VisionAlgorithms.Checks.csproj
```

覆盖灰度展开、亮度上下限、反转、阈值等号边界、原图保留，以及四邻域的孤立点、斜向、边界、全黑、全白和重复调用；另检查区域坐标完整且不重复、面积筛选含等号、全部保留、全部去除和输入不变。
