---
name: port-vtk-example
description: Translate or port a VTK C++ example from a URL, local source file, or example name into the VtkSharp C# ExampleBrowser, supplementing only the required VTK bindings through the candidate whitelist workflow. Use for VTK example translation, example migration, missing wrapper discovery, and example-specific binding additions. Do not use for unrelated VtkSharp API additions without an example.
---

# Port a VTK example

Work from the repository root. Read `references/workflow.md` completely before editing because it defines source acquisition, translation rules, binding generation, and validation.

Follow these invariants:

- Read the actual C++ source; do not translate from memory or screenshots.
- Match current ExampleBrowser structure and existing examples.
- Add only APIs exercised by the example.
- Find the declaring VTK class before creating a candidate.
- Modify formal whitelist files only through `create-candidate`, `diff-whitelist`, and `merge-candidate`.
- Always use `--supported-only`; do not force unsupported native signatures into generated bindings.
- Use managed `AddObserver` instead of exposing `vtkCallbackCommand` directly.
- Do not edit generated wrapper or export files to fix generator problems.
- Validate generation, native/managed builds, and the target example before completion.

Use PowerShell commands on Windows. Put temporary candidates outside the repository or under an ignored output directory.
