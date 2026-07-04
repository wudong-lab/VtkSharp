# VtkSharp Directory Restructure Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Move VtkSharp to the approved `src/bindings`, `src/generator`, and `src/examples` layout without changing public API, package identity, namespace, or native DLL name.

**Architecture:** This migration is a path and solution-boundary change. Stage one keeps `VtkSharp.Native.dll` as the single native DLL while moving the native project to `src/bindings/VtkSharp.Native`; WPF native splitting remains a later task.

**Tech Stack:** .NET SDK projects, `.slnx`, CMake, PowerShell build scripts, xUnit tests, WPF example project.

---

### Task 1: Move Directories

**Files:**
- Move: `generator/` to `src/generator/`
- Move: `examples/` to `src/examples/`
- Move: `src/native/` to `src/bindings/VtkSharp.Native/`

- [ ] **Step 1: Verify current directories exist**

Run:

```powershell
Test-Path generator
Test-Path examples
Test-Path src/native
Test-Path src/bindings
```

Expected: all commands print `True`.

- [ ] **Step 2: Move directories**

Run:

```powershell
Move-Item -LiteralPath generator -Destination src/generator
Move-Item -LiteralPath examples -Destination src/examples
Move-Item -LiteralPath src/native -Destination src/bindings/VtkSharp.Native
```

Expected: directories are moved and no error is printed.

- [ ] **Step 3: Verify target directories exist**

Run:

```powershell
Test-Path src/generator/VtkSharp.Generator.slnx
Test-Path src/examples/ExampleBrowser/ExampleBrowser.csproj
Test-Path src/bindings/VtkSharp.Native/CMakeLists.txt
```

Expected: all commands print `True`.

### Task 2: Update Project and Solution Paths

**Files:**
- Modify: `src/bindings/VtkSharp/VtkSharp.csproj`
- Modify: `src/bindings/VtkSharp.Tests/WpfExportNameTests.cs`
- Modify: `src/bindings/VtkSharp.slnx`
- Modify: `src/examples/ExampleBrowser/ExampleBrowser.csproj`
- Create: `src/examples/VtkSharp.Examples.slnx`

- [ ] **Step 1: Update native DLL paths in `VtkSharp.csproj`**

Replace local native paths so they point from `src/bindings/VtkSharp/` to `src/bindings/VtkSharp.Native/`:

```xml
<VtkSharpNativeDefaultDllPath>$(MSBuildThisFileDirectory)..\VtkSharp.Native\out\build\win-x64-vs2026\$(Configuration)\$(VtkSharpNativeDllName)</VtkSharpNativeDefaultDllPath>
<VtkSharpNativeVs2022DllPath>$(MSBuildThisFileDirectory)..\VtkSharp.Native\out\build\win-x64-vs2022\$(Configuration)\$(VtkSharpNativeDllName)</VtkSharpNativeVs2022DllPath>
```

- [ ] **Step 2: Update bindings solution**

Set `src/bindings/VtkSharp.slnx` to include only binding subsystem projects:

```xml
<Solution>
  <Project Path="VtkSharp/VtkSharp.csproj" />
  <Project Path="VtkSharp.Wpf/VtkSharp.Wpf.csproj" />
  <Project Path="VtkSharp.Tests/VtkSharp.Tests.csproj" />
</Solution>
```

- [ ] **Step 3: Update ExampleBrowser project references**

Set `src/examples/ExampleBrowser/ExampleBrowser.csproj` references to:

```xml
<ProjectReference Include="..\..\bindings\VtkSharp\VtkSharp.csproj" />
<ProjectReference Include="..\..\bindings\VtkSharp.Wpf\VtkSharp.Wpf.csproj" />
```

- [ ] **Step 4: Create examples solution**

Create `src/examples/VtkSharp.Examples.slnx`:

```xml
<Solution>
  <Project Path="ExampleBrowser/ExampleBrowser.csproj" />
</Solution>
```

- [ ] **Step 5: Update test path constants**

In `src/bindings/VtkSharp.Tests/WpfExportNameTests.cs`, change repository-relative paths that locate WPF and native files from old locations to:

```csharp
Path.Combine(FindRepositoryRoot().FullName, "src", "bindings", "VtkSharp.Wpf")
Path.Combine(FindRepositoryRoot().FullName, "src", "bindings", "VtkSharp.Native", "src", "wpf", "D3DImageRenderTarget.cpp")
Path.Combine(FindRepositoryRoot().FullName, "src", "bindings", "VtkSharp.Native", "src", "wpf", "D3DImageRenderTarget.h")
```

### Task 3: Update Scripts and Documentation Paths

**Files:**
- Modify: `tools/build-native.ps1`
- Modify: `tools/package-nuget.ps1`
- Modify: `src/generator/config/vtksharp.generator.yml`
- Modify: `src/generator/config/vtksharp.generator.local.example.yml`
- Modify: `src/generator/README` references if present
- Modify: repo docs that mention old `generator`, `examples`, or `src/native` paths where needed

- [ ] **Step 1: Update native script path**

In `tools/build-native.ps1`, change:

```powershell
$nativeDir = Join-Path $repoRoot "src\native"
```

to:

```powershell
$nativeDir = Join-Path $repoRoot "src\bindings\VtkSharp.Native"
```

- [ ] **Step 2: Update package script path**

Keep `$bindingsDir = Join-Path $repoRoot "src" "bindings"` in `tools/package-nuget.ps1`; no change is required unless later checks reveal direct `src\native` references.

- [ ] **Step 3: Update generator config paths**

Search for old path fragments and update them to the new layout:

```powershell
rg -n "src/native|src\\native|generator/|generator\\|examples/|examples\\" src tools docs README.md AGENTS.md CLAUDE.md
```

Expected: old path references are either updated or intentionally left when they describe historical design.

### Task 4: Verify Migration

**Files:**
- Read: all changed files
- Run: build and test commands where local environment supports them

- [ ] **Step 1: Check git status**

Run:

```powershell
git status --short
```

Expected: moved files and path edits are visible; no unrelated files are changed.

- [ ] **Step 2: Build bindings solution**

Run:

```powershell
dotnet build src/bindings/VtkSharp.slnx
```

Expected: build exits with code `0`.

- [ ] **Step 3: Test bindings solution**

Run:

```powershell
dotnet test src/bindings/VtkSharp.slnx
```

Expected: tests exit with code `0`.

- [ ] **Step 4: Build generator solution**

Run:

```powershell
dotnet build src/generator/VtkSharp.Generator.slnx
```

Expected: build exits with code `0`.

- [ ] **Step 5: Test generator solution**

Run:

```powershell
dotnet test src/generator/VtkSharp.Generator.slnx
```

Expected: tests exit with code `0`.

- [ ] **Step 6: Build examples solution**

Run:

```powershell
dotnet build src/examples/VtkSharp.Examples.slnx
```

Expected: build exits with code `0`.

### Task 5: Commit Migration

**Files:**
- Stage: moved directories, edited project files, edited scripts, implementation plan

- [ ] **Step 1: Review final diff summary**

Run:

```powershell
git status --short
git diff --stat
```

Expected: changes match the directory migration scope.

- [ ] **Step 2: Commit**

Run:

```powershell
git add -A
git commit -m "调整项目目录结构"
```

Expected: commit succeeds.
