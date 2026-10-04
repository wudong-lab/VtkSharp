# 移植说明

- 原始示例：[RibbonFilter.cxx](https://gitlab.kitware.com/vtk/vtk-examples/-/blob/3f2e3c4e9dc8c9f8f9d772e8d9ad45b01c58fa41/src/Cxx/PolyData/RibbonFilter.cxx)，仓库 revision：`3f2e3c4e9dc8c9f8f9d772e8d9ad45b01c58fa41`。
- 保留螺旋中心线和固定宽度带面的生成；弯矩、剪力及单元局部轴的含义由接入的业务层定义。
- 无外部数据文件。
- 验证：ExampleBrowser Release 构建和截图 smoke 通过，首帧已检查。交互窗操作与重复创建/释放尚未人工检查。
