---
name: port-vtk-example
description: Translate a VTK C++ example to C# for VtkSharp and automatically discover/add any missing VTK APIs to the binding whitelist. Trigger when the user asks to port, translate, or add a VTK example from a URL, file path, or example name.
argument-hint: <example-url-or-path> <ExampleName>
tools: Bash, Read, Write, Edit, Grep, Glob
---

# Port VTK C++ Example to C#

Follow this workflow exactly to translate a VTK C++ example to C# and extend the VtkSharp bindings as needed.

All commands run from repository root: `D:\Code\wudong-lab\VtkSharp`. Use `--` after `--project` to pass arguments to the CLI.

CLI binary: `dotnet run --project generator/VtkSharp.Generator.Cli --`

## 0. Fetch the C++ source

First obtain the C++ source. Try in order:
1. If given a URL to `examples.vtk.org`, fetch it with `curl -sL <url>` and extract the code from `<pre>` tags.
2. If given a local file path, read it directly.
3. Do NOT rely on memory — always read the actual source. Missing calls like `RotateX`, `Zoom`, `SetWindowName`, `ResetCamera` leads to incorrect ports.

Identify all VTK classes and methods used. Note any types that are likely unsupported (pointer returns to non-vtkObject types, references, `std::array`, `vtkVariant`, etc.) — these will need inline value substitution.

## 1. Write the C# translation

Create `examples/<Name>/<Name>.cs`. Follow the pattern from existing examples (see `examples/Cone/Cone.cs`, `examples/Cylinder/Cylinder.cs`):

```csharp
using System;
using VtkSharp;

namespace VtkSharp.Examples;

// Port of <original-path>
// <one-line description of what the example does>
// <note any deviations here, e.g. vtkNamedColors values inlined>

internal class <Name>
{
    static void Main()
    {
        // C# translation here
    }
}
```

Key translation rules:
- `vtkNew<vtkXxx> xxx;` → `using var xxx = vtkXxx.New();`
- `->` method calls → `.` method calls
- `vtkSmartPointer` / raw pointers → no wrapping needed, `New()` returns a managed wrapper
- Wrap every VTK object in `using var` (implements `IDisposable`)
- vtkNamedColors is available: use `GetColorRGB(name, stackalloc double[3])` or `GetColor(name, stackalloc double[4])` with `Span<double>`. Example:
  ```csharp
  using var colors = vtkNamedColors.New();
  Span<double> rgb = stackalloc double[3];
  colors.GetColorRGB("Tomato", rgb);
  actor.GetProperty().SetColor(rgb[0], rgb[1], rgb[2]);
  ```
  The value-type overloads (`GetColor3d` returning `vtkColor3d`) are not bound; use the fixed-array overloads instead.
- For other unsupported types (returning non-vtkObject pointers, `int&` out params, `std::string&`, etc.), work around them and document the deviation in porting-notes.md.
- Do NOT add example code to TestConsole at this stage — the example file in `examples/<Name>/` is the canonical port.

## 2. Build and identify missing symbols

```bash
dotnet build src/bindings/VtkSharp.slnx
```

Parse compilation errors to identify exactly which classes and methods are missing. Collect a list of `<ClassName>::<MethodName>` pairs needed.

Note: methods defined on base classes (e.g. `SetBackground` is on `vtkViewport`, not `vtkRenderer`) will resolve via C# inheritance once the base class method is whitelisted. Use `inspect-function` to verify which class declares a method:

```bash
dotnet run --project generator/VtkSharp.Generator.Cli -- inspect-function <ClassName> <MethodName> --format json
```

## 3. Build the candidate whitelist

Create `examples/<Name>/candidate.yml`. There are two approaches:

**Approach A (recommended for new examples): targeted build**

For each new class, use `create-candidate --methods` to get only the needed methods:
```bash
dotnet run --project generator/VtkSharp.Generator.Cli -- create-candidate <NewClass> \
  -o /tmp/<name>_<class>.yml \
  --supported-only \
  --source-kind vtk-example --source-name <Name> \
  --source-original "<original-path>" \
  --methods Method1 Method2 ...
```

