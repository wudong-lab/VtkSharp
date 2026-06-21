# VtkSharp MVP Data Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the first VtkSharp MVP slice that creates a triangle `vtkPolyData` from C#, checks point count, cell count, and bounds, and validates the basic ownership/Dispose path.

**Architecture:** Keep one managed assembly (`VtkSharp.dll`) and one native shim (`vtksharp_native.dll`). Use CMake to build a hand-written native C ABI shim for the first API slice, use a minimal manifest-driven generator to emit C# P/Invoke and thin wrappers, and keep generated code checked in.

**Tech Stack:** C# `netstandard2.0` for `VtkSharp`, .NET SDK test projects, CMake + VTK 9.x dynamic libraries, C++17 for native shim, YAML manifest files.

---

## File Structure

Create or modify these files:

- Create `Directory.Build.props`: common C# project settings.
- Create `VtkSharp.sln`: solution containing managed library, generator, and tests.
- Create `src/VtkSharp/VtkSharp.csproj`: core managed binding package targeting `netstandard2.0`.
- Create `src/VtkSharp/VtkSharpBounds3.cs`: first non-reference-counted value type.
- Create `src/VtkSharp/Native/VtkSharpNativeLibrary.cs`: shared native library name.
- Create `src/VtkSharp/Native/NativeMethods.cs`: generated P/Invoke declarations.
- Create `src/VtkSharp/Generated/*.cs`: generated thin wrappers for first VTK types.
- Create `generator/VtkSharp.Generator.Core/VtkSharp.Generator.Core.csproj`: manifest model, YAML loading, wrapper emitter.
- Create `generator/VtkSharp.Generator.Core/*.cs`: focused generator units.
- Create `generator/VtkSharp.Generator.Cli/VtkSharp.Generator.Cli.csproj`: command-line entry point.
- Create `generator/VtkSharp.Generator.Cli/Program.cs`: `generate` command for resolved manifest.
- Create `bindings/vtk-9.6/profiles.yaml`: `mvp-data` profile.
- Create `bindings/vtk-9.6/examples/engineering.yaml`: triangle MVP scenario.
- Create `bindings/vtk-9.6/overrides/CommonCore.overrides.yaml`: reviewed base object methods.
- Create `bindings/vtk-9.6/overrides/CommonDataModel.overrides.yaml`: reviewed data model methods.
- Create `bindings/vtk-9.6/resolved/mvp-data.resolved.yaml`: resolved manifest consumed by generator.
- Create `native/CMakeLists.txt`: native shim build.
- Create `native/CMakePresets.json`: Windows x64 default configure preset.
- Create `native/include/vtksharp_api.h`: export macro.
- Create `native/src/manual/vtksharp_core.cpp`: hand-written C ABI shim.
- Create `tests/VtkSharp.Tests/VtkSharp.Tests.csproj`: managed-only unit tests.
- Create `tests/VtkSharp.Tests/VtkSharpBounds3Tests.cs`: value type tests.
- Create `tests/VtkSharp.Tests/WrapperLifetimeTests.cs`: managed wrapper lifetime tests using fake pointer construction where possible.
- Create `tests/VtkSharp.NativeSmokeTests/VtkSharp.NativeSmokeTests.csproj`: native smoke tests.
- Create `tests/VtkSharp.NativeSmokeTests/TrianglePolyDataSmokeTests.cs`: triangle `vtkPolyData` test.
- Create `scripts/test.ps1`: local test runner that accepts VTK bin/native output paths.

## Task 1: Create .NET Skeleton

**Files:**
- Create: `Directory.Build.props`
- Create: `VtkSharp.sln`
- Create: `src/VtkSharp/VtkSharp.csproj`
- Create: `generator/VtkSharp.Generator.Core/VtkSharp.Generator.Core.csproj`
- Create: `generator/VtkSharp.Generator.Cli/VtkSharp.Generator.Cli.csproj`
- Create: `tests/VtkSharp.Tests/VtkSharp.Tests.csproj`
- Create: `tests/VtkSharp.NativeSmokeTests/VtkSharp.NativeSmokeTests.csproj`

- [ ] **Step 1: Create project folders**

Run:

```powershell
New-Item -ItemType Directory -Force `
  src/VtkSharp, `
  generator/VtkSharp.Generator.Core, `
  generator/VtkSharp.Generator.Cli, `
  tests/VtkSharp.Tests, `
  tests/VtkSharp.NativeSmokeTests
```

Expected: directories exist.

- [ ] **Step 2: Create `Directory.Build.props`**

Write:

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

- [ ] **Step 3: Create managed project files**

Create `src/VtkSharp/VtkSharp.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
    <AssemblyName>VtkSharp</AssemblyName>
    <RootNamespace>VtkSharp</RootNamespace>
  </PropertyGroup>
</Project>
```

Create `generator/VtkSharp.Generator.Core/VtkSharp.Generator.Core.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <AssemblyName>VtkSharp.Generator.Core</AssemblyName>
    <RootNamespace>VtkSharp.Generator.Core</RootNamespace>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="YamlDotNet" Version="16.3.0" />
  </ItemGroup>
</Project>
```

Create `generator/VtkSharp.Generator.Cli/VtkSharp.Generator.Cli.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <AssemblyName>VtkSharp.Generator.Cli</AssemblyName>
    <RootNamespace>VtkSharp.Generator.Cli</RootNamespace>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\VtkSharp.Generator.Core\VtkSharp.Generator.Core.csproj" />
  </ItemGroup>
</Project>
```

- [ ] **Step 4: Create test project files**

Create `tests/VtkSharp.Tests/VtkSharp.Tests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.11.1" />
    <PackageReference Include="xunit" Version="2.9.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2" />
    <ProjectReference Include="..\..\src\VtkSharp\VtkSharp.csproj" />
  </ItemGroup>
</Project>
```

Create `tests/VtkSharp.NativeSmokeTests/VtkSharp.NativeSmokeTests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.11.1" />
    <PackageReference Include="xunit" Version="2.9.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2" />
    <ProjectReference Include="..\..\src\VtkSharp\VtkSharp.csproj" />
  </ItemGroup>
