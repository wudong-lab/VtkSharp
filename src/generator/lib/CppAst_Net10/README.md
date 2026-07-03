# CppAst_Net10

This directory contains a patched build of CppAst from [CppAst.NET](https://github.com/xoofx/CppAst.NET) v0.25.0.

## Source

Built from `https://github.com/xoofx/CppAst.NET` at version 0.25.0, with ClangSharp 21.1.8.3.

## Patch

`CppContainerList<T>.Add()` (and `Insert`) were patched to allow re-parenting of elements. The stock NuGet 0.25.0 throws `ArgumentException("The item belongs already to a container")` when parsing VTK 9.5 headers.

Root cause: during type resolution, `GetCppType` returns type instances that already belong to one container (e.g., a `CppClass` in `compilation.Classes`). When these same type instances are collected as template parameters and added to `CppUnexposedType.TemplateParameters` via `AddRange`, the strict single-parent check fails.

The patch changes `Add` and `Insert` to silently allow an element to move to a new container (re-parent), while still skipping duplicates in the same container:

```csharp
// Before (stock NuGet 0.25.0):
public void Add(TElement item)
{
    if (item.Parent != null)
        throw new ArgumentException("The item belongs already to a container");
    item.Parent = Container;
    _elements.Add(item);
}

// After (patched):
public void Add(TElement item)
{
    if (item.Parent == Container)
        return; // Already in this container
    item.Parent = Container;
    _elements.Add(item);
}
```

## Rebuilding

```bash
git clone https://github.com/xoofx/CppAst.NET.git
# Apply the patch above to src/CppAst/CppContainerList.cs
dotnet build src/CppAst/CppAst.csproj -c Release
cp src/CppAst/bin/Release/net8.0/CppAst.dll <this-directory>/
```

## Upstream

When the upstream NuGet package fixes this issue, this local DLL can be replaced with a `<PackageReference Include="CppAst" Version="..." />`.
