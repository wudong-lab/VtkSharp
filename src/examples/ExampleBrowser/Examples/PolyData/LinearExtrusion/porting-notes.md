# 移植说明

- 原始示例：[LinearExtrusion.cxx](https://gitlab.kitware.com/vtk/vtk-examples/-/blob/3f2e3c4e9dc8c9f8f9d772e8d9ad45b01c58fa41/src/Cxx/PolyData/LinearExtrusion.cxx)，仓库 revision：`3f2e3c4e9dc8c9f8f9d772e8d9ad45b01c58fa41`。
- 按业务要求以闭合 I 形轮廓替代文字轮廓；轮廓位于单元局部 Y/Z 平面，向量挤出沿两端点连线，长度由端点坐标确定。选取不平行于单元轴的参考向量构造右手局部系。
- 示例端点固定且不重合；退化零长度单元需由业务数据层过滤。
- 无外部数据文件。
- 验证：ExampleBrowser Release 构建和截图 smoke 通过，首帧已检查。交互窗操作与重复创建/释放尚未人工检查。