</Project>
```

- [ ] **Step 5: Create solution and add projects**

Run:

```powershell
dotnet new sln -n VtkSharp
dotnet sln VtkSharp.sln add src/VtkSharp/VtkSharp.csproj
dotnet sln VtkSharp.sln add generator/VtkSharp.Generator.Core/VtkSharp.Generator.Core.csproj
dotnet sln VtkSharp.sln add generator/VtkSharp.Generator.Cli/VtkSharp.Generator.Cli.csproj
dotnet sln VtkSharp.sln add tests/VtkSharp.Tests/VtkSharp.Tests.csproj
dotnet sln VtkSharp.sln add tests/VtkSharp.NativeSmokeTests/VtkSharp.NativeSmokeTests.csproj
```

Expected: all projects are listed by `dotnet sln VtkSharp.sln list`.

- [ ] **Step 6: Build skeleton**

Run:

```powershell
dotnet build VtkSharp.sln
```

Expected: build succeeds.

- [ ] **Step 7: Commit skeleton**

```powershell
git add Directory.Build.props VtkSharp.sln src generator tests
git commit -m "chore: 创建 VtkSharp MVP 工程骨架"
```

## Task 2: Add Manifest Inputs

**Files:**
- Create: `bindings/vtk-9.6/profiles.yaml`
- Create: `bindings/vtk-9.6/examples/engineering.yaml`
- Create: `bindings/vtk-9.6/overrides/CommonCore.overrides.yaml`
- Create: `bindings/vtk-9.6/overrides/CommonDataModel.overrides.yaml`
- Create: `bindings/vtk-9.6/resolved/mvp-data.resolved.yaml`

- [ ] **Step 1: Create manifest directories**

Run:

```powershell
New-Item -ItemType Directory -Force `
  bindings/vtk-9.6/examples, `
  bindings/vtk-9.6/generated, `
  bindings/vtk-9.6/overrides, `
  bindings/vtk-9.6/decisions, `
  bindings/vtk-9.6/resolved
```

- [ ] **Step 2: Create `profiles.yaml`**

Write:

```yaml
vtkVersion: "9.6"

profiles:
  mvp-data:
    modules:
      - CommonCore
      - CommonDataModel
    classes:
      - vtkObjectBase
      - vtkObject
      - vtkDataObject
      - vtkDataSet
      - vtkPointSet
      - vtkPolyData
      - vtkAbstractCellArray
      - vtkCellArray
      - vtkPoints
```

- [ ] **Step 3: Create engineering scenario manifest**

Create `bindings/vtk-9.6/examples/engineering.yaml`:

```yaml
examples:
  TrianglePolyDataSmoke:
    source: engineering/scenarios/TrianglePolyDataSmoke
    category: data-model
    priority: mvp
    expectedApis:
      classes:
        - vtkPoints
        - vtkCellArray
        - vtkPolyData
      methods:
        - vtkPoints.FromArray
        - vtkCellArray.FromTriangles
        - vtkPolyData.SetPoints
        - vtkPolyData.SetPolys
        - vtkPolyData.GetNumberOfPoints
        - vtkPolyData.GetNumberOfCells
        - vtkPolyData.GetBounds
```

- [ ] **Step 4: Create reviewed CommonCore override manifest**

Create `bindings/vtk-9.6/overrides/CommonCore.overrides.yaml`:

```yaml
vtkVersion: "9.6"
module: CommonCore

classes:
  vtkObjectBase:
    module: CommonCore
    baseType:
    methods:
      - name: Register
        visibility: public
        parameters:
          - name: owner
            type: vtkObjectBase
            nullable: true
            ownership: borrowed
      - name: UnRegister
        visibility: public
        parameters:
          - name: owner
            type: vtkObjectBase
            nullable: true
            ownership: borrowed
      - name: GetReferenceCount
        visibility: public
        returns:
          type: int

  vtkObject:
    module: CommonCore
    baseType: vtkObjectBase
    methods:
      - name: GetMTime
        visibility: public
        returns:
          type: vtkMTimeType
      - name: Modified
        visibility: public
```

- [ ] **Step 5: Create reviewed CommonDataModel override manifest**

Create `bindings/vtk-9.6/overrides/CommonDataModel.overrides.yaml`:

```yaml
vtkVersion: "9.6"
module: CommonDataModel

classes:
  vtkDataObject:
    module: CommonDataModel
    baseType: vtkObject
    methods: []

  vtkDataSet:
    module: CommonDataModel
    baseType: vtkDataObject
    methods:
      - name: GetBounds
        nativeName: GetBounds
        customShim: true
        visibility: public
        returns:
          managedType: VtkSharpBounds3
          marshal: fixedArray
          elementType: double
          fixedLength: 6

  vtkPointSet:
    module: CommonDataModel
    baseType: vtkDataSet
    methods: []

  vtkPolyData:
    module: CommonDataModel
    baseType: vtkPointSet
    metadata:
      coverageSources:
        - examples/engineering/TrianglePolyDataSmoke
    methods:
      - name: New
        visibility: public
        returns:
          type: vtkPolyData
          ownership: owned
          nullable: false
      - name: SetPoints
        visibility: public
        parameters:
          - name: points
            type: vtkPoints
            ownership: borrowed
            nullable: false
      - name: SetPolys
        visibility: public
        parameters:
          - name: polys
            type: vtkCellArray
            ownership: borrowed
            nullable: false
      - name: GetNumberOfPoints
        visibility: public
        returns:
          type: vtkIdType
      - name: GetNumberOfCells
        visibility: public
        returns:
          type: vtkIdType

  vtkAbstractCellArray:
    module: CommonDataModel
    baseType: vtkObject
    methods: []

  vtkCellArray:
    module: CommonDataModel
    baseType: vtkAbstractCellArray
    methods:
      - name: New
        visibility: public
        returns:
          type: vtkCellArray
          ownership: owned
          nullable: false
      - name: GetNumberOfCells
        visibility: public
        returns:
          type: vtkIdType
      - name: FromTriangles
        customShim: true
        visibility: public
        overloadId: long-array
        parameters:
          - name: ids
            type: long[]
            marshal: array
            direction: in
            multipleOf: 3
            notNull: true
        returns:
          type: vtkCellArray
          ownership: owned
          nullable: false
      - name: FromTriangles
        customShim: true
        visibility: public
        overloadId: int-array
        parameters:
          - name: ids
            type: int[]
            marshal: array
            direction: in
            multipleOf: 3
            notNull: true
        returns:
          type: vtkCellArray
          ownership: owned
          nullable: false

  vtkPoints:
    module: CommonDataModel
    baseType: vtkObject
    methods:
      - name: New
        visibility: public
        returns:
          type: vtkPoints
          ownership: owned
          nullable: false
      - name: GetNumberOfPoints
        visibility: public
        returns:
          type: vtkIdType
      - name: FromArray
        customShim: true
        visibility: public
        parameters:
          - name: xyz
            type: double[]
            marshal: array
            direction: in
            multipleOf: 3
            notNull: true
        returns:
          type: vtkPoints
          ownership: owned
          nullable: false
```

