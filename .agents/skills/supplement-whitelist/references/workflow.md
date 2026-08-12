# 参考导出接口补充工作流

## 目录

1. 扫描参考目录
2. 校验类型与方法
3. 生成 candidate
4. 合并和生成
5. 构建与核对

## 1. 扫描参考目录

运行技能附带脚本：

```powershell
.\.agents\skills\supplement-whitelist\scripts\scan-reference-exports.ps1 `
    -ReferenceDirectory <path> `
    -OutputPath <temporary-json-path>
```

脚本递归扫描 `*_export_gen.cpp`，从 VTK header include 获取类名，从导出函数名获取方法名；它会跳过 `New`，移除结尾的数字 overload 标识并去重。先检查 JSON 中的 `Files`、`Classes` 和 `Warnings`。

参考导出可能来自旧 VTK 或不同生成器，因此结果只用于产生待检查的方法集合。

## 2. 校验类型与方法

对每个类运行：

```powershell
dotnet run --project src/generator/VtkSharp.Generator.Cli -- `
    inspect-class <ClassName> --format json
```

不存在于当前 VTK 安装的类应记录并跳过。对继承关系不清楚的方法使用 `inspect-function` 确认声明类。

将类分为：

- 新类且有支持的方法；
- 已有类但缺少方法；
- 当前 VTK 不存在或签名不支持；
- 已完全覆盖，无需修改。

如果参考文件只有 `New()`，不要在不传 `--methods` 的情况下创建 candidate；该参数缺失代表导出全部支持的方法，而不是空类型。确实需要仅补类型时，应先确认生成器是否已有明确的 class-only 能力，否则停止并报告缺口。

## 3. 生成 candidate

对需要补充的声明类运行：

```powershell
dotnet run --project src/generator/VtkSharp.Generator.Cli -- `
    create-candidate <ClassName> `
    -o <temporary-candidate.yml> `
    --supported-only `
    --skip-missing-methods `
    --source-kind manual `
    --source-name from-reference-exports `
    --source-original <reference-file> `
    --methods Method1 Method2

dotnet run --project src/generator/VtkSharp.Generator.Cli -- `
    diff-whitelist <temporary-candidate.yml>
```

审核只出现预期新增项后再 `merge-candidate`。缺失方法警告必须归因到版本差异、声明类或解析问题，不能静默忽略系统性错误。

## 4. 合并和生成

```powershell
dotnet run --project src/generator/VtkSharp.Generator.Cli -- merge-candidate <candidate.yml>
dotnet run --project src/generator/VtkSharp.Generator.Cli -- normalize-whitelist
dotnet run --project src/generator/VtkSharp.Generator.Cli -- validate-whitelist
dotnet run --project src/generator/VtkSharp.Generator.Cli -- `
    generate-bindings --output-root src --incremental
```

生成器应从 whitelist 和 runtime modules 生成完整 module 列表。如果 module 缺失，修正 generator/config 输入；不要把手工编辑生成 CMake 文件作为常规方案。

## 5. 构建与核对

```powershell
$vtkDir = "D:\Code\VTK\VtkGitBuild\install\lib\cmake\vtk-9.6"
.\tools\build-native.ps1 -Configuration Release -VtkDir $vtkDir
dotnet test src/bindings/VtkSharp.slnx --configuration Release
dotnet run --project src/generator/VtkSharp.Generator.Cli -- generate-bindings --check
```

如果 C# 报告缺少基类 wrapper，应对基类执行同样的 inspect/candidate/diff/merge 流程，再重新生成。最终报告扫描文件数、有效/跳过类型、添加方法数、发现的基类和全部验证结果。
