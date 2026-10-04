# 移植说明

- 原始示例：[Glyph3DMapper.cxx](https://gitlab.kitware.com/vtk/vtk-examples/-/blob/3f2e3c4e9dc8c9f8f9d772e8d9ad45b01c58fa41/src/Cxx/Visualization/Glyph3DMapper.cxx)，仓库 revision：`3f2e3c4e9dc8c9f8f9d772e8d9ad45b01c58fa41`。
- 保留 GPU glyph、点颜色数组和三分量缩放数组；使用已绑定的数组访问接口填入三种 RGB 颜色。
- 无外部数据文件。
- 验证：ExampleBrowser Release 构建和截图 smoke 通过，首帧已检查。交互窗操作与重复创建/释放尚未人工检查。