- [ ] **Step 6: Create initial resolved manifest**

Create `bindings/vtk-9.6/resolved/mvp-data.resolved.yaml`:

```yaml
vtkVersion: "9.6"
profile: mvp-data
modules:
  - CommonCore
  - CommonDataModel
classes:
  vtkObjectBase:
    module: CommonCore
    baseType:
    methods:
      - name: Register
        visibility: public
        parameters:
          - name: owner
            type: vtkObjectBase
            nullable: true
            ownership: borrowed
      - name: UnRegister
        visibility: public
        parameters:
          - name: owner
            type: vtkObjectBase
            nullable: true
            ownership: borrowed
      - name: GetReferenceCount
        visibility: public
        returns:
          type: int

  vtkObject:
    module: CommonCore
    baseType: vtkObjectBase
    methods:
      - name: GetMTime
        visibility: public
        returns:
          type: vtkMTimeType
      - name: Modified
        visibility: public

  vtkDataObject:
    module: CommonDataModel
    baseType: vtkObject
    methods: []

  vtkDataSet:
    module: CommonDataModel
    baseType: vtkDataObject
    methods:
      - name: GetBounds
        nativeName: GetBounds
        customShim: true
        visibility: public
        returns:
          managedType: VtkSharpBounds3
          marshal: fixedArray
          elementType: double
          fixedLength: 6

  vtkPointSet:
    module: CommonDataModel
    baseType: vtkDataSet
    methods: []

  vtkPolyData:
    module: CommonDataModel
    baseType: vtkPointSet
    metadata:
      coverageSources:
        - examples/engineering/TrianglePolyDataSmoke
    methods:
      - name: New
        visibility: public
        returns:
          type: vtkPolyData
          ownership: owned
          nullable: false
      - name: SetPoints
        visibility: public
        parameters:
          - name: points
            type: vtkPoints
            ownership: borrowed
            nullable: false
      - name: SetPolys
        visibility: public
        parameters:
          - name: polys
            type: vtkCellArray
            ownership: borrowed
            nullable: false
      - name: GetNumberOfPoints
        visibility: public
        returns:
          type: vtkIdType
      - name: GetNumberOfCells
        visibility: public
        returns:
          type: vtkIdType

  vtkAbstractCellArray:
    module: CommonDataModel
    baseType: vtkObject
    methods: []

  vtkCellArray:
    module: CommonDataModel
    baseType: vtkAbstractCellArray
    methods:
      - name: New
        visibility: public
        returns:
          type: vtkCellArray
          ownership: owned
          nullable: false
      - name: GetNumberOfCells
        visibility: public
        returns:
          type: vtkIdType
      - name: FromTriangles
        customShim: true
        visibility: public
        overloadId: long-array
        parameters:
          - name: ids
            type: long[]
            marshal: array
            direction: in
            multipleOf: 3
            notNull: true
        returns:
          type: vtkCellArray
          ownership: owned
          nullable: false
      - name: FromTriangles
        customShim: true
        visibility: public
        overloadId: int-array
        parameters:
          - name: ids
            type: int[]
            marshal: array
            direction: in
            multipleOf: 3
            notNull: true
        returns:
          type: vtkCellArray
          ownership: owned
          nullable: false

  vtkPoints:
    module: CommonDataModel
    baseType: vtkObject
    methods:
      - name: New
        visibility: public
        returns:
          type: vtkPoints
          ownership: owned
          nullable: false
      - name: GetNumberOfPoints
        visibility: public
        returns:
          type: vtkIdType
      - name: FromArray
        customShim: true
        visibility: public
        parameters:
          - name: xyz
            type: double[]
            marshal: array
            direction: in
            multipleOf: 3
            notNull: true
        returns:
          type: vtkPoints
          ownership: owned
          nullable: false
```

The resolved file is intentionally checked in so the generator input is deterministic.

- [ ] **Step 7: Commit manifest inputs**

```powershell
git add bindings/vtk-9.6
git commit -m "docs: 添加 MVP 数据绑定 manifest"
```

## Task 3: Implement Managed Runtime Primitives

**Files:**
- Create: `src/VtkSharp/VtkSharpBounds3.cs`
- Create: `src/VtkSharp/Native/VtkSharpNativeLibrary.cs`
- Create: `src/VtkSharp/Native/NativeMethods.cs`
- Create: `src/VtkSharp/Generated/vtkObjectBase.cs`
- Test: `tests/VtkSharp.Tests/VtkSharpBounds3Tests.cs`

- [ ] **Step 1: Write failing bounds tests**

Create `tests/VtkSharp.Tests/VtkSharpBounds3Tests.cs`:

```csharp
using System;
using Xunit;

namespace VtkSharp.Tests;

public sealed class VtkSharpBounds3Tests
{
    [Fact]
    public void FromArrayCopiesSixValues()
    {
        var values = new[] { 0.0, 1.0, -2.0, 3.0, 4.0, 5.0 };

        var bounds = VtkSharpBounds3.FromArray(values);

        Assert.Equal(0.0, bounds.XMin);
        Assert.Equal(1.0, bounds.XMax);
        Assert.Equal(-2.0, bounds.YMin);
        Assert.Equal(3.0, bounds.YMax);
        Assert.Equal(4.0, bounds.ZMin);
        Assert.Equal(5.0, bounds.ZMax);
    }

    [Fact]
    public void FromArrayRejectsInvalidLength()
    {
        Assert.Throws<ArgumentException>(() => VtkSharpBounds3.FromArray(new[] { 1.0, 2.0, 3.0 }));
    }

    [Fact]
    public void FromArrayRejectsNull()
    {
        Assert.Throws<ArgumentNullException>(() => VtkSharpBounds3.FromArray(null!));
    }
}
```

- [ ] **Step 2: Run failing tests**

Run:

```powershell
dotnet test tests/VtkSharp.Tests/VtkSharp.Tests.csproj --filter VtkSharpBounds3Tests
```

Expected: fails because `VtkSharpBounds3` is not defined.

- [ ] **Step 3: Implement `VtkSharpBounds3`**

Create `src/VtkSharp/VtkSharpBounds3.cs`:

