# VtkSharp Generator MVP Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the first CLI-based VtkSharp binding generator that can inspect, validate, and generate the current `TestConsole` API closure into a temporary output directory.

**Architecture:** Implement a small generator solution under `src/generator` with a reusable Core library and a CLI front-end. The first implementation is deliberately narrow: it reads YAML configuration and module whitelist files, canonicalizes CppAst signatures, validates the `TestConsole` API closure, and emits generated C# / C++ / CMake files to a temp output root for round-trip diffing.

**Tech Stack:** .NET, C#, CppAst NuGet package, YamlDotNet, System.CommandLine, xUnit, JSON Schema files for YAML editing.

---

## File Structure

Create:

```text
src/generator/VtkSharp.Generator.slnx
src/generator/Directory.Build.props
src/generator/VtkSharp.Generator.Core/VtkSharp.Generator.Core.csproj
src/generator/VtkSharp.Generator.Cli/VtkSharp.Generator.Cli.csproj
src/generator/VtkSharp.Generator.Tests/VtkSharp.Generator.Tests.csproj
src/generator/config/vtksharp.generator.yml
src/generator/config/vtksharp.generator.local.example.yml
src/generator/schemas/vtksharp.generator.schema.json
src/generator/schemas/vtksharp.generator.local.schema.json
src/generator/schemas/vtksharp.whitelist.schema.json
src/generator/schemas/vtksharp.whitelist-candidate.schema.json
src/generator/whitelist/vtkCommonCore.yml
src/generator/whitelist/vtkCommonExecutionModel.yml
src/generator/whitelist/vtkFiltersSources.yml
src/generator/whitelist/vtkRenderingCore.yml
src/generator/VtkSharp.Generator.Core/Configuration/GeneratorConfig.cs
src/generator/VtkSharp.Generator.Core/Configuration/GeneratorConfigLoader.cs
src/generator/VtkSharp.Generator.Core/Whitelist/WhitelistDocument.cs
src/generator/VtkSharp.Generator.Core/Whitelist/WhitelistLoader.cs
src/generator/VtkSharp.Generator.Core/Vtk/VtkHierarchyEntry.cs
src/generator/VtkSharp.Generator.Core/Vtk/VtkHierarchyReader.cs
src/generator/VtkSharp.Generator.Core/Types/CanonicalType.cs
src/generator/VtkSharp.Generator.Core/Types/TypeCanonicalizer.cs
src/generator/VtkSharp.Generator.Core/Inspection/VtkClassInspector.cs
src/generator/VtkSharp.Generator.Core/Validation/WhitelistValidator.cs
src/generator/VtkSharp.Generator.Core/Generation/ExportNameGenerator.cs
src/generator/VtkSharp.Generator.Core/Generation/CSharpBindingEmitter.cs
src/generator/VtkSharp.Generator.Core/Generation/CppExportEmitter.cs
src/generator/VtkSharp.Generator.Core/Generation/CMakeModulesEmitter.cs
src/generator/VtkSharp.Generator.Cli/Program.cs
src/generator/VtkSharp.Generator.Tests/TypeCanonicalizerTests.cs
src/generator/VtkSharp.Generator.Tests/ExportNameGeneratorTests.cs
src/generator/VtkSharp.Generator.Tests/WhitelistLoaderTests.cs
src/generator/VtkSharp.Generator.Tests/TestData/whitelist/vtkRenderingCore.yml
```

Modify:

```text
.gitignore
```

Do not modify in this MVP:

```text
src/bindings/VtkSharp/VtkSharp/**/*_gen.cs
src/native/src/**/*_export_gen.cpp
src/native/CMakeLists.txt
```

The generator must emit into a temporary output root first.

---

### Task 1: Generator Solution Scaffold

**Files:**
- Create: `src/generator/VtkSharp.Generator.slnx`
- Create: `src/generator/Directory.Build.props`
- Create: `src/generator/VtkSharp.Generator.Core/VtkSharp.Generator.Core.csproj`
- Create: `src/generator/VtkSharp.Generator.Cli/VtkSharp.Generator.Cli.csproj`
- Create: `src/generator/VtkSharp.Generator.Tests/VtkSharp.Generator.Tests.csproj`
- Modify: `.gitignore`

- [ ] **Step 1: Create the generator solution and projects**

Run:

```powershell
Set-Location <repo-root>
New-Item -ItemType Directory -Force src\generator | Out-Null
dotnet new sln -n VtkSharp.Generator -o src\generator
dotnet new classlib -n VtkSharp.Generator.Core -o src\generator\VtkSharp.Generator.Core
dotnet new console -n VtkSharp.Generator.Cli -o src\generator\VtkSharp.Generator.Cli
dotnet new xunit -n VtkSharp.Generator.Tests -o src\generator\VtkSharp.Generator.Tests
dotnet sln src\generator\VtkSharp.Generator.sln add src\generator\VtkSharp.Generator.Core\VtkSharp.Generator.Core.csproj
dotnet sln src\generator\VtkSharp.Generator.sln add src\generator\VtkSharp.Generator.Cli\VtkSharp.Generator.Cli.csproj
dotnet sln src\generator\VtkSharp.Generator.sln add src\generator\VtkSharp.Generator.Tests\VtkSharp.Generator.Tests.csproj
dotnet add src\generator\VtkSharp.Generator.Cli\VtkSharp.Generator.Cli.csproj reference src\generator\VtkSharp.Generator.Core\VtkSharp.Generator.Core.csproj
dotnet add src\generator\VtkSharp.Generator.Tests\VtkSharp.Generator.Tests.csproj reference src\generator\VtkSharp.Generator.Core\VtkSharp.Generator.Core.csproj
```

Expected: solution and three projects are created.

- [ ] **Step 2: Add packages**

Run:

```powershell
dotnet add src\generator\VtkSharp.Generator.Core\VtkSharp.Generator.Core.csproj package CppAst
dotnet add src\generator\VtkSharp.Generator.Core\VtkSharp.Generator.Core.csproj package YamlDotNet
dotnet add src\generator\VtkSharp.Generator.Cli\VtkSharp.Generator.Cli.csproj package System.CommandLine --prerelease
```

Expected: packages restore successfully. Use the latest available NuGet package versions at implementation time.

- [ ] **Step 3: Add shared build props**

Create `src/generator/Directory.Build.props`:

```xml
<Project>
  <PropertyGroup>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>false</TreatWarningsAsErrors>
  </PropertyGroup>
</Project>
```

- [ ] **Step 4: Ignore local generator config and generated temp output**

Append to `.gitignore` if missing:

```gitignore
# VtkSharp generator local configuration
src/generator/config/vtksharp.generator.local.yml
src/generator/out/
```

- [ ] **Step 5: Build scaffold**

Run:

```powershell
dotnet build src\generator\VtkSharp.Generator.sln
```

Expected: build succeeds.

- [ ] **Step 6: Commit scaffold**

Run:

```powershell
git add .gitignore src/generator
git commit -m "搭建绑定生成器项目骨架"
```

---

### Task 2: Configuration And Schema Files

**Files:**
- Create: `src/generator/config/vtksharp.generator.yml`
- Create: `src/generator/config/vtksharp.generator.local.example.yml`
- Create: `src/generator/schemas/vtksharp.generator.schema.json`
- Create: `src/generator/schemas/vtksharp.generator.local.schema.json`
- Create: `src/generator/schemas/vtksharp.whitelist.schema.json`
- Create: `src/generator/schemas/vtksharp.whitelist-candidate.schema.json`
- Create: `src/generator/VtkSharp.Generator.Core/Configuration/GeneratorConfig.cs`
- Create: `src/generator/VtkSharp.Generator.Core/Configuration/GeneratorConfigLoader.cs`

- [ ] **Step 1: Add public generator config**

Create `src/generator/config/vtksharp.generator.yml`:

```yaml
# yaml-language-server: $schema=../schemas/vtksharp.generator.schema.json

vtk:
  version: "9.5"
  modulePrefix: vtk

binding:
  namespace: VtkSharp
  nativeLibraryName: vtksharp_native
  manualBindingClasses:
    - vtkObjectBase
    - vtkObject

paths:
  whitelistDirectory: ../whitelist
  managedOutputDirectory: ../../bindings/VtkSharp/VtkSharp
  nativeOutputDirectory: ../../native/src
  nativeModulesFile: ../../native/vtksharp.modules.generated.cmake

generation:
  createManualExtensionFiles: false
  overwriteGeneratedFiles: true
  deleteOrphanGeneratedFiles: false
```

- [ ] **Step 2: Add local config example**

Create `src/generator/config/vtksharp.generator.local.example.yml`:

```yaml
# yaml-language-server: $schema=../schemas/vtksharp.generator.local.schema.json

vtk:
  rootDirectory: "C:/Program Files/VTK"
```

- [ ] **Step 3: Add minimal JSON schemas**

Create `src/generator/schemas/vtksharp.generator.schema.json`:

```json
{
  "$schema": "https://json-schema.org/draft/2020-12/schema",
  "title": "VtkSharp Generator Config",
  "type": "object",
  "additionalProperties": false,
  "required": ["vtk", "binding", "paths", "generation"],
  "properties": {
    "vtk": {
      "type": "object",
      "additionalProperties": false,
      "required": ["version", "modulePrefix"],
      "properties": {
        "version": { "type": "string" },
        "modulePrefix": { "type": "string" }
      }
    },
    "binding": {
      "type": "object",
      "additionalProperties": false,
      "required": ["namespace", "nativeLibraryName", "manualBindingClasses"],
      "properties": {
        "namespace": { "type": "string" },
        "nativeLibraryName": { "type": "string" },
        "manualBindingClasses": {
          "type": "array",
          "items": { "type": "string" }
        }
      }
    },
    "paths": {
      "type": "object",
      "additionalProperties": false,
      "required": ["whitelistDirectory", "managedOutputDirectory", "nativeOutputDirectory", "nativeModulesFile"],
      "properties": {
        "whitelistDirectory": { "type": "string" },
        "managedOutputDirectory": { "type": "string" },
        "nativeOutputDirectory": { "type": "string" },
        "nativeModulesFile": { "type": "string" }
      }
    },
    "generation": {
      "type": "object",
      "additionalProperties": false,
      "required": ["createManualExtensionFiles", "overwriteGeneratedFiles", "deleteOrphanGeneratedFiles"],
      "properties": {
        "createManualExtensionFiles": { "type": "boolean" },
        "overwriteGeneratedFiles": { "type": "boolean" },
        "deleteOrphanGeneratedFiles": { "type": "boolean" }
      }
    }
  }
}
```

Create `src/generator/schemas/vtksharp.generator.local.schema.json`:

```json
{
  "$schema": "https://json-schema.org/draft/2020-12/schema",
  "title": "VtkSharp Generator Local Config",
  "type": "object",
  "additionalProperties": false,
  "required": ["vtk"],
  "properties": {
    "vtk": {
      "type": "object",
      "additionalProperties": false,
      "required": ["rootDirectory"],
      "properties": {
        "rootDirectory": { "type": "string" },
        "includeDirectory": { "type": "string" },
        "hierarchyDirectory": { "type": "string" }
      }
    }
  }
}
```

Create `src/generator/schemas/vtksharp.whitelist.schema.json`:

```json
{
  "$schema": "https://json-schema.org/draft/2020-12/schema",
  "title": "VtkSharp Whitelist",
  "type": "object",
  "additionalProperties": false,
  "required": ["module", "classes"],
  "properties": {
    "module": { "type": "string" },
    "classes": {
      "type": "array",
      "items": {
        "type": "object",
        "additionalProperties": false,
        "required": ["name", "header", "functions"],
        "properties": {
          "name": { "type": "string" },
          "header": { "type": "string" },
          "functions": {
            "type": "array",
            "items": {
              "type": "object",
              "additionalProperties": false,
              "required": ["name", "cppSignature", "return", "parameters"],
              "properties": {
                "name": { "type": "string" },
                "cppSignature": { "type": "string" },
                "return": {
                  "type": "object",
                  "additionalProperties": false,
                  "required": ["type"],
                  "properties": {
                    "type": { "type": "string" },
                    "ownership": { "type": "string", "enum": ["owned", "borrowed"] }
                  }
                },
                "parameters": {
                  "type": "array",
                  "items": {
                    "type": "object",
                    "additionalProperties": false,
                    "required": ["type", "name"],
                    "properties": {
                      "type": { "type": "string" },
                      "name": { "type": "string" },
                      "direction": { "type": "string", "enum": ["in", "out", "inout"] },
                      "length": {
                        "type": "object",
                        "additionalProperties": false,
                        "required": ["kind"],
                        "properties": {
                          "kind": { "type": "string", "enum": ["fixed", "parameter"] },
                          "value": { "type": "integer" },
                          "name": { "type": "string" }
                        }
                      }
                    }
                  }
                }
              }
            }
          }
        }
      }
    }
  }
}
```

Create `src/generator/schemas/vtksharp.whitelist-candidate.schema.json` as a permissive first version:

```json
{
  "$schema": "https://json-schema.org/draft/2020-12/schema",
  "title": "VtkSharp Whitelist Candidate",
  "type": "object",
  "additionalProperties": true,
  "required": ["status", "source", "requirements"],
  "properties": {
    "status": { "type": "string", "enum": ["proposed"] },
    "source": { "type": "object" },
    "requirements": { "type": "array" }
  }
}
```

- [ ] **Step 4: Add config model**

Create `src/generator/VtkSharp.Generator.Core/Configuration/GeneratorConfig.cs`:

```csharp
namespace VtkSharp.Generator.Core.Configuration;

public sealed record GeneratorConfig(
    VtkConfig Vtk,
    BindingConfig Binding,
    PathConfig Paths,
    GenerationConfig Generation);

public sealed record VtkConfig(
    string Version,
    string ModulePrefix,
    string? RootDirectory = null,
    string? IncludeDirectory = null,
    string? HierarchyDirectory = null);

public sealed record BindingConfig(
    string Namespace,
    string NativeLibraryName,
    IReadOnlyList<string> ManualBindingClasses);

public sealed record PathConfig(
    string WhitelistDirectory,
    string ManagedOutputDirectory,
    string NativeOutputDirectory,
    string NativeModulesFile);

public sealed record GenerationConfig(
    bool CreateManualExtensionFiles,
    bool OverwriteGeneratedFiles,
    bool DeleteOrphanGeneratedFiles);
```

- [ ] **Step 5: Add config loader**

Create `src/generator/VtkSharp.Generator.Core/Configuration/GeneratorConfigLoader.cs`:

```csharp
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace VtkSharp.Generator.Core.Configuration;

public sealed class GeneratorConfigLoader
{
    private readonly IDeserializer _deserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    public GeneratorConfig Load(string configPath, string? localConfigPath, string? vtkRootOverride)
    {
        var config = ReadRequired(configPath);
        var local = localConfigPath is not null && File.Exists(localConfigPath)
            ? ReadRequired(localConfigPath)
            : null;

        var vtkRoot = vtkRootOverride
            ?? Environment.GetEnvironmentVariable("VTK_ROOT")
            ?? local?.Vtk.RootDirectory
            ?? config.Vtk.RootDirectory;

        var vtk = config.Vtk with
        {
            RootDirectory = vtkRoot,
            IncludeDirectory = local?.Vtk.IncludeDirectory ?? config.Vtk.IncludeDirectory,
            HierarchyDirectory = local?.Vtk.HierarchyDirectory ?? config.Vtk.HierarchyDirectory,
        };

        return config with { Vtk = vtk };
    }

    private GeneratorConfig ReadRequired(string path)
    {
        using var reader = File.OpenText(path);
        return _deserializer.Deserialize<GeneratorConfig>(reader);
    }
}
```

- [ ] **Step 6: Build**

Run:

```powershell
dotnet build src\generator\VtkSharp.Generator.sln
```

Expected: build succeeds.

- [ ] **Step 7: Commit**

Run:

```powershell
git add src/generator
git commit -m "添加生成器配置和 schema"
```

---

### Task 3: Whitelist Model And TestConsole Minimal Whitelist

**Files:**
- Create: `src/generator/VtkSharp.Generator.Core/Whitelist/WhitelistDocument.cs`
- Create: `src/generator/VtkSharp.Generator.Core/Whitelist/WhitelistLoader.cs`
- Create: `src/generator/whitelist/vtkCommonCore.yml`
- Create: `src/generator/whitelist/vtkCommonExecutionModel.yml`
- Create: `src/generator/whitelist/vtkFiltersSources.yml`
- Create: `src/generator/whitelist/vtkRenderingCore.yml`
- Create: `src/generator/VtkSharp.Generator.Tests/WhitelistLoaderTests.cs`
- Create: `src/generator/VtkSharp.Generator.Tests/TestData/whitelist/vtkRenderingCore.yml`

- [ ] **Step 1: Add whitelist records**

Create `src/generator/VtkSharp.Generator.Core/Whitelist/WhitelistDocument.cs`:

```csharp
namespace VtkSharp.Generator.Core.Whitelist;

public sealed record WhitelistDocument(
    string Module,
    IReadOnlyList<WhitelistClass> Classes);

public sealed record WhitelistClass(
    string Name,
    string Header,
    IReadOnlyList<WhitelistFunction> Functions);

public sealed record WhitelistFunction(
    string Name,
    string CppSignature,
    WhitelistReturn Return,
    IReadOnlyList<WhitelistParameter> Parameters);

public sealed record WhitelistReturn(
    string Type,
    string? Ownership = null);

public sealed record WhitelistParameter(
    string Type,
    string Name,
    string? Direction = null,
    WhitelistLength? Length = null);

public sealed record WhitelistLength(
    string Kind,
    int? Value = null,
    string? Name = null);
```

- [ ] **Step 2: Add whitelist loader**

Create `src/generator/VtkSharp.Generator.Core/Whitelist/WhitelistLoader.cs`:

```csharp
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace VtkSharp.Generator.Core.Whitelist;

public sealed class WhitelistLoader
{
    private readonly IDeserializer _deserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    public IReadOnlyList<WhitelistDocument> LoadDirectory(string directory)
    {
        var files = Directory.GetFiles(directory, "vtk*.yml", SearchOption.TopDirectoryOnly)
            .OrderBy(static path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return files.Select(LoadFile).ToList();
    }

    public WhitelistDocument LoadFile(string path)
    {
        using var reader = File.OpenText(path);
        return _deserializer.Deserialize<WhitelistDocument>(reader);
    }
}
```

- [ ] **Step 3: Add TestConsole minimal whitelist**

Create `src/generator/whitelist/vtkFiltersSources.yml`:

```yaml
# yaml-language-server: $schema=../schemas/vtksharp.whitelist.schema.json

module: vtkFiltersSources
classes:
  - name: vtkConeSource
    header: vtkConeSource.h
    functions: []
```

Create `src/generator/whitelist/vtkCommonExecutionModel.yml`:

```yaml
# yaml-language-server: $schema=../schemas/vtksharp.whitelist.schema.json

module: vtkCommonExecutionModel
classes:
  - name: vtkAlgorithm
    header: vtkAlgorithm.h
    functions:
      - name: SetInputConnection
        cppSignature: "void SetInputConnection(vtkAlgorithmOutput* input)"
        return:
          type: void
        parameters:
          - type: vtkAlgorithmOutput*
            name: input
      - name: GetOutputPort
        cppSignature: "vtkAlgorithmOutput* GetOutputPort()"
        return:
          type: vtkAlgorithmOutput*
        parameters: []
      - name: Update
        cppSignature: "void Update()"
        return:
          type: void
        parameters: []
  - name: vtkAlgorithmOutput
    header: vtkAlgorithmOutput.h
    functions: []
  - name: vtkPolyDataAlgorithm
    header: vtkPolyDataAlgorithm.h
    functions: []
```

Create `src/generator/whitelist/vtkRenderingCore.yml`:

```yaml
# yaml-language-server: $schema=../schemas/vtksharp.whitelist.schema.json

module: vtkRenderingCore
classes:
  - name: vtkActor
    header: vtkActor.h
    functions:
      - name: SetMapper
        cppSignature: "void SetMapper(vtkMapper* mapper)"
        return:
          type: void
        parameters:
          - type: vtkMapper*
            name: mapper
  - name: vtkMapper
    header: vtkMapper.h
    functions: []
  - name: vtkPolyDataMapper
    header: vtkPolyDataMapper.h
    functions:
      - name: SetInputConnection
        cppSignature: "void SetInputConnection(vtkAlgorithmOutput* input)"
        return:
          type: void
        parameters:
          - type: vtkAlgorithmOutput*
            name: input
  - name: vtkRenderer
    header: vtkRenderer.h
    functions:
      - name: AddActor
        cppSignature: "void AddActor(vtkProp* p)"
        return:
          type: void
        parameters:
          - type: vtkProp*
            name: p
  - name: vtkRenderWindow
    header: vtkRenderWindow.h
    functions:
      - name: AddRenderer
        cppSignature: "void AddRenderer(vtkRenderer* renderer)"
        return:
          type: void
        parameters:
          - type: vtkRenderer*
            name: renderer
  - name: vtkRenderWindowInteractor
    header: vtkRenderWindowInteractor.h
    functions:
      - name: SetRenderWindow
        cppSignature: "void SetRenderWindow(vtkRenderWindow* aren)"
        return:
          type: void
        parameters:
          - type: vtkRenderWindow*
            name: aren
      - name: Start
        cppSignature: "void Start()"
        return:
          type: void
        parameters: []
  - name: vtkProp
    header: vtkProp.h
    functions: []
  - name: vtkProp3D
    header: vtkProp3D.h
    functions: []
```

Create `src/generator/whitelist/vtkCommonCore.yml`:

```yaml
# yaml-language-server: $schema=../schemas/vtksharp.whitelist.schema.json

module: vtkCommonCore
classes:
  - name: vtkWindow
    header: vtkWindow.h
    functions:
      - name: SetSize
        cppSignature: "void SetSize(int width, int height)"
        return:
          type: void
        parameters:
          - { type: int, name: width }
          - { type: int, name: height }
      - name: Render
        cppSignature: "void Render()"
        return:
          type: void
        parameters: []
```

The exact function set can be adjusted during validation if current headers expose inherited methods differently. Keep the scope tied to `TestConsole`.

- [ ] **Step 4: Add loader test**

Create `src/generator/VtkSharp.Generator.Tests/WhitelistLoaderTests.cs`:

```csharp
using VtkSharp.Generator.Core.Whitelist;

namespace VtkSharp.Generator.Tests;

public sealed class WhitelistLoaderTests
{
    [Fact]
    public void LoadFile_ReadsWhitelistDocument()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "TestData", "whitelist", "vtkRenderingCore.yml");
        var loader = new WhitelistLoader();

        var document = loader.LoadFile(path);

        Assert.Equal("vtkRenderingCore", document.Module);
        var actor = Assert.Single(document.Classes);
        Assert.Equal("vtkActor", actor.Name);
        var function = Assert.Single(actor.Functions);
        Assert.Equal("SetMapper", function.Name);
        Assert.Equal("void", function.Return.Type);
        Assert.Equal("vtkMapper*", function.Parameters[0].Type);
    }
}
```

Create `src/generator/VtkSharp.Generator.Tests/TestData/whitelist/vtkRenderingCore.yml`:

```yaml
module: vtkRenderingCore
classes:
  - name: vtkActor
    header: vtkActor.h
    functions:
      - name: SetMapper
        cppSignature: "void SetMapper(vtkMapper* mapper)"
        return:
          type: void
        parameters:
          - type: vtkMapper*
            name: mapper
```

Modify `src/generator/VtkSharp.Generator.Tests/VtkSharp.Generator.Tests.csproj` to copy test data:

```xml
<ItemGroup>
  <None Include="TestData\**\*.*" CopyToOutputDirectory="PreserveNewest" />
</ItemGroup>
```

- [ ] **Step 5: Run tests**

Run:

```powershell
dotnet test src\generator\VtkSharp.Generator.Tests\VtkSharp.Generator.Tests.csproj
```

Expected: test passes.

- [ ] **Step 6: Commit**

Run:

```powershell
git add src/generator
git commit -m "添加 TestConsole 最小白名单"
```

---

### Task 4: Canonical Type And Export Name Rules

**Files:**
- Create: `src/generator/VtkSharp.Generator.Core/Types/CanonicalType.cs`
- Create: `src/generator/VtkSharp.Generator.Core/Types/TypeCanonicalizer.cs`
- Create: `src/generator/VtkSharp.Generator.Core/Generation/ExportNameGenerator.cs`
- Create: `src/generator/VtkSharp.Generator.Tests/TypeCanonicalizerTests.cs`
- Create: `src/generator/VtkSharp.Generator.Tests/ExportNameGeneratorTests.cs`

- [ ] **Step 1: Add canonical type value**

Create `src/generator/VtkSharp.Generator.Core/Types/CanonicalType.cs`:

```csharp
namespace VtkSharp.Generator.Core.Types;

public readonly record struct CanonicalType(string Text)
{
    public override string ToString() => Text;
}
```

- [ ] **Step 2: Add type canonicalizer**

Create `src/generator/VtkSharp.Generator.Core/Types/TypeCanonicalizer.cs`:

```csharp
using System.Text.RegularExpressions;

namespace VtkSharp.Generator.Core.Types;

public sealed partial class TypeCanonicalizer
{
    public CanonicalType Canonicalize(string typeName)
    {
        var text = NormalizeWhitespace(typeName);
        text = NormalizeWin32Handle(text);
        text = NormalizeArray(text);
        text = NormalizePointer(text);
        text = NormalizeConstPointer(text);
        text = NormalizeConstScalarArray(text);
        return new CanonicalType(text);
    }

    private static string NormalizeWhitespace(string text)
        => WhitespaceRegex().Replace(text.Trim(), " ");

    private static string NormalizeWin32Handle(string text)
        => text switch
        {
            "HWND__ *" or "HWND__*" => "HWND",
            "HDC__ *" or "HDC__*" => "HDC",
            "HGLRC__ *" or "HGLRC__*" => "HGLRC",
            _ => text,
        };

    private static string NormalizeArray(string text)
        => ArraySpaceRegex().Replace(text, "$1[$2]");

    private static string NormalizePointer(string text)
        => PointerSpaceRegex().Replace(text, "$1*");

    private static string NormalizeConstPointer(string text)
    {
        var match = ConstPointerRegex().Match(text);
        return match.Success ? $"const {match.Groups["type"].Value}*" : text;
    }

    private static string NormalizeConstScalarArray(string text)
    {
        var match = ConstArrayRegex().Match(text);
        return match.Success ? $"const {match.Groups["type"].Value}[{match.Groups["count"].Value}]" : text;
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();

    [GeneratedRegex(@"^(.+?)\s+\[(\d+)\]$")]
    private static partial Regex ArraySpaceRegex();

    [GeneratedRegex(@"^(.+?)\s+\*$")]
    private static partial Regex PointerSpaceRegex();

    [GeneratedRegex(@"^(?<type>vtk\w+)\s+const\*$")]
    private static partial Regex ConstPointerRegex();

    [GeneratedRegex(@"^(?<type>double|float|int)\s+const\[(?<count>\d+)\]$")]
    private static partial Regex ConstArrayRegex();
}
```