Then assemble all requirements into a single `examples/<Name>/candidate.yml`. Look at `examples/Cylinder/candidate.yml` for the exact format.

For existing classes that need additional methods (e.g. adding `GetProperty` to `vtkActor`), inspect the exact signature with `inspect-function` and add the entry directly to the candidate YAML manually — the candidate format supports adding methods to already-whitelisted classes.

**Approach B: full class export then prune**

If you need many methods from one class, use `create-candidate` without `--methods` to get all methods, then manually prune to only the needed entries. This produces very large YAML (1000+ lines for classes with deep inheritance).

**For methods on existing classes:** add them to the candidate's requirements list for that class. The candidate format supports adding entries for classes that already exist in the formal whitelist — `merge-candidate` will only add new function fingerprints.

Before merging, verify with `diff-whitelist`:
```bash
dotnet run --project generator/VtkSharp.Generator.Cli -- diff-whitelist examples/<Name>/candidate.yml
```
Check that only the expected methods are listed as "Added".

## 4. Merge candidate and regenerate

```bash
dotnet run --project generator/VtkSharp.Generator.Cli -- merge-candidate examples/<Name>/candidate.yml
dotnet run --project generator/VtkSharp.Generator.Cli -- generate-bindings --output-root src/
```

`merge-candidate` automatically normalizes the whitelist after merging.

## 5. Build native and managed code

```bash
cmake --build src/native/out/build/windows-x64-vs2022 --config Release
dotnet build src/bindings/VtkSharp.slnx
```

If there are compilation errors in generated code, diagnose the type mapping issue rather than editing generated files directly.

## 6. Smoke test

Copy the native DLL and temporarily put the example code in `src/bindings/TestConsole/Program.cs` to run it:
```bash
cp src/native/out/build/windows-x64-vs2022/Release/vtksharp_native.dll src/bindings/TestConsole/bin/Debug/net48/
PATH="C:/Program Files/VTK/bin:$PATH" dotnet run --project src/bindings/TestConsole -- --smoke
```

`--smoke` renders one frame and exits without starting the interactor event loop (the test calls `Render()` and prints a message but skips `Start()`). Confirm "Smoke test completed." appears with exit code 0.

After smoke test, restore TestConsole to its original state:
```bash
git checkout src/bindings/TestConsole/Program.cs
```

Do NOT commit TestConsole changes with the example.

## 7. Verify generation is clean

```bash
dotnet run --project generator/VtkSharp.Generator.Cli -- generate-bindings --check
```

This must print "Generated output is up to date." If it reports differences, regenerate (`--output-root src/`) and rebuild.

## 8. Write porting notes

Create `examples/<Name>/porting-notes.md` following the pattern in `examples/Cylinder/porting-notes.md`. Include:
- Original source path
- All VTK classes used and their whitelist status
- All APIs added (with module and class)
- Any deviations from the C++ original (unsupported types, inlined constants, etc.)

## 9. Commit

Review changes with `git status` and `git diff`. Commit message format:
```
添加 <Name> 示例：翻译 <original-class-name> 并补齐所需 API
```

## Rules

- **Never edit formal whitelist files directly.** Always go through candidate + merge-candidate.
- **Never commit changes to TestConsole/Program.cs.** It should always be restored after smoke testing.
- **Always use `--supported-only`** with create-candidate to filter out unsupported signatures.
- **Always fetch actual source** — do not translate from memory or examples.vtk.org screenshots.
- **Method goes on the declaring class** (use `inspect-function` to find where it's defined if the inheritance is unclear).
- Revert whitespace-only changes to `src/native/CMakeLists.txt`, `CMakePresets.json`, `vtksharp_api.h` caused by regeneration — these are line-ending artifacts.
- If a type is unsupported (e.g. returns a non-vtkObject pointer, uses `int&` out parameters, `std::string&`, etc.), do NOT add it to the whitelist. Instead, work around it in the C# port and document the deviation in porting-notes.md.
