# 渲染窗口截图的数据持有关系

## vtkWindowToImageFilter::vtkImageData* GetOutput()

- 契约：返回 filter 管线中的图像对象，不向调用方转移一个新增引用。需要在 filter 释放后继续使用图像时，managed 侧必须增加自己的引用。
- 依据：本地 VTK 9.7 源码提交 `23f0a095621e91bbdbeace8451e22b950c8e5f46`。`Rendering/Core/vtkWindowToImageFilter.cxx` 的 `GetOutput()` 转发到 `GetOutputDataObject(0)`；`Common/ExecutionModel/vtkAlgorithm.cxx` 转发到 executive；`Common/ExecutionModel/vtkExecutive.cxx` 从 output information 读取数据对象，没有给调用方增加引用。源码仓库为 `https://gitlab.kitware.com/vtk/vtk`。
- 决定：保持生成的 `GetOutput()` 为 borrowed；复用现有 `vtkWindow.GetRgbImageData()`，它在局部 filter 释放前执行 `vtkImageData.Register(...)`。Cone 截图模式用 `using` 持有返回图像，写出结束后释放该引用。不修改现有白名单或包装实现。
- 验证：2026-08-30，Cone 自动验收成功输出 800×600 PNG，运行器读取成功并正常退出，人工查看锥体形状正常。验证报告位于 `artifacts/verification/20260830-190313-0f961f37/verification.json`。未验证长期重复截图的内存增长，也未做引用计数专项测试。
- 复核条件：VTK 升级、下列函数或文件变化、`GetRgbImageData`/`Register` 实现、返回值 ownership 或托管释放策略改变时重新检查。源码与本机安装二进制的对应关系仍依赖本地 VTK 构建配置，本记录不是二进制来源证明。

检查时的 SHA256：

| 文件 | SHA256 |
|---|---|
| `Rendering/Core/vtkWindowToImageFilter.cxx` | `44BCC3346CEEA0C14A228DE7BD2A70A4009E9213DA7040A2F4005273053BBC14` |
| `Common/ExecutionModel/vtkAlgorithm.cxx` | `AB834D50BF036F00E1A9952E8B880DA9AA4CF8122E995DE137C46C6B3CC18EBF` |
| `Common/ExecutionModel/vtkExecutive.cxx` | `921505CD196449E3E59DD968EAAE63A0DB7A1114F9B272196770AD429DEE580E` |

本机安装头文件 `include/vtk-9.7/vtkWindowToImageFilter.h` 的 SHA256 为 `8EFACE9A3741BFC237D4C7A553444FE8C07E2F5F01F608E0BCC0A2289E84F0D3`。