- [ ] **Step 3: Add export name generator**

Create `src/generator/VtkSharp.Generator.Core/Generation/ExportNameGenerator.cs`:

```csharp
using System.Security.Cryptography;
using System.Text;
using VtkSharp.Generator.Core.Types;

namespace VtkSharp.Generator.Core.Generation;

public sealed class ExportNameGenerator
{
    public string Create(string className, string methodName, IReadOnlyList<CanonicalType> parameterTypes, bool hasOverloads)
    {
        if (!hasOverloads)
            return $"{className}_{methodName}";

        var suffix = string.Join("_", parameterTypes.Select(ToSuffix));
        return $"{className}_{methodName}_{suffix}";
    }

    public string CreateWithHash(string className, string methodName, string canonicalSignature)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(canonicalSignature));
        var hash = Convert.ToHexString(bytes).ToLowerInvariant()[..6];
        return $"{className}_{methodName}_h{hash}";
    }

    private static string ToSuffix(CanonicalType type)
    {
        var text = type.Text;
        if (text.EndsWith("*", StringComparison.Ordinal))
        {
            var core = text[..^1].Replace("const ", "", StringComparison.Ordinal);
            return text.StartsWith("const ", StringComparison.Ordinal) ? $"{core}ConstPtr" : $"{core}Ptr";
        }

        if (text.StartsWith("const ", StringComparison.Ordinal) && text.Contains('[', StringComparison.Ordinal))
            return text.Replace("const ", "", StringComparison.Ordinal).Replace("[", "ConstArray", StringComparison.Ordinal).Replace("]", "", StringComparison.Ordinal);

        if (text.Contains('[', StringComparison.Ordinal))
            return text.Replace("[", "Array", StringComparison.Ordinal).Replace("]", "", StringComparison.Ordinal);

        return text switch
        {
            "unsigned int" => "uint",
            "long long" => "long",
            "unsigned long long" => "ulong",
            "HWND" => "hwnd",
            "HDC" => "hdc",
            "HGLRC" => "hglrc",
            "const char*" => "constCharPtr",
            "char*" => "charPtr",
            "void*" => "voidPtr",
            _ => text.Replace(" ", "", StringComparison.Ordinal),
        };
    }
}
```

- [ ] **Step 4: Add canonicalizer tests**

Create `src/generator/VtkSharp.Generator.Tests/TypeCanonicalizerTests.cs`:

```csharp
using VtkSharp.Generator.Core.Types;

namespace VtkSharp.Generator.Tests;

public sealed class TypeCanonicalizerTests
{
    [Theory]
    [InlineData("vtkMapper *", "vtkMapper*")]
    [InlineData("vtkMapper const *", "const vtkMapper*")]
    [InlineData("char const *", "const char*")]
    [InlineData("double const[3]", "const double[3]")]
    [InlineData("double [3]", "double[3]")]
    [InlineData("HWND__ *", "HWND")]
    public void Canonicalize_NormalizesSupportedSpelling(string input, string expected)
    {
        var canonicalizer = new TypeCanonicalizer();
        Assert.Equal(expected, canonicalizer.Canonicalize(input).Text);
    }
}
```

- [ ] **Step 5: Add export name tests**

Create `src/generator/VtkSharp.Generator.Tests/ExportNameGeneratorTests.cs`:

```csharp
using VtkSharp.Generator.Core.Generation;
using VtkSharp.Generator.Core.Types;

namespace VtkSharp.Generator.Tests;

public sealed class ExportNameGeneratorTests
{
    [Fact]
    public void Create_WithoutOverloads_UsesClassAndMethod()
    {
        var generator = new ExportNameGenerator();
        var name = generator.Create("vtkActor", "SetMapper", [new("vtkMapper*")], hasOverloads: false);
        Assert.Equal("vtkActor_SetMapper", name);
    }

    [Fact]
    public void Create_WithOverloads_UsesParameterSuffix()
    {
        var generator = new ExportNameGenerator();
        var name = generator.Create("vtkActor", "SetPosition", [new("double"), new("double"), new("double")], hasOverloads: true);
        Assert.Equal("vtkActor_SetPosition_double_double_double", name);
    }

    [Fact]
    public void Create_WithArrayOverload_UsesArraySuffix()
    {
        var generator = new ExportNameGenerator();
        var name = generator.Create("vtkTransform", "SetMatrix", [new("const double[16]")], hasOverloads: true);
        Assert.Equal("vtkTransform_SetMatrix_doubleConstArray16", name);
    }
}
```

- [ ] **Step 6: Run tests**

Run:

```powershell
dotnet test src\generator\VtkSharp.Generator.Tests\VtkSharp.Generator.Tests.csproj
```

Expected: tests pass.

- [ ] **Step 7: Commit**

Run:

```powershell
git add src/generator
git commit -m "实现类型规范化和导出命名规则"
```

---

### Task 5: VTK Hierarchy Reader

**Files:**
- Create: `src/generator/VtkSharp.Generator.Core/Vtk/VtkHierarchyEntry.cs`
- Create: `src/generator/VtkSharp.Generator.Core/Vtk/VtkHierarchyReader.cs`
- Create: `src/generator/VtkSharp.Generator.Tests/VtkHierarchyReaderTests.cs`
- Create: `src/generator/VtkSharp.Generator.Tests/TestData/hierarchy/vtkRenderingCore-hierarchy.txt`

- [ ] **Step 1: Add hierarchy model**

Create `src/generator/VtkSharp.Generator.Core/Vtk/VtkHierarchyEntry.cs`:

```csharp
namespace VtkSharp.Generator.Core.Vtk;

public sealed record VtkHierarchyEntry(
    string ClassName,
    string BaseClassName,
    string Header,
    string Module);
```

- [ ] **Step 2: Add reader**

Create `src/generator/VtkSharp.Generator.Core/Vtk/VtkHierarchyReader.cs`:

```csharp
using System.Text.RegularExpressions;

namespace VtkSharp.Generator.Core.Vtk;

public sealed partial class VtkHierarchyReader
{
    public IReadOnlyDictionary<string, VtkHierarchyEntry> ReadDirectory(string directory)
    {
        var result = new Dictionary<string, VtkHierarchyEntry>(StringComparer.Ordinal);
        foreach (var file in Directory.GetFiles(directory, "vtk*-hierarchy.txt", SearchOption.TopDirectoryOnly))
        {
            foreach (var entry in ReadFile(file))
            {
                if (!result.ContainsKey(entry.ClassName))
                    result.Add(entry.ClassName, entry);
            }
        }

        return result;
    }

    public IReadOnlyList<VtkHierarchyEntry> ReadFile(string path)
    {
        return File.ReadLines(path)
            .Select(ParseLine)
            .Where(static entry => entry is not null)
            .Select(static entry => entry!)
            .ToList();
    }

    private static VtkHierarchyEntry? ParseLine(string line)
    {
        var match = ClassLineRegex().Match(line.Trim());
        if (!match.Success)
            return null;

        return new VtkHierarchyEntry(
            match.Groups["class"].Value,
            match.Groups["base"].Value,
            match.Groups["header"].Value,
            match.Groups["module"].Value);
    }

    [GeneratedRegex(@"^\s*(?<class>vtk[\w-]+)\s*:\s*(?<base>vtk[\w-]+)\s*;\s*(?<header>vtk[\w-]+\.h)\s*;\s*(?<module>vtk[\w-]+)\s*$")]
    private static partial Regex ClassLineRegex();
}
```

