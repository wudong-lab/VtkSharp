---
name: supplement-whitelist
description: Import or supplement VtkSharp bindings by scanning a reference directory of VTK-style *_export_gen.cpp files, extracting referenced classes and methods, and adding supported APIs through the candidate whitelist workflow. Use when comparing or importing interfaces from VtkNet or another generated native export set. Do not use for a single known API or VTK example translation.
---

# Supplement the whitelist from reference exports

Work from the repository root. Read `references/workflow.md` completely before changing bindings.

Use `scripts/scan-reference-exports.ps1` for deterministic extraction; do not recreate the export-name parser ad hoc. Treat extracted names only as requests to inspect against the installed VTK version, never as authoritative signatures.

Follow these invariants:

- Verify every class and method through the VtkSharp generator CLI.
- Strip reference-export overload suffixes, deduplicate names, and skip `New`.
- Add only methods supported by the current generator and VTK version.
- Modify formal whitelist files only through candidate diff/merge commands.
- Resolve missing base wrappers through the same candidate workflow.
- Never manually patch generated wrapper/export files or generated CMake module lists as the normal fix.
- Validate whitelist, generated output, native build, and managed tests.

Use PowerShell on Windows and keep temporary candidates outside tracked source directories.
