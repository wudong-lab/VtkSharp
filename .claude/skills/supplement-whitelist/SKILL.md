---
name: supplement-whitelist
description: Scan a reference directory of *_export_gen.cpp files, extract VTK classes and methods, and add both to the VtkSharp binding whitelist via create-candidate + merge-candidate. Trigger when the user asks to import, supplement, or add VTK interfaces from a reference native export directory.
argument-hint: <reference-directory-path>
tools: Bash, Read, Write, Edit, Grep, Glob
---

# Supplement Whitelist from Reference Exports

Follow this workflow exactly to extract VTK classes and methods from a reference directory of `*_export_gen.cpp` files and add them to the VtkSharp binding whitelist.

All commands run from repository root.

CLI binary: `dotnet run --project generator/VtkSharp.Generator.Cli --`

## Prerequisites

- A reference directory containing `*_export_gen.cpp` files (e.g. from BrdiVtkNet or another VTK .NET binding project)
- Each file `#include`s a VTK class header; `VTK_NET_API` lines export individual methods

## 1. Scan reference directory — extract class names and method names

List `*_export_gen.cpp` files in the reference directory. Read each one. Extract two kinds of information:

### 1a. Class name from `#include`

```cpp
#include <vtkClassName.h>       ← extract class name from here
```

→ `vtkClassName`

### 1b. Method names from `VTK_NET_API` export lines

Each export line follows this pattern:

```cpp
VTK_NET_API <returntype> vtk<ClassName>_<MethodName>(<params>) { ... }

// Overload disambiguation — trailing _<digits> suffix:
VTK_NET_API vtkIdType vtkCellArray_InsertNextCell_51(vtkCellArray* self, vtkCell * cell) { ... }
VTK_NET_API vtkIdType vtkCellArray_InsertNextCell_53(vtkCellArray* self, vtkIdList * pts) { ... }
```

**Parsing rules:**

1. Extract the C export function name from each `VTK_NET_API` line (the token immediately after the return type, before the opening `(`)
2. Strip the class prefix: remove `vtk<ClassName>_` from the start
3. Strip any trailing disambiguator suffix: remove `_\d+` from the end
4. The result is the VTK method name
5. **Skip `New`** — the generator handles this automatically; it is never a whitelist method
6. **Deduplicate** — multiple overloads with different `_\d+` suffixes map to the same method name (e.g. `InsertNextCell_51` and `InsertNextCell_53` both → `InsertNextCell`). Keep unique names only.

Example for `vtkCellArray_export_gen.cpp`:

| Export function name | Strip prefix | Strip suffix | Final |
|---|---|---|---|
| `vtkCellArray_New` | `New` | — | **skip** |
| `vtkCellArray_InsertNextCell_51` | `InsertNextCell_51` | `InsertNextCell` | `InsertNextCell` |
| `vtkCellArray_InsertNextCell_53` | `InsertNextCell_53` | `InsertNextCell` | dup → skip |
| `vtkCellArray_InsertCellPoint` | `InsertCellPoint` | — | `InsertCellPoint` |

→ Methods for `vtkCellArray`: `InsertNextCell`, `InsertCellPoint`

Do NOT read the companion `*_export.cpp` files (no `_gen` suffix) — they only contain `#include` lines, no exports.

**Build a per-class method list** for the entire directory:
```
vtkClassName1 → [MethodA, MethodB]
vtkClassName2 → [MethodC]
...
```

## 2. Verify all classes exist in VTK headers

For each extracted class name:

```bash
dotnet run --project generator/VtkSharp.Generator.Cli -- inspect-class <ClassName> --format json
```

Use `--format json` to get structured output containing `Name`, `Module`, `Header`, `BaseClassName`, and `HasStaticNew`.

If this fails (class not found / inspection failure), the class does not exist in the current VTK installation → **skip it and its methods**.

Record for each valid class:
- `Name`
- `Module` — the authoritative VTK module
- `Header` (e.g. `vtkLine.h`)
- `BaseClassName` (for diagnosing build failures in step 10)
- `Methods` — the list from step 1b

## 3. Check existing whitelist coverage

For each class:

```bash
grep -r "name: <ClassName>" generator/whitelist/
```

Build three categories:

| Category | Class | Methods | Action |
|---|---|---|---|
| **A: New class, has methods** | Not in whitelist | `[GetX, SetY]` | `create-candidate --methods` → `merge-candidate` |
| **B: New class, no methods beyond New()** | Not in whitelist | `[]` (only `New` in reference) | `create-candidate` without `--methods` → `merge-candidate` |
| **C: Existing class, has new methods** | Already whitelisted | `[ExtraMethod1]` | `create-candidate --methods ExtraMethod1` → `merge-candidate` (only adds new function fingerprints) |
| **D: Existing class, no new methods** | Already whitelisted | `[]` or methods already covered | Skip |

For categories B and D: if every class falls here (reference files only export `New()` for all classes), the older class-stub-only workflow was sufficient. But this skill always uses `create-candidate` + `merge-candidate` to be consistent — the CLI handles all cases correctly.

## 4. Build candidates and merge into formal whitelist

**For each class** in categories A, B, or C, run `create-candidate` to generate a candidate YAML with accurate type signatures, then `merge-candidate` to merge.

### 4a. Classes with methods (categories A, C)

```bash
# Create candidate with specific methods — always use --skip-missing-methods
dotnet run --project generator/VtkSharp.Generator.Cli -- create-candidate <ClassName> \
  -o /tmp/candidate_<ClassName>.yml \
  --supported-only \
  --skip-missing-methods \
  --source-kind manual --source-name from-reference-exports \
  --source-original "<reference-directory>/<ClassName>_export_gen.cpp" \
  --methods Method1 Method2 Method3 ...
```

`--supported-only` filters out methods whose parameter or return types are not supported by the generator (e.g. `unsigned long`, `basic_ostream&`, non-const references).

`--skip-missing-methods` tells the generator to warn about but not fail on method names that are not found. This is essential because method names parsed from BrdiVtkNet export files carry disambiguator suffixes (`_\d+`) that must be stripped — if an edge case slips through, the candidate is still generated with the correctly-matched methods rather than failing the entire batch. Review the warnings in the output to catch any systematic parsing issues.

If a method name is not found, `create-candidate` will emit:
```
Warning: method 'NameNotFound' not found on 'vtkXxx' — skipped.
```

Common causes for warnings:
- The disambiguator suffix (`_\d+`) was not fully stripped (check step 1b)
- The method exists only on a base class and `inspect-class` didn't return it (rare for VTK classes)
- The reference file exports a method that doesn't exist in the current VTK version

Treat warnings as informational — the candidate will still be generated and merged correctly. Do NOT manually add methods to the candidate YAML.

### 4b. Classes without methods — New() factory only (category B)

```bash
# Create candidate with just the class stub (no --methods)
dotnet run --project generator/VtkSharp.Generator.Cli -- create-candidate <ClassName> \
  -o /tmp/candidate_<ClassName>.yml \
  --supported-only \
  --source-kind manual --source-name from-reference-exports \
  --source-original "<reference-directory>/<ClassName>_export_gen.cpp"
```

### 4c. Review before merging

```bash
dotnet run --project generator/VtkSharp.Generator.Cli -- diff-whitelist /tmp/candidate_<ClassName>.yml
```

Check that only expected classes and methods are listed as "Added". No "Removed" entries should appear.

### 4d. Merge

```bash
dotnet run --project generator/VtkSharp.Generator.Cli -- merge-candidate /tmp/candidate_<ClassName>.yml
```

`merge-candidate` automatically normalizes after merging and reports the merged file path.

Process all classes (categories A/B/C) before proceeding to the next step.

## 5. Normalize

After all merge operations, run normalization to ensure consistent formatting across all module files:

```bash
dotnet run --project generator/VtkSharp.Generator.Cli -- normalize-whitelist
```

Review with `git diff generator/whitelist/` to confirm only intended modifications.

## 6. Validate

```bash
dotnet run --project generator/VtkSharp.Generator.Cli -- validate-whitelist
```

This checks:
- **Module placement** — class belongs to the declared module per VTK hierarchy
- **Function signatures** — each method matches a VTK header signature exactly
- **Schema compliance** — no missing required fields, invalid types