```csharp
using System;

namespace VtkSharp;

public readonly struct VtkSharpBounds3
{
    public VtkSharpBounds3(double xMin, double xMax, double yMin, double yMax, double zMin, double zMax)
    {
        XMin = xMin;
        XMax = xMax;
        YMin = yMin;
        YMax = yMax;
        ZMin = zMin;
        ZMax = zMax;
    }

    public double XMin { get; }
    public double XMax { get; }
    public double YMin { get; }
    public double YMax { get; }
    public double ZMin { get; }
    public double ZMax { get; }

    public static VtkSharpBounds3 FromArray(double[] values)
    {
        if (values == null)
            throw new ArgumentNullException(nameof(values));
        if (values.Length != 6)
            throw new ArgumentException("Bounds array length must be 6.", nameof(values));

        return new VtkSharpBounds3(values[0], values[1], values[2], values[3], values[4], values[5]);
    }
}
```

- [ ] **Step 4: Add native library constant**

Create `src/VtkSharp/Native/VtkSharpNativeLibrary.cs`:

```csharp
namespace VtkSharp.Native;

internal static class VtkSharpNativeLibrary
{
    internal const string Name = "vtksharp_native";
}
```

- [ ] **Step 5: Add base wrapper skeleton**

Create `src/VtkSharp/Native/NativeMethods.cs`:

```csharp
using System;
using System.Runtime.InteropServices;

namespace VtkSharp.Native;

internal static partial class NativeMethods
{
    [DllImport(VtkSharpNativeLibrary.Name)]
    internal static extern IntPtr vtkObjectBase_Register(IntPtr self, IntPtr owner);

    [DllImport(VtkSharpNativeLibrary.Name)]
    internal static extern void vtkObjectBase_UnRegister(IntPtr self, IntPtr owner);

    [DllImport(VtkSharpNativeLibrary.Name)]
    internal static extern int vtkObjectBase_GetReferenceCount(IntPtr self);
}
```

Create `src/VtkSharp/Generated/vtkObjectBase.cs`:

```csharp
using System;

namespace VtkSharp;

public partial class vtkObjectBase : IDisposable
{
    private bool _disposed;

    protected vtkObjectBase(IntPtr nativePointer, bool ownsReference)
    {
        if (nativePointer == IntPtr.Zero)
            throw new InvalidOperationException("Native VTK pointer cannot be zero.");

        NativePointer = nativePointer;
        OwnsReference = ownsReference;
    }

    internal IntPtr NativePointer { get; private set; }

    protected bool OwnsReference { get; private set; }

    protected void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(GetType().Name);
    }

    public vtkObjectBase Register()
    {
        ThrowIfDisposed();
        var nativePointer = Native.NativeMethods.vtkObjectBase_Register(NativePointer, IntPtr.Zero);
        if (nativePointer == IntPtr.Zero)
            throw new InvalidOperationException("vtkObjectBase.Register returned a null native pointer.");
        return new vtkObjectBase(nativePointer, ownsReference: true);
    }

    public int GetReferenceCount()
    {
        ThrowIfDisposed();
        return Native.NativeMethods.vtkObjectBase_GetReferenceCount(NativePointer);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        if (OwnsReference)
            Native.NativeMethods.vtkObjectBase_UnRegister(NativePointer, IntPtr.Zero);

        NativePointer = IntPtr.Zero;
        OwnsReference = false;
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
```

- [ ] **Step 6: Run managed tests**

Run:

```powershell
dotnet test tests/VtkSharp.Tests/VtkSharp.Tests.csproj --filter VtkSharpBounds3Tests
```

Expected: bounds tests pass.

- [ ] **Step 7: Commit runtime primitives**

```powershell
git add src/VtkSharp tests/VtkSharp.Tests
git commit -m "feat: 添加托管绑定基础类型"
```

## Task 4: Implement Generator MVP

**Files:**
- Create: `generator/VtkSharp.Generator.Core/BindingManifest.cs`
- Create: `generator/VtkSharp.Generator.Core/ManifestLoader.cs`
- Create: `generator/VtkSharp.Generator.Core/CSharpEmitter.cs`
- Create: `generator/VtkSharp.Generator.Cli/Program.cs`

- [ ] **Step 1: Add manifest model**

Create `generator/VtkSharp.Generator.Core/BindingManifest.cs`:

```csharp
namespace VtkSharp.Generator.Core;

public sealed class BindingManifest
{
    public string VtkVersion { get; set; } = "";
    public string Profile { get; set; } = "";
    public List<string> Modules { get; set; } = new();
    public Dictionary<string, VtkClassManifest> Classes { get; set; } = new();
}

public sealed class VtkClassManifest
{
    public string Module { get; set; } = "";
    public string? BaseType { get; set; }
    public List<VtkMethodManifest> Methods { get; set; } = new();
}

public sealed class VtkMethodManifest
{
    public string Name { get; set; } = "";
    public string? NativeName { get; set; }
    public string Visibility { get; set; } = "public";
    public string? OverloadId { get; set; }
    public bool CustomShim { get; set; }
    public List<VtkParameterManifest> Parameters { get; set; } = new();
    public VtkReturnManifest? Returns { get; set; }
}

public sealed class VtkParameterManifest
{
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public string? Marshal { get; set; }
    public string? Direction { get; set; }
    public int? MultipleOf { get; set; }
    public bool NotNull { get; set; }
    public bool Nullable { get; set; }
    public string? Ownership { get; set; }
}

public sealed class VtkReturnManifest
{
    public string? Type { get; set; }
    public string? Ownership { get; set; }
    public bool Nullable { get; set; }
    public string? ManagedType { get; set; }
    public string? Marshal { get; set; }
    public string? ElementType { get; set; }
    public int? FixedLength { get; set; }
}
```

- [ ] **Step 2: Add manifest loader**

Create `generator/VtkSharp.Generator.Core/ManifestLoader.cs`:

```csharp
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace VtkSharp.Generator.Core;

public sealed class ManifestLoader
{
    private readonly IDeserializer _deserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    public BindingManifest Load(string path)
    {
        var text = File.ReadAllText(path);
        var manifest = _deserializer.Deserialize<BindingManifest>(text);
        if (manifest.Classes.Count == 0)
            throw new InvalidOperationException("Resolved manifest must contain at least one class.");
        return manifest;
    }
}
```

- [ ] **Step 3: Add C# emitter**

Create `generator/VtkSharp.Generator.Core/CSharpEmitter.cs`:

```csharp
using System.Text;

namespace VtkSharp.Generator.Core;

public sealed class CSharpEmitter
{
    public IReadOnlyDictionary<string, string> Emit(BindingManifest manifest)
    {
        var files = new Dictionary<string, string>
        {
            ["Native/NativeMethods.cs"] = EmitNativeMethods(manifest)
        };

        foreach (var pair in manifest.Classes)
        {
            if (pair.Key == "vtkObjectBase")
                continue;

            files[$"Generated/{pair.Key}.cs"] = EmitClass(pair.Key, pair.Value);
        }

        return files;
    }

    private static string EmitNativeMethods(BindingManifest manifest)
    {
        var sb = new StringBuilder();
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Runtime.InteropServices;");
        sb.AppendLine();
        sb.AppendLine("namespace VtkSharp.Native;");
        sb.AppendLine();
        sb.AppendLine("internal static partial class NativeMethods");
        sb.AppendLine("{");
        sb.AppendLine("    [DllImport(VtkSharpNativeLibrary.Name)]");
        sb.AppendLine("    internal static extern IntPtr vtkObjectBase_Register(IntPtr self, IntPtr owner);");
        sb.AppendLine();
        sb.AppendLine("    [DllImport(VtkSharpNativeLibrary.Name)]");
        sb.AppendLine("    internal static extern void vtkObjectBase_UnRegister(IntPtr self, IntPtr owner);");
        sb.AppendLine();
        sb.AppendLine("    [DllImport(VtkSharpNativeLibrary.Name)]");
        sb.AppendLine("    internal static extern int vtkObjectBase_GetReferenceCount(IntPtr self);");
        sb.AppendLine();

        foreach (var (className, cls) in manifest.Classes)
        {
            if (className == "vtkObjectBase")
                continue;

            foreach (var method in cls.Methods)
            {
                foreach (var declaration in NativeDeclarationEmitter.Emit(className, method))
                {
                    sb.AppendLine("    [DllImport(VtkSharpNativeLibrary.Name)]");
                    sb.AppendLine($"    internal static extern {declaration};");
                    sb.AppendLine();
                }
            }
        }

        sb.AppendLine("}");
        return sb.ToString();
    }

    private static string EmitClass(string className, VtkClassManifest cls)
    {
        var baseType = string.IsNullOrWhiteSpace(cls.BaseType) ? "vtkObjectBase" : cls.BaseType;
        if (className == "vtkObjectBase")
            baseType = "";

        var sb = new StringBuilder();
        sb.AppendLine("using System;");
        sb.AppendLine();
        sb.AppendLine("namespace VtkSharp;");
        sb.AppendLine();
        sb.Append($"public partial class {className}");
        if (!string.IsNullOrWhiteSpace(baseType))
            sb.Append($" : {baseType}");
        sb.AppendLine();
        sb.AppendLine("{");

        if (className != "vtkObjectBase")
        {
            sb.AppendLine($"    internal {className}(IntPtr nativePointer, bool ownsReference)");
            sb.AppendLine("        : base(nativePointer, ownsReference)");
            sb.AppendLine("    {");
            sb.AppendLine("    }");
            sb.AppendLine();
        }

        foreach (var method in cls.Methods)
        {
            foreach (var member in ManagedMethodEmitter.Emit(className, method))
            {
                sb.AppendLine(member);
            }
        }

        sb.AppendLine("}");
        return sb.ToString();
    }
}
```

Also create `generator/VtkSharp.Generator.Core/NativeDeclarationEmitter.cs` and `ManagedMethodEmitter.cs` with narrowly supported MVP mappings:

```csharp
namespace VtkSharp.Generator.Core;

internal static class NativeDeclarationEmitter
{
    public static IEnumerable<string> Emit(string className, VtkMethodManifest method)
    {
        var nativeName = GetNativeFunctionName(className, method);
        if (method.Name == "New" || method.Name == "FromArray" || method.Name == "FromTriangles")
            yield return $"IntPtr {nativeName}({EmitParameters(method)})";
        else if (method.Name.StartsWith("GetNumberOf", StringComparison.Ordinal))
            yield return $"long {nativeName}(IntPtr self)";
        else if (method.Name == "GetBounds")
            yield return $"void {nativeName}(IntPtr self, [Out] double[] values)";
        else if (method.Name.StartsWith("Set", StringComparison.Ordinal))
            yield return $"void {nativeName}(IntPtr self, IntPtr value)";
        else if (method.Name == "Modified")
            yield return $"void {nativeName}(IntPtr self)";
        else if (method.Name == "GetMTime")
            yield return $"ulong {nativeName}(IntPtr self)";
        else if (method.Name == "GetReferenceCount")
            yield return $"int {nativeName}(IntPtr self)";
    }

    internal static string GetNativeFunctionName(string className, VtkMethodManifest method)
    {
        return method.OverloadId == null
            ? $"{className}_{method.Name}"
            : $"{className}_{method.Name}_{method.OverloadId.Replace("-", "_")}";
    }

    private static string EmitParameters(VtkMethodManifest method)
    {
        if (method.Parameters.Count == 0)
            return "";

        return string.Join(", ", method.Parameters.Select(p =>
            p.Type switch
            {
                "double[]" => "double[] " + p.Name,
                "long[]" => "long[] " + p.Name,
                "int[]" => "int[] " + p.Name,
                _ when p.Type.StartsWith("vtk", StringComparison.Ordinal) => "IntPtr " + p.Name,
                _ => throw new NotSupportedException("Unsupported parameter type: " + p.Type)
            }));
    }
}
```