- [ ] **Step 3: Add hierarchy test**

Create `src/generator/VtkSharp.Generator.Tests/VtkHierarchyReaderTests.cs`:

```csharp
using VtkSharp.Generator.Core.Vtk;

namespace VtkSharp.Generator.Tests;

public sealed class VtkHierarchyReaderTests
{
    [Fact]
    public void ReadFile_ParsesClassLine()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "TestData", "hierarchy", "vtkRenderingCore-hierarchy.txt");
        var reader = new VtkHierarchyReader();

        var entries = reader.ReadFile(path);

        var actor = Assert.Single(entries, entry => entry.ClassName == "vtkActor");
        Assert.Equal("vtkProp3D", actor.BaseClassName);
        Assert.Equal("vtkActor.h", actor.Header);
        Assert.Equal("vtkRenderingCore", actor.Module);
    }
}
```

Create `src/generator/VtkSharp.Generator.Tests/TestData/hierarchy/vtkRenderingCore-hierarchy.txt`:

```text
vtkActor : vtkProp3D ; vtkActor.h ; vtkRenderingCore
vtkRenderer : vtkViewport ; vtkRenderer.h ; vtkRenderingCore
```

- [ ] **Step 4: Run tests**

Run:

```powershell
dotnet test src\generator\VtkSharp.Generator.Tests\VtkSharp.Generator.Tests.csproj
```

Expected: tests pass.

- [ ] **Step 5: Commit**

Run:

```powershell
git add src/generator
git commit -m "实现 VTK hierarchy 读取"
```

---

### Task 6: Inspect-Class CLI

**Files:**
- Create: `src/generator/VtkSharp.Generator.Core/Inspection/VtkClassInspector.cs`
- Modify: `src/generator/VtkSharp.Generator.Cli/Program.cs`

- [ ] **Step 1: Add inspector DTOs and service skeleton**

Create `src/generator/VtkSharp.Generator.Core/Inspection/VtkClassInspector.cs`:

```csharp
using VtkSharp.Generator.Core.Types;

namespace VtkSharp.Generator.Core.Inspection;

public sealed record InspectedClass(
    string Name,
    IReadOnlyList<InspectedFunction> Functions);

public sealed record InspectedFunction(
    string Name,
    string CppSignature,
    string ReturnType,
    IReadOnlyList<InspectedParameter> Parameters,
    bool IsSupported);

public sealed record InspectedParameter(
    string Type,
    string Name);

public sealed class VtkClassInspector
{
    private readonly TypeCanonicalizer _canonicalizer = new();

    public InspectedClass InspectSynthetic(string className, IReadOnlyList<(string Name, string ReturnType, IReadOnlyList<(string Type, string Name)> Parameters)> functions)
    {
        var inspected = functions.Select(function =>
        {
            var parameters = function.Parameters
                .Select(parameter => new InspectedParameter(_canonicalizer.Canonicalize(parameter.Type).Text, parameter.Name))
                .ToList();

            var signature = $"{_canonicalizer.Canonicalize(function.ReturnType)} {function.Name}(" +
                            string.Join(", ", parameters.Select(parameter => $"{parameter.Type} {parameter.Name}")) +
                            ")";

            return new InspectedFunction(function.Name, signature, _canonicalizer.Canonicalize(function.ReturnType).Text, parameters, IsSupported: true);
        }).ToList();

        return new InspectedClass(className, inspected);
    }
}
```

This synthetic method keeps CLI formatting testable before wiring real CppAst parsing. Real CppAst inspection is added in Task 7.

- [ ] **Step 2: Add CLI command skeleton**

Replace `src/generator/VtkSharp.Generator.Cli/Program.cs`:

```csharp
using System.CommandLine;
using System.Text.Json;
using VtkSharp.Generator.Core.Inspection;

var classArgument = new Argument<string>("class-name");
var formatOption = new Option<string>("--format", () => "text")
{
    Description = "Output format: text or json"
};

var inspectClassCommand = new Command("inspect-class", "Inspect a VTK class")
{
    classArgument,
    formatOption,
};

inspectClassCommand.SetAction(parseResult =>
{
    var className = parseResult.GetValue(classArgument)!;
    var format = parseResult.GetValue(formatOption)!;
    var inspector = new VtkClassInspector();
    var inspected = inspector.InspectSynthetic(className,
    [
        ("SetMapper", "void", [("vtkMapper *", "mapper")]),
    ]);

    if (format.Equals("json", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine(JsonSerializer.Serialize(inspected, new JsonSerializerOptions { WriteIndented = true }));
    }
    else
    {
        Console.WriteLine(inspected.Name);
        foreach (var function in inspected.Functions)
            Console.WriteLine($"  {function.CppSignature}");
    }
});

var rootCommand = new RootCommand("VtkSharp binding generator")
{
    inspectClassCommand,
};

return rootCommand.Parse(args).Invoke();
```

- [ ] **Step 3: Run CLI smoke test**

Run:

```powershell
dotnet run --project src\generator\VtkSharp.Generator.Cli -- inspect-class vtkActor --format text
```

Expected output contains:

```text
vtkActor
  void SetMapper(vtkMapper* mapper)
```

- [ ] **Step 4: Commit**

Run:

```powershell
git add src/generator
git commit -m "添加 inspect-class 命令骨架"
```

---

### Task 7: CppAst-Based Inspection And Validation

**Files:**
- Modify: `src/generator/VtkSharp.Generator.Core/Inspection/VtkClassInspector.cs`
- Create: `src/generator/VtkSharp.Generator.Core/Validation/WhitelistValidator.cs`
- Modify: `src/generator/VtkSharp.Generator.Cli/Program.cs`

- [ ] **Step 1: Add real CppAst inspection method**

Extend `VtkClassInspector` with a method that parses one header:

```csharp
// Add using CppAst;

public InspectedClass InspectHeader(string includeDirectory, string headerFileName, string className)
{
    var options = new CppParserOptions();
    options.ConfigureForWindowsMsvc(CppTargetCpu.X86_64, CppVisualStudioVersion.VS2022);
    options.IncludeFolders.Add(includeDirectory);

    var headerPath = Path.Combine(includeDirectory, headerFileName);
    var compilation = CppParser.ParseFile(headerPath, options);
    if (compilation.HasErrors)
        throw new InvalidOperationException(string.Join(Environment.NewLine, compilation.Diagnostics));

    var cppClass = compilation.Classes.FirstOrDefault(item => item.Name == className)
        ?? throw new InvalidOperationException($"Class '{className}' was not found in '{headerFileName}'.");

    var functions = cppClass.Functions
        .Where(static function =>
            function.Visibility == CppVisibility.Public &&
            !function.IsConstructor &&
            !function.IsDestructor &&
            !function.IsStatic &&
            !function.IsFunctionTemplate)
        .Select(function =>
        {
            var parameters = function.Parameters
                .Select((parameter, index) =>
                {
                    var name = string.IsNullOrWhiteSpace(parameter.Name) ? $"_arg{index + 1}" : parameter.Name;
                    return new InspectedParameter(_canonicalizer.Canonicalize(parameter.Type.FullName).Text, name);
                })
                .ToList();

            var returnType = _canonicalizer.Canonicalize(function.ReturnType.FullName).Text;
            var signature = $"{returnType} {function.Name}(" + string.Join(", ", parameters.Select(p => $"{p.Type} {p.Name}")) + ")";
            return new InspectedFunction(function.Name, signature, returnType, parameters, IsSupported: true);
        })
        .ToList();

    return new InspectedClass(className, functions);
}
```

- [ ] **Step 2: Add validator skeleton**

Create `src/generator/VtkSharp.Generator.Core/Validation/WhitelistValidator.cs`:

```csharp
using VtkSharp.Generator.Core.Inspection;
using VtkSharp.Generator.Core.Whitelist;

namespace VtkSharp.Generator.Core.Validation;

public sealed record ValidationDiagnostic(string Message);

public sealed record ValidationResult(IReadOnlyList<ValidationDiagnostic> Diagnostics)
{
    public bool Success => Diagnostics.Count == 0;
}

public sealed class WhitelistValidator
{
    public ValidationResult Validate(WhitelistDocument document, IReadOnlyDictionary<string, InspectedClass> inspectedClasses)
    {
        var diagnostics = new List<ValidationDiagnostic>();

        foreach (var whitelistClass in document.Classes)
        {
            if (!inspectedClasses.TryGetValue(whitelistClass.Name, out var inspectedClass))
            {
                diagnostics.Add(new ValidationDiagnostic($"Class '{whitelistClass.Name}' was not inspected."));
                continue;
            }

            foreach (var function in whitelistClass.Functions)
            {
                var matches = inspectedClass.Functions
                    .Where(item => item.Name == function.Name)
                    .Where(item => item.ReturnType == function.Return.Type)
                    .Where(item => item.Parameters.Select(p => p.Type).SequenceEqual(function.Parameters.Select(p => p.Type)))
                    .ToList();

                if (matches.Count == 0)
                    diagnostics.Add(new ValidationDiagnostic($"Function '{whitelistClass.Name}.{function.Name}' was not found."));
                else if (matches.Count > 1)
                    diagnostics.Add(new ValidationDiagnostic($"Function '{whitelistClass.Name}.{function.Name}' matched multiple overloads."));
            }
        }

        return new ValidationResult(diagnostics);
    }
}
```

- [ ] **Step 3: Add validate-whitelist CLI placeholder**

Extend `Program.cs` with `validate-whitelist` that loads whitelist files and prints a placeholder success. In the next task wire actual config/hierarchy paths:

```csharp
var configOption = new Option<FileInfo>("--config") { Description = "Generator config file" };
var validateCommand = new Command("validate-whitelist", "Validate whitelist")
{
    configOption,
};

validateCommand.SetAction(_ =>
{
    Console.WriteLine("Whitelist validation command is available.");
});

rootCommand.Subcommands.Add(validateCommand);
```

Adjust syntax to match the installed `System.CommandLine` API if needed.

- [ ] **Step 4: Build**

Run:

```powershell
dotnet build src\generator\VtkSharp.Generator.sln
```

Expected: build succeeds.

- [ ] **Step 5: Commit**

Run:

```powershell
git add src/generator
git commit -m "接入 CppAst 检查和白名单校验模型"
```

---

### Task 8: Emit Generated Files To Temporary Output

**Files:**
- Create: `src/generator/VtkSharp.Generator.Core/Generation/CSharpBindingEmitter.cs`
- Create: `src/generator/VtkSharp.Generator.Core/Generation/CppExportEmitter.cs`
- Create: `src/generator/VtkSharp.Generator.Core/Generation/CMakeModulesEmitter.cs`
- Modify: `src/generator/VtkSharp.Generator.Cli/Program.cs`

- [ ] **Step 1: Add C# emitter**

Create `src/generator/VtkSharp.Generator.Core/Generation/CSharpBindingEmitter.cs`:

```csharp
using System.Text;
using VtkSharp.Generator.Core.Whitelist;

namespace VtkSharp.Generator.Core.Generation;

public sealed class CSharpBindingEmitter
{
    public string Emit(string namespaceName, string className, string baseClassName, IReadOnlyList<WhitelistFunction> functions)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("// This file is generated by VtkSharp.Generator. Do not edit manually.");
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Diagnostics;");
        sb.AppendLine("using System.Runtime.InteropServices;");
        sb.AppendLine();
        sb.AppendLine($"namespace {namespaceName};");
        sb.AppendLine();
        sb.AppendLine($"public unsafe partial class {className} : {baseClassName}");
        sb.AppendLine("{");
        sb.AppendLine($"    protected {className}(nint nativePointer, bool ownsReference) : base(nativePointer, ownsReference) {{ }}");
        sb.AppendLine($"    public new static {className} New() => new({className}_New(), ownsReference: true);");
        sb.AppendLine($"    public new static {className} WeakReference(nint nativePointer) => new(nativePointer, ownsReference: false);");
        sb.AppendLine();
        sb.AppendLine("    #region Interop");
        sb.AppendLine("    [DllImport(InteropInfo.NativeLibraryName)]");
        sb.AppendLine($"    private static extern nint {className}_New();");
        sb.AppendLine("    #endregion");
        sb.AppendLine("}");
        return sb.ToString();
    }
}
```

This first emitter intentionally emits `New()` only. Add function wrappers in the next implementation iteration after validation proves class matching.

- [ ] **Step 2: Add C++ emitter**

Create `src/generator/VtkSharp.Generator.Core/Generation/CppExportEmitter.cs`:

```csharp
using System.Text;

namespace VtkSharp.Generator.Core.Generation;

public sealed class CppExportEmitter
{
    public string Emit(string className, IReadOnlyList<string> includeClassNames)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("// This file is generated by VtkSharp.Generator. Do not edit manually.");
        sb.AppendLine("#include \"vtksharp_api.h\"");
        foreach (var include in includeClassNames.Order(StringComparer.Ordinal))
            sb.AppendLine($"#include <{include}.h>");
        sb.AppendLine($"#include <{className}.h>");
        sb.AppendLine();
        sb.AppendLine($"VTKSHARP_API {className}* {className}_New() {{ return {className}::New(); }}");
        return sb.ToString();
    }
}
```

