# VtkSharp 文档

本文档目录只记录公开 VTK 绑定项目当前有效的设计和开发流程。

- [项目架构](architecture.md)：项目职责、绑定分层和 native 产物。
- [Native 指针封装与所有权](native-pointer-ownership.md)：从外部指针创建 wrapper 时的引用计数和生命周期约定。
- [绑定生成器](generator.md)：配置、白名单、导出规则和常用命令。
- [VTK 构建](build/vtk.md)：Windows 静态 VTK 的配置、编译和安装。
- [VtkSharp 构建](build/vtksharp.md)：native/managed 构建、CRT 匹配和产物收集。
- [动态 VTK 迁移结果与约束](build/dynamic-vtk-migration.md)：当前动态链接、依赖收集、本地 NuGet 和私有 native 独立架构及验证结果。
- [模块化 native 导出层与按需加载设计](build/modular-native-loading.md)：已确认决策、分组依据、子目录部署和公开/私有仓库实施步骤，尚未实施。
- [贡献指南](../CONTRIBUTING.md)：问题反馈、API 补充与 Pull Request 要求。
- [统一验证与示例验收](workflow/verification.md)：构建、测试、生成检查和截图验收。
- [互操作依据记录](workflow/interop-evidence.md)：方向、长度和所有权判断的依据。
- [示例浏览器](../src/examples/README.md)：运行方式、分类和新增示例约定。
- [地形示例移植清单](examples/terrain-porting-checklist.md)：官方地形建模与处理示例、移植难度和推荐实施顺序。
- [模型着色与图例示例移植清单](examples/coloring-legend-porting-checklist.md)：顶点与单元着色、色标、分类图例、移植难度和推荐实施顺序。
- [鼠标、键盘、选择与高亮示例移植清单](examples/interaction-picking-porting-checklist.md)：输入交互、图元拾取与框选、高亮、拖动编辑、移植难度和推荐实施顺序。
- [AI 辅助开发](workflow/ai-assisted-development.md)：项目协作与验证约定。
- `learning/`：C#、P/Invoke 和 native 互操作专题资料。

已完成的实施计划和与现状冲突的历史规格不在仓库中继续维护；需要追溯时使用 Git 历史。
