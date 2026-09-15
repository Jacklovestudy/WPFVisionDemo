# WPFVisionDemo

使用 C# 学习机器视觉原理的实验项目。通过小矩阵手算、算法实现和图像实验，逐步理解算法的作用、适用条件与失败原因。

## 当前基础

- .NET 10 / WPF。
- Prism.DryIoc 9.0.537：应用启动、依赖注入、ViewModel 绑定。
- [WPF UI](https://github.com/lepoco/wpfui) 4.3.0：Fluent 控件样式和黑色主题。
- 当前测试按钮可显示实验图和四邻域统计；各项已学功能已拆为独立方法。

## 界面模块

界面通过五个 Tab 区分功能，共用图片预览和处理结果。切换 Tab 保留图片、ROI 和参数。

| Tab | 功能 |
| --- | --- |
| 图片与 ROI | 框选、清除 ROI，查看 ROI 二值图 |
| 灰度与滤波 | 转灰度、均值/中值滤波、滤波后二值化、边缘检测 |
| 二值化与形态学 | 原始二值图、开/闭运算预览、邻域大小 |
| 轮廓与筛选 | 提取轮廓、按面积/形状/位置筛选、轮廓单步跟踪 |
| 基础演示 | 数组实验、开/闭运算演示、轮廓跟踪演示 |

右上角始终提供“选择图片”和“显示原图”。图片和标记一起缩放，ROI 框选仍按原图像素换算。

“轮廓与筛选”中，先点击“按条件选目标”，再点击“下一个合格区域”循环浏览面积/形状合格的区域（按距离由近到远）。界面显示当前序号、面积、宽高比、填充率和距离；距离不合格的候选也可查看，并明确标注。最后一个会回到第一个，切换区域会将轮廓跟踪重置到起点。更改筛选参数后再次点击会按新条件重新计算。

## 学习入口

从原“视觉”对话承接的目标、计划和课程已归档到当前项目：

- [学习进度与项目约定](docs/vision/progress.md)
- [24 周学习计划](docs/vision/learning-plan.md)
- [视觉知识体系](docs/vision/knowledge-map.md)
- [第一课：图像、像素与坐标](docs/vision/lesson-01-pixels.md)

当前学习四邻域连通域搜索，见 [实验代码阅读指南](docs/vision/code-guide.md)。课程讲解和项目实现逐课推进，练习验收后更新进度。

## 运行

在 Windows 上安装 .NET 10 SDK，使用 Visual Studio 打开 `VisionStudyDemo/VisionStudyDemo.slnx`，或在仓库根目录执行：

```powershell
dotnet run --project VisionStudyDemo/VisionStudyDemo.csproj
```