- [ ] **Step 3: Add CMake modules emitter**

Create `src/generator/VtkSharp.Generator.Core/Generation/CMakeModulesEmitter.cs`:

```csharp
using System.Text;

namespace VtkSharp.Generator.Core.Generation;

public sealed class CMakeModulesEmitter
{
    public string Emit(IReadOnlyList<string> vtkModules)
    {
        var components = vtkModules.Select(static module => module.StartsWith("vtk", StringComparison.Ordinal) ? module[3..] : module)
            .Order(StringComparer.Ordinal)
            .ToList();

        var sb = new StringBuilder();
        sb.AppendLine("# This file is generated by VtkSharp.Generator. Do not edit manually.");
        sb.AppendLine("set(VTKSHARP_VTK_COMPONENTS");
        foreach (var component in components)
            sb.AppendLine($"  {component}");
        sb.AppendLine(")");
        sb.AppendLine();
        sb.AppendLine("set(VTKSHARP_VTK_TARGETS");
        foreach (var component in components)
            sb.AppendLine($"  VTK::{component}");
        sb.AppendLine(")");
        return sb.ToString();
    }
}
```

- [ ] **Step 4: Add generate CLI with output root**

Extend `Program.cs` with:

```csharp
var outputRootOption = new Option<DirectoryInfo>("--output-root") { Description = "Temporary output root" };
var generateCommand = new Command("generate", "Generate bindings")
{
    outputRootOption,
};

generateCommand.SetAction(parseResult =>
{
    var outputRoot = parseResult.GetValue(outputRootOption)
        ?? throw new InvalidOperationException("--output-root is required for the first MVP.");

    Directory.CreateDirectory(outputRoot.FullName);
    Console.WriteLine($"Generated files will be written to: {outputRoot.FullName}");
});
```

- [ ] **Step 5: Run CLI generate smoke test**

Run:

```powershell
dotnet run --project src\generator\VtkSharp.Generator.Cli -- generate --output-root src\generator\out\roundtrip
```

Expected output contains:

```text
Generated files will be written to:
```

- [ ] **Step 6: Commit**

Run:

```powershell
git add src/generator
git commit -m "添加临时目录生成输出骨架"
```

---

### Task 9: Round-Trip Validation Against TestConsole Baseline

**Files:**
- Modify: `src/generator/VtkSharp.Generator.Cli/Program.cs`
- Modify: `src/generator/VtkSharp.Generator.Core/Generation/CSharpBindingEmitter.cs`
- Modify: `src/generator/VtkSharp.Generator.Core/Generation/CppExportEmitter.cs`
- Modify: `src/generator/VtkSharp.Generator.Core/Generation/CMakeModulesEmitter.cs`

- [ ] **Step 1: Wire generate command to emit TestConsole closure**

For the MVP, `generate --output-root <dir>` must create at least:

```text
<dir>/bindings/VtkSharp/vtkFiltersSources/vtkConeSource_gen.cs
<dir>/bindings/VtkSharp/vtkRenderingCore/vtkActor_gen.cs
<dir>/native/src/vtkFiltersSources/vtkConeSource_export_gen.cpp
<dir>/native/src/vtkRenderingCore/vtkActor_export_gen.cpp
<dir>/native/vtksharp.modules.generated.cmake
```

Use the existing whitelist files as input. It is acceptable in this task to emit class-level `New()` first and then iteratively add wrapper methods.

- [ ] **Step 2: Run generator to temp output**

Run:

```powershell
Remove-Item -Recurse -Force src\generator\out\roundtrip -ErrorAction SilentlyContinue
dotnet run --project src\generator\VtkSharp.Generator.Cli -- generate --output-root src\generator\out\roundtrip
```

Expected: temp files exist under `src/generator/out/roundtrip`.

- [ ] **Step 3: Compare representative files**

Run:

```powershell
git diff --no-index src\bindings\VtkSharp\VtkSharp\vtkCommonExecutionModel\vtkAlgorithm_gen.cs src\generator\out\roundtrip\bindings\VtkSharp\vtkCommonExecutionModel\vtkAlgorithm_gen.cs
```

Expected: differences are reviewed manually. Formatting should move toward `vtkAlgorithm_gen.cs` layout with `#region Interop`.

- [ ] **Step 4: Build generator**

Run:

```powershell
dotnet build src\generator\VtkSharp.Generator.sln
```

Expected: build succeeds.

- [ ] **Step 5: Commit**

Run:

```powershell
git add src/generator
git commit -m "生成 TestConsole 最小闭环到临时目录"
```

---

### Task 10: Manual Smoke Validation With Existing TestConsole

**Files:**
- No code changes expected.

- [ ] **Step 1: Build native library**

Run:

```powershell
cmake --build src\native\out\build\windows-x64 --config Release
```

Expected: native build succeeds. If this preset is not configured, run the existing project-specific configure command first.

- [ ] **Step 2: Build managed solution**

Run:

```powershell
dotnet build src\bindings\VtkSharp\VtkSharp.slnx
```

Expected: managed build succeeds.

- [ ] **Step 3: Run TestConsole manually**

Run:

```powershell
dotnet run --project src\bindings\VtkSharp\TestConsole\TestConsole.csproj
```

Expected: VTK render window opens and the program can be closed manually. Because this starts an interactive VTK window, do not run it unattended in CI.

- [ ] **Step 4: Record validation result**

Append a short note to the implementation log or PR description:

```text
Validation:
- dotnet build src/generator/VtkSharp.Generator.sln: passed
- dotnet build src/bindings/VtkSharp/VtkSharp.slnx: passed
- TestConsole: manually ran and rendered VTK window
```

- [ ] **Step 5: Commit validation docs only if a tracked document was updated**

If no files changed, do not create an empty commit.

---

## Self-Review

Spec coverage:

- Configuration split and priority: Tasks 2.
- YAML schema and VS Code schema files: Task 2.
- Function-level whitelist: Task 3.
- TestConsole minimal closure: Tasks 3, 9, 10.
- CppAst canonical types: Task 4.
- Export naming rules: Task 4.
- Hierarchy parsing: Task 5.
- CLI inspect/validate/generate: Tasks 6, 7, 8, 9.
- Temporary output and round-trip diff: Task 9.
- No automatic extension shell creation in phase one: Task 8 does not create `vtkXxx.cs` or `vtkXxx_export.cpp`.

Known gaps intentionally left for later phases:

- Full `generate --check`.
- Orphan cleanup.
- Build log parsing.
- CppAst parsing performance: the MVP may parse classes/headers repeatedly while the end-to-end flow is being stabilized. After validate/generate/round-trip/build/smoke test are working, refactor inspection to batch-parse all required classes and cache parsed header/module results.
- Automatic example translation.
- Mature extension shell creation.
- Full old BrdiVtkNet whitelist import.