```csharp
using System.Text;

namespace VtkSharp.Generator.Core;

internal static class ManagedMethodEmitter
{
    public static IEnumerable<string> Emit(string className, VtkMethodManifest method)
    {
        if (method.Name == "New")
        {
            yield return $$"""
    public static {{className}} New()
    {
        var nativePointer = Native.NativeMethods.{{className}}_New();
        if (nativePointer == IntPtr.Zero)
            throw new InvalidOperationException("{{className}}.New returned a null native pointer.");
        return new {{className}}(nativePointer, ownsReference: true);
    }

""";
            yield break;
        }

        if (method.Name == "FromArray")
        {
            yield return $$"""
    public static {{className}} FromArray(double[] xyz)
    {
        if (xyz == null)
            throw new ArgumentNullException(nameof(xyz));
        if (xyz.Length % 3 != 0)
            throw new ArgumentException("Point coordinate array length must be a multiple of 3.", nameof(xyz));
        var nativePointer = Native.NativeMethods.{{className}}_FromArray(xyz);
        if (nativePointer == IntPtr.Zero)
            throw new InvalidOperationException("{{className}}.FromArray returned a null native pointer.");
        return new {{className}}(nativePointer, ownsReference: true);
    }

""";
            yield break;
        }

        if (method.Name == "FromTriangles")
        {
            var parameter = method.Parameters[0];
            var parameterType = parameter.Type == "int[]" ? "int[]" : "long[]";
            var nativeName = NativeDeclarationEmitter.GetNativeFunctionName(className, method);
            yield return $$"""
    public static {{className}} FromTriangles({{parameterType}} ids)
    {
        if (ids == null)
            throw new ArgumentNullException(nameof(ids));
        if (ids.Length % 3 != 0)
            throw new ArgumentException("Triangle index array length must be a multiple of 3.", nameof(ids));
        var nativePointer = Native.NativeMethods.{{nativeName}}(ids);
        if (nativePointer == IntPtr.Zero)
            throw new InvalidOperationException("{{className}}.FromTriangles returned a null native pointer.");
        return new {{className}}(nativePointer, ownsReference: true);
    }

""";
            yield break;
        }

        if (method.Name.StartsWith("GetNumberOf", StringComparison.Ordinal))
        {
            yield return $$"""
    public long {{method.Name}}()
    {
        ThrowIfDisposed();
        return Native.NativeMethods.{{className}}_{{method.Name}}(NativePointer);
    }

""";
            yield break;
        }

        if (method.Name == "GetBounds")
        {
            yield return $$"""
    public VtkSharpBounds3 GetBounds()
    {
        ThrowIfDisposed();
        var values = new double[6];
        Native.NativeMethods.vtkDataSet_GetBounds(NativePointer, values);
        return VtkSharpBounds3.FromArray(values);
    }

""";
            yield break;
        }

        if (method.Name is "SetPoints" or "SetPolys")
        {
            var parameterName = method.Parameters[0].Name;
            var parameterType = method.Parameters[0].Type;
            yield return $$"""
    public void {{method.Name}}({{parameterType}} {{parameterName}})
    {
        ThrowIfDisposed();
        if ({{parameterName}} == null)
            throw new ArgumentNullException(nameof({{parameterName}}));
        Native.NativeMethods.{{className}}_{{method.Name}}(NativePointer, {{parameterName}}.NativePointer);
    }

""";
        }
    }
}
```

- [ ] **Step 4: Add CLI**

Create `generator/VtkSharp.Generator.Cli/Program.cs`:

```csharp
using VtkSharp.Generator.Core;

if (args.Length != 4 || args[0] != "generate" || args[2] != "--out")
{
    Console.Error.WriteLine("Usage: VtkSharp.Generator.Cli generate <manifest> --out <directory>");
    return 2;
}

var manifestPath = args[1];
var outputDirectory = args[3];
var manifest = new ManifestLoader().Load(manifestPath);
var files = new CSharpEmitter().Emit(manifest);

foreach (var file in files)
{
    var path = Path.Combine(outputDirectory, file.Key.Replace('/', Path.DirectorySeparatorChar));
    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
    File.WriteAllText(path, file.Value);
}

return 0;
```

- [ ] **Step 5: Build generator**

Run:

```powershell
dotnet build generator/VtkSharp.Generator.Cli/VtkSharp.Generator.Cli.csproj
```

Expected: generator builds successfully.

- [ ] **Step 6: Commit generator**

```powershell
git add generator
git commit -m "feat: 添加 MVP C# 绑定生成器"
```

## Task 5: Generate First Managed Wrappers

**Files:**
- Modify: `src/VtkSharp/Native/NativeMethods.cs`
- Create/Modify: `src/VtkSharp/Generated/*.cs`

- [ ] **Step 1: Run generator**

Run:

```powershell
dotnet run --project generator/VtkSharp.Generator.Cli/VtkSharp.Generator.Cli.csproj -- generate bindings/vtk-9.6/resolved/mvp-data.resolved.yaml --out src/VtkSharp
```

Expected: generated files appear under `src/VtkSharp/Generated` and `src/VtkSharp/Native/NativeMethods.cs`.

- [ ] **Step 2: Inspect generated wrappers**

Run:

```powershell
Get-ChildItem src/VtkSharp/Generated
Get-Content src/VtkSharp/Native/NativeMethods.cs
```

Expected: wrappers exist for `vtkObjectBase`, `vtkObject`, `vtkDataObject`, `vtkDataSet`, `vtkPointSet`, `vtkPolyData`, `vtkAbstractCellArray`, `vtkCellArray`, and `vtkPoints`.

- [ ] **Step 3: Build managed library**

Run:

```powershell
dotnet build src/VtkSharp/VtkSharp.csproj
```

Expected: build succeeds. Generated files do not overwrite `src/VtkSharp/Generated/vtkObjectBase.cs`.

- [ ] **Step 4: Commit generated wrappers**

```powershell
git add src/VtkSharp
git commit -m "feat: 生成首批 VTK 托管 wrapper"
```

## Task 6: Add Native C ABI Shim

**Files:**
- Create: `native/CMakeLists.txt`
- Create: `native/CMakePresets.json`
- Create: `native/include/vtksharp_api.h`
- Create: `native/src/manual/vtksharp_core.cpp`

- [ ] **Step 1: Create export header**

Create `native/include/vtksharp_api.h`:

```cpp
#pragma once

#if defined(_WIN32)
#define VTKSHARP_API extern "C" __declspec(dllexport)
#else
#define VTKSHARP_API extern "C" __attribute__((visibility("default")))
#endif
```

- [ ] **Step 2: Create native shim implementation**

Create `native/src/manual/vtksharp_core.cpp`:

```cpp
#include "vtksharp_api.h"

#include <cassert>
#include <cstdint>

#include <vtkCellArray.h>
#include <vtkDataSet.h>
#include <vtkObject.h>
#include <vtkObjectBase.h>
#include <vtkPoints.h>
#include <vtkPolyData.h>
#include <vtkSmartPointer.h>
#include <vtkType.h>

static_assert(sizeof(vtkIdType) == sizeof(std::int64_t));

VTKSHARP_API void vtkObjectBase_UnRegister(vtkObjectBase* self, vtkObjectBase* owner)
{
    assert(self != nullptr);
    self->UnRegister(owner);
}

VTKSHARP_API vtkObjectBase* vtkObjectBase_Register(vtkObjectBase* self, vtkObjectBase* owner)
{
    assert(self != nullptr);
    self->Register(owner);
    return self;
}

VTKSHARP_API int vtkObjectBase_GetReferenceCount(vtkObjectBase* self)
{
    assert(self != nullptr);
    return self->GetReferenceCount();
}

VTKSHARP_API std::uint64_t vtkObject_GetMTime(vtkObject* self)
{
    assert(self != nullptr);
    return static_cast<std::uint64_t>(self->GetMTime());
}

VTKSHARP_API void vtkObject_Modified(vtkObject* self)
{
    assert(self != nullptr);
    self->Modified();
}

VTKSHARP_API vtkPoints* vtkPoints_New()
{
    return vtkPoints::New();
}

VTKSHARP_API vtkIdType vtkPoints_GetNumberOfPoints(vtkPoints* self)
{
    assert(self != nullptr);
    return self->GetNumberOfPoints();
}

VTKSHARP_API vtkPoints* vtkPoints_FromArray(const double* xyz, vtkIdType valueCount)
{
    assert(xyz != nullptr);
    assert(valueCount % 3 == 0);

    vtkPoints* points = vtkPoints::New();
    const vtkIdType pointCount = valueCount / 3;
    points->SetNumberOfPoints(pointCount);

    for (vtkIdType i = 0; i < pointCount; ++i)
    {
        points->SetPoint(i, xyz[i * 3], xyz[i * 3 + 1], xyz[i * 3 + 2]);
    }

    return points;
}

VTKSHARP_API vtkCellArray* vtkCellArray_New()
{
    return vtkCellArray::New();
}

VTKSHARP_API vtkIdType vtkCellArray_GetNumberOfCells(vtkCellArray* self)
{
    assert(self != nullptr);
    return self->GetNumberOfCells();
}

VTKSHARP_API vtkCellArray* vtkCellArray_FromTriangles_long_array(const vtkIdType* ids, vtkIdType valueCount)
{
    assert(ids != nullptr);
    assert(valueCount % 3 == 0);

    vtkCellArray* cells = vtkCellArray::New();
    const vtkIdType triangleCount = valueCount / 3;
    for (vtkIdType i = 0; i < triangleCount; ++i)
    {
        vtkIdType tri[3] = { ids[i * 3], ids[i * 3 + 1], ids[i * 3 + 2] };
        cells->InsertNextCell(3, tri);
    }

    return cells;
}

VTKSHARP_API vtkCellArray* vtkCellArray_FromTriangles_int_array(const int* ids, vtkIdType valueCount)
{
    assert(ids != nullptr);
    assert(valueCount % 3 == 0);

    vtkCellArray* cells = vtkCellArray::New();
    const vtkIdType triangleCount = valueCount / 3;
    for (vtkIdType i = 0; i < triangleCount; ++i)
    {
        vtkIdType tri[3] = {
            static_cast<vtkIdType>(ids[i * 3]),
            static_cast<vtkIdType>(ids[i * 3 + 1]),
            static_cast<vtkIdType>(ids[i * 3 + 2])
        };
        cells->InsertNextCell(3, tri);
    }

    return cells;
}

VTKSHARP_API vtkPolyData* vtkPolyData_New()
{
    return vtkPolyData::New();
}

VTKSHARP_API void vtkPolyData_SetPoints(vtkPolyData* self, vtkPoints* points)
{
    assert(self != nullptr);
    assert(points != nullptr);
    self->SetPoints(points);
}

VTKSHARP_API void vtkPolyData_SetPolys(vtkPolyData* self, vtkCellArray* polys)
{
    assert(self != nullptr);
    assert(polys != nullptr);
    self->SetPolys(polys);
}

VTKSHARP_API vtkIdType vtkPolyData_GetNumberOfPoints(vtkPolyData* self)
{
    assert(self != nullptr);
    return self->GetNumberOfPoints();
}

VTKSHARP_API vtkIdType vtkPolyData_GetNumberOfCells(vtkPolyData* self)
{
    assert(self != nullptr);
    return self->GetNumberOfCells();
}

VTKSHARP_API void vtkDataSet_GetBounds(vtkDataSet* self, double* bounds)
{
    assert(self != nullptr);
    assert(bounds != nullptr);
    self->GetBounds(bounds);
}
```

- [ ] **Step 3: Create CMake build**

Create `native/CMakeLists.txt`:

```cmake
cmake_minimum_required(VERSION 3.25)

project(vtksharp_native LANGUAGES CXX)

find_package(VTK CONFIG REQUIRED COMPONENTS
  CommonCore
  CommonDataModel
)

add_library(vtksharp_native SHARED
  src/manual/vtksharp_core.cpp
)

target_compile_features(vtksharp_native PRIVATE cxx_std_17)

target_include_directories(vtksharp_native
  PRIVATE
    ${CMAKE_CURRENT_SOURCE_DIR}/include
)

target_link_libraries(vtksharp_native
  PRIVATE
    VTK::CommonCore
    VTK::CommonDataModel
)
```

- [ ] **Step 4: Create CMake preset**

Create `native/CMakePresets.json`:

```json
{
  "version": 6,
  "configurePresets": [
    {
      "name": "windows-x64",
      "displayName": "Windows x64",
      "generator": "Ninja",
      "binaryDir": "${sourceDir}/out/build/windows-x64",
      "cacheVariables": {
        "CMAKE_BUILD_TYPE": "Release"
      }
    }
  ],
  "buildPresets": [
    {
      "name": "windows-x64",
      "configurePreset": "windows-x64"
    }
  ]
}
```

- [ ] **Step 5: Configure and build native shim**

Run with a local VTK install:

```powershell
cmake --preset windows-x64 -S native -DVTK_DIR="C:\Program Files\VTK\lib\cmake\vtk-9.5"
cmake --build native/out/build/windows-x64 --config Release
```

Expected: `vtksharp_native.dll` is produced under `native/out/build/windows-x64`.

- [ ] **Step 6: Commit native shim**

```powershell
git add native
git commit -m "feat: 添加首批 VTK native shim"
```

## Task 7: Fix P/Invoke Array Signatures

**Files:**
- Modify: `generator/VtkSharp.Generator.Core/NativeDeclarationEmitter.cs`
- Modify: generated `src/VtkSharp/Native/NativeMethods.cs`

- [ ] **Step 1: Adjust generated P/Invoke array signatures**

Update `NativeDeclarationEmitter.EmitParameters` so array custom shims include array length:

```csharp
private static string EmitParameters(VtkMethodManifest method)
{
    if (method.Parameters.Count == 0)
        return "";

    var parts = new List<string>();
    foreach (var parameter in method.Parameters)
    {
        if (parameter.Type == "double[]")
        {
            parts.Add("double[] " + parameter.Name);
            parts.Add("long valueCount");
        }
        else if (parameter.Type == "long[]")
        {
            parts.Add("long[] " + parameter.Name);
            parts.Add("long valueCount");
        }
        else if (parameter.Type == "int[]")
        {
            parts.Add("int[] " + parameter.Name);
            parts.Add("long valueCount");
        }
        else if (parameter.Type.StartsWith("vtk", StringComparison.Ordinal))
        {
            parts.Add("IntPtr " + parameter.Name);
        }
        else
        {
            throw new NotSupportedException("Unsupported parameter type: " + parameter.Type);
        }
    }

    return string.Join(", ", parts);
}
```

- [ ] **Step 2: Adjust managed emitter calls**

Update `ManagedMethodEmitter` so custom array calls pass `LongLength`:

```csharp
Native.NativeMethods.vtkPoints_FromArray(xyz, xyz.LongLength);
Native.NativeMethods.vtkCellArray_FromTriangles_long_array(ids, ids.LongLength);
Native.NativeMethods.vtkCellArray_FromTriangles_int_array(ids, ids.LongLength);
```

- [ ] **Step 3: Regenerate wrappers**

Run:

```powershell
dotnet run --project generator/VtkSharp.Generator.Cli/VtkSharp.Generator.Cli.csproj -- generate bindings/vtk-9.6/resolved/mvp-data.resolved.yaml --out src/VtkSharp
dotnet build src/VtkSharp/VtkSharp.csproj
```

Expected: managed library builds and P/Invoke signatures match the native shim.

- [ ] **Step 4: Commit signature fix**

```powershell
git add generator src/VtkSharp
git commit -m "fix: 对齐数组 shim 的 PInvoke 签名"
```

## Task 8: Add Native Smoke Test

**Files:**
- Create: `tests/VtkSharp.NativeSmokeTests/TrianglePolyDataSmokeTests.cs`
- Create: `scripts/test.ps1`

- [ ] **Step 1: Write failing native smoke test**

Create `tests/VtkSharp.NativeSmokeTests/TrianglePolyDataSmokeTests.cs`:

```csharp
using Xunit;

namespace VtkSharp.NativeSmokeTests;

public sealed class TrianglePolyDataSmokeTests
{
    [Fact]
    public void CreatesTrianglePolyDataAndComputesBounds()
    {
        using var points = vtkPoints.FromArray(new[]
        {
            0.0, 0.0, 0.0,
            1.0, 0.0, 0.0,
            0.0, 1.0, 0.0,
        });

        using var polys = vtkCellArray.FromTriangles(new long[] { 0, 1, 2 });

        using var polyData = vtkPolyData.New();
        polyData.SetPoints(points);
        polyData.SetPolys(polys);

        Assert.Equal(3, polyData.GetNumberOfPoints());
        Assert.Equal(1, polyData.GetNumberOfCells());

        var bounds = polyData.GetBounds();
        Assert.Equal(0.0, bounds.XMin);
        Assert.Equal(1.0, bounds.XMax);
        Assert.Equal(0.0, bounds.YMin);
        Assert.Equal(1.0, bounds.YMax);
        Assert.Equal(0.0, bounds.ZMin);
        Assert.Equal(0.0, bounds.ZMax);
    }
}
```

- [ ] **Step 2: Add local test script**

Create `scripts/test.ps1`:

```powershell
param(
    [Parameter(Mandatory = $true)]
    [string] $VtkBin,

    [Parameter(Mandatory = $true)]
    [string] $NativeBuildDir
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$testOutput = Join-Path $repoRoot "tests/VtkSharp.NativeSmokeTests/bin/Debug/net8.0"
$nativeDll = Join-Path $NativeBuildDir "vtksharp_native.dll"

dotnet build "$repoRoot/VtkSharp.sln"

if (-not (Test-Path $nativeDll)) {
    throw "Native DLL not found: $nativeDll"
}

New-Item -ItemType Directory -Force $testOutput | Out-Null
Copy-Item $nativeDll $testOutput -Force

$env:PATH = "$VtkBin;$NativeBuildDir;$env:PATH"

dotnet test "$repoRoot/tests/VtkSharp.Tests/VtkSharp.Tests.csproj"
dotnet test "$repoRoot/tests/VtkSharp.NativeSmokeTests/VtkSharp.NativeSmokeTests.csproj"
```

- [ ] **Step 3: Run smoke test before copying native DLL**

Run:

```powershell
dotnet test tests/VtkSharp.NativeSmokeTests/VtkSharp.NativeSmokeTests.csproj --filter TrianglePolyDataSmokeTests
```

Expected: fails with native DLL load error if `vtksharp_native.dll` has not been copied to test output.

- [ ] **Step 4: Run full local test script**

Run:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/test.ps1 `
  -VtkBin "C:\Program Files\VTK\bin" `
  -NativeBuildDir "D:\Code\wudong-lab\VtkSharp\native\out\build\windows-x64"
```

Expected: managed tests and native smoke tests pass.

- [ ] **Step 5: Commit smoke test**

```powershell
git add tests scripts
git commit -m "test: 添加 triangle polydata native smoke test"
```

## Task 9: Final Verification

**Files:**
- Verify all files touched in previous tasks.

- [ ] **Step 1: Confirm generator output is stable**

Run:

```powershell
dotnet run --project generator/VtkSharp.Generator.Cli/VtkSharp.Generator.Cli.csproj -- generate bindings/vtk-9.6/resolved/mvp-data.resolved.yaml --out src/VtkSharp
git diff --exit-code
```

Expected: no diff. A non-empty diff means the task is not complete; commit the generated files in the task that changed the generator.

- [ ] **Step 2: Run managed build**

Run:

```powershell
dotnet build VtkSharp.sln
```

Expected: build succeeds.

- [ ] **Step 3: Run native build**

Run:

```powershell
cmake --preset windows-x64 -S native -DVTK_DIR="C:\Program Files\VTK\lib\cmake\vtk-9.5"
cmake --build native/out/build/windows-x64 --config Release
```

Expected: native build succeeds.

- [ ] **Step 4: Run test script**

Run:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/test.ps1 `
  -VtkBin "C:\Program Files\VTK\bin" `
  -NativeBuildDir "D:\Code\wudong-lab\VtkSharp\native\out\build\windows-x64"
```

Expected: all tests pass.

- [ ] **Step 5: Check git status**

Run:

```powershell
git status --short
```

Expected: clean worktree.

- [ ] **Step 6: Finish with a clean worktree**

Run:

```powershell
git status --short
```

Expected: no output. This confirms every intentional change was committed in the task that introduced it.
