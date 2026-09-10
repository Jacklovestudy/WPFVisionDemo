# WPFVisionDemo

使用 C# 学习机器视觉原理的实验项目。通过小矩阵手算、算法实现和图像实验，逐步理解算法的作用、适用条件与失败原因。

## 当前基础

- .NET 10 / WPF。
- Prism.DryIoc 9.0.537：应用启动、依赖注入、ViewModel 绑定。
- 当前测试按钮可显示实验图和四邻域统计；各项已学功能已拆为独立方法。

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
