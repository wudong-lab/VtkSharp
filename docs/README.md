# VtkSharp 文档

本文档目录只记录公开 VTK 绑定项目当前有效的设计和开发流程。

- [项目架构](architecture.md)：项目职责、绑定分层和 native 产物。
- [Native 指针封装与所有权](native-pointer-ownership.md)：从外部指针创建 wrapper 时的引用计数和生命周期约定。
- [绑定生成器](generator.md)：配置、白名单、导出规则和常用命令。
- [VTK 构建](build/vtk.md)：Windows 静态 VTK 的配置、编译和安装。
- [VtkSharp 构建](build/vtksharp.md)：native/managed 构建、CRT 匹配和产物收集。
- [AI 辅助开发](workflow/ai-assisted-development.md)：项目协作与验证约定。
- `learning/`：C#、P/Invoke 和 native 互操作专题资料。

已完成的实施计划和与现状冲突的历史规格不在仓库中继续维护；需要追溯时使用 Git 历史。