If validation reports a module mismatch, `merge-candidate` placed the class in the wrong module (should be rare since it uses `inspect-class` hierarchy data). Manually move the class entry to the correct whitelist file and re-normalize.

Loop (normalize → validate → fix) until validation passes.

## 7. Clean up temporary candidate files

```bash
rm /tmp/candidate_*.yml
```

Or leave them — they are outside the repo and won't be committed.

## 8. Regenerate bindings

```bash
dotnet run --project generator/VtkSharp.Generator.Cli -- generate-bindings --output-root src
```

This outputs:
- C# binding files → `src/bindings/VtkSharp/<Module>/<ClassName>_gen.cs`
- C++ export files → `src/native/src/<Module>/<ClassName>_export_gen.cpp`

## 9. Build and resolve inheritance gaps

```bash
dotnet build src/bindings/VtkSharp.slnx
```

If compilation fails with a missing base class:

```
error CS0246: The type or namespace name 'vtkXxxBase' could not be found
```

This means a newly added class inherits from a base class that is not yet whitelisted. To resolve:
1. Note the missing base class name
2. Run `inspect-class <MissingBaseClass> --format json` to get its `Module` and `Header`
3. Create a candidate for it: `create-candidate <MissingBaseClass> -o /tmp/candidate_<MissingBaseClass>.yml --source-kind manual --source-name from-reference-exports --source-original "<reference-directory>"`
4. `merge-candidate /tmp/candidate_<MissingBaseClass>.yml`
5. Re-run normalize → validate → regenerate → build

## 10. Final verification

```bash
dotnet run --project generator/VtkSharp.Generator.Cli -- generate-bindings --check
```

Must print: `Generated output is up to date.`

## 11. Cross-reference: compare generated exports against reference (optional)

For a completeness check, compare each generated `*_export_gen.cpp` against the reference:

```bash
# For each class, show methods exported by reference but NOT by VtkSharp:
for cls in <class1> <class2> ...; do
  echo "=== $cls ==="
  grep "VTK_NET_API" "<ref-dir>/${cls}_export_gen.cpp" | sed 's/.*VTK_NET_API //' | while read line; do
    method_name=$(echo "$line" | sed 's/.*vtk'"${cls}"'_\([A-Za-z][^(]*\).*/\1/' | sed 's/_[0-9]\+$//')
    if ! grep -q "$method_name" "src/native/src/<Module>/${cls}_export_gen.cpp" 2>/dev/null; then
      echo "  Missing: $method_name (in reference, not in VtkSharp)"
    fi
  done
done
```

Differences are expected when:
- The reference has methods with unsupported types (filtered by `--supported-only`)
- The reference has methods the generator's `inspect-class` didn't return (e.g. manually added exports in the reference not present in VTK headers)

This step helps identify gaps but does not block completion.

## 12. Summary

Report:
- Reference directory scanned
- Total `*_export_gen.cpp` files found
- Classes skipped (not found in VTK headers)
- Candidate YAML files created and merged (count)
- Total methods added across all classes
- Any base classes discovered and added during step 9
- Build result (success/failure, error/warning count)

## Rules

- **Always verify class existence** via `inspect-class` before creating a candidate.
- **Always use `create-candidate` + `merge-candidate`** to modify the formal whitelist. Never edit whitelist YAML directly.
- **Always use `--supported-only`** with `create-candidate` to filter unsupported types.
- **Strip disambiguator suffixes** from method names (step 1b) before passing to `--methods`.
- **Skip `New`** — it is a VTK factory function auto-generated by the binding generator, not a whitelist method.
- **`diff-whitelist` before `merge-candidate`** — review changes before they land in the formal whitelist.
- **Run `normalize-whitelist`** after all merge operations complete.
- **`validate-whitelist` is the authority** for module placement and signature correctness.
- **If build fails due to missing base class**, process the base class through the same create-candidate + merge-candidate flow.
- **Temporary candidate files** go to `/tmp/` (outside the repo). Clean up after step 7.
- Do NOT modify `generator/whitelist/` files directly.
