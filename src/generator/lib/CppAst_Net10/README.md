# CppAst_Net10

This directory contains the CppAst build currently used by `VtkSharp.Generator.Core`.

Reason:

- The NuGet `CppAst` 0.25.0 package currently fails while parsing VTK 9.5 headers with `The item belongs already to a container`.
- This build was taken from the existing `BrdiVtkNet` generator toolchain, where it successfully parses the same VTK 9.5 headers with `ClangSharp` 20.1.2.3.

This is a compatibility bridge for the generator MVP. Prefer replacing it with an official NuGet package or a documented source-built package once the VTK header parsing issue is resolved upstream.
