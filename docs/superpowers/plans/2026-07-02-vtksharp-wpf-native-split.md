# VtkSharp WPF Native Split Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Split WPF OpenGL/D3D interop exports into `VtkSharp.Wpf.Native.dll` while keeping core VTK bindings in `VtkSharp.Native.dll`.

**Architecture:** Keep `src/bindings/VtkSharp.Native` as the single CMake configure entry so existing scripts and presets remain stable. Move WPF native source to `src/bindings/VtkSharp.Wpf.Native`, add a second CMake target, point WPF P/Invoke to `VtkSharp.Wpf.Native`, and package the WPF native DLL from `VtkSharp.Wpf`.

**Tech Stack:** CMake, MSVC, VTK, WPF, .NET SDK-style projects, P/Invoke, NuGet runtime assets, xUnit.

---

### Task 1: Move WPF Native Sources

**Files:**
- Move: `src/bindings/VtkSharp.Native/src/wpf/*` to `src/bindings/VtkSharp.Wpf.Native/src/*`
- Create: `src/bindings/VtkSharp.Wpf.Native/CMakeLists.txt`
- Create: `src/bindings/VtkSharp.Wpf.Native/.gitignore`
- Modify: `src/bindings/VtkSharp.Native/CMakeLists.txt`

- [ ] Move the WPF native source directory to a sibling native project directory.
- [ ] Keep `src/bindings/VtkSharp.Native/include/vtksharp_api.h` in the core native project and include it from the WPF native target.
- [ ] Update core CMake so it builds `VtkSharp.Native` from core sources only and adds `VtkSharp.Wpf.Native` as a subdirectory target.
- [ ] Add WPF native CMake target that links VTK plus `d3d9`, `gdi32`, `opengl32`, and `user32`.

### Task 2: Update Managed WPF Native Binding

**Files:**
- Modify: `src/bindings/VtkSharp.Wpf/InteropInfo.cs`
- Modify: `src/bindings/VtkSharp.Wpf/VtkSharp.Wpf.csproj`

- [ ] Change WPF P/Invoke library name to `VtkSharp.Wpf.Native`.
- [ ] Add local build copy item for `VtkSharp.Wpf.Native.dll`.
- [ ] Add NuGet runtime asset entry under `runtimes/win-x64/native/VtkSharp.Wpf.Native.dll`.

### Task 3: Update Tests

**Files:**
- Modify: `src/bindings/VtkSharp.Tests/*Wpf*.cs`

- [ ] Update native WPF source path lookups to `src/bindings/VtkSharp.Wpf.Native/src`.
- [ ] Add assertions that core interop still uses `VtkSharp.Native` while WPF interop uses `VtkSharp.Wpf.Native`.
- [ ] Add assertions that CMake defines a separate `VtkSharp.Wpf.Native` target.

### Task 4: Verify Build and Runtime Asset Flow

**Commands:**

```powershell
dotnet test src/bindings/VtkSharp.slnx
powershell -NoProfile -ExecutionPolicy Bypass -File tools/build-native.ps1 -Configuration Debug
dotnet build src/bindings/VtkSharp.slnx
dotnet build src/examples/VtkSharp.Examples.slnx
```

Expected:

- xUnit tests pass.
- CMake produces both `VtkSharp.Native.dll` and `VtkSharp.Wpf.Native.dll`.
- `VtkSharp.Wpf` and `ExampleBrowser` build after the native split.

### Task 5: Commit

**Files:**
- Stage all moved sources, CMake edits, project edits, tests, and this plan.

Commit message:

```text
拆分 WPF native 模块
```
