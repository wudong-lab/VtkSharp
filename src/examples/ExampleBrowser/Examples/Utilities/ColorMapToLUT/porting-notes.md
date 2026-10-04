# ColorMapToLUT 移植记录

- 官方源码：[ColorMapToLUT](https://examples.vtk.org/site/Cxx/Utilities/ColorMapToLUT/)，Attribution 2.5。
- 固定采用原例 Fast palette、Lab 插值和离散化模式；示例浏览器版本使用固定圆锥数据，不复制 CLI11 的球体、连续/反向参数切换。
- Mapper 默认查表映射；未显式开启 `InterpolateScalarsBeforeMapping`，因此颜色插值阶段与原例略有差异。
- 验证：统一流程通过，报告 `artifacts/verification/20261004-195910-42b073a2/verification.json`；仍需人工查看离散配色首帧。
