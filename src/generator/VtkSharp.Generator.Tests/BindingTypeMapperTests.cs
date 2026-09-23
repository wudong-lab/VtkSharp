using VtkSharp.Generator.Core.Generation;

namespace VtkSharp.Generator.Tests;

[TestClass]
public sealed class BindingTypeMapperTests
{
    [TestMethod]
    [DataRow("void", "void", "void", "void")]
    [DataRow("char", "char", "char", "char")]
    [DataRow("unsigned char", "byte", "byte", "unsigned char")]
    [DataRow("unsigned int", "uint", "uint", "unsigned int")]
    [DataRow("unsigned long", "ulong", "ulong", "std::uint64_t")]
    [DataRow("long long", "long", "long", "long long")]
    [DataRow("vtkTypeBool", "bool", "int", "vtkTypeBool")]
    [DataRow("vtkIdType", "long", "long", "vtkIdType")]
    [DataRow("const char*", "string", "nint", "const char*")]
    [DataRow("void*", "nint", "nint", "void*")]
    [DataRow("HWND", "nint", "nint", "void*")]
    public void MapsScalarTypes(string type, string csharpPublic, string csharpInterop, string cppExport)
    {
        Assert.IsTrue(BindingTypeMapper.IsSupportedType(type));
        Assert.AreEqual(csharpPublic, BindingTypeMapper.ToCSharpPublicType(type));
        Assert.AreEqual(csharpInterop, BindingTypeMapper.ToCSharpInteropType(type));
        Assert.AreEqual(cppExport, BindingTypeMapper.ToCppExportType(type));
    }

    [TestMethod]
    public void MapsVtkClassPointer()
    {
        Assert.IsTrue(BindingTypeMapper.IsSupportedType("vtkActor*"));
        Assert.AreEqual("vtkActor", BindingTypeMapper.ToCSharpPublicType("vtkActor*"));
        Assert.AreEqual("nint", BindingTypeMapper.ToCSharpInteropType("vtkActor*"));
        Assert.AreEqual("vtkActor*", BindingTypeMapper.ToCppExportType("vtkActor*"));
    }

    [TestMethod]
    public void MapsVtkColor3ubValueStruct()
    {
        Assert.IsTrue(BindingTypeMapper.IsSupportedType("vtkColor3ub"));
        Assert.AreEqual("VtkColor3ub", BindingTypeMapper.ToCSharpPublicType("vtkColor3ub"));
        Assert.AreEqual("void", BindingTypeMapper.ToCSharpInteropType("vtkColor3ub"));
        Assert.AreEqual("unsigned char", TypeClassifier.GetValueStructCppElementType("vtkColor3ub"));
        Assert.AreEqual("byte", TypeClassifier.GetValueStructCSharpElementType("vtkColor3ub"));
    }

    [TestMethod]
    public void MapsVtkStdStringReturn()
    {
        Assert.IsTrue(BindingTypeMapper.IsSupportedType("vtkStdString"));
        Assert.AreEqual("string", BindingTypeMapper.ToCSharpPublicType("vtkStdString"));
        Assert.AreEqual("void", BindingTypeMapper.ToCSharpInteropType("vtkStdString"));
        Assert.AreEqual("void", BindingTypeMapper.ToCppExportType("vtkStdString"));
    }

    [TestMethod]
    public void MapsPrimitivePointersAndFixedArrays()
    {
        Assert.IsTrue(BindingTypeMapper.IsSupportedType("double*"));
        Assert.IsTrue(BindingTypeMapper.IsSupportedType("const double[3]"));
        Assert.AreEqual("double*", BindingTypeMapper.ToCSharpPublicType("double*"));
        Assert.AreEqual("ReadOnlySpan<double>", BindingTypeMapper.ToCSharpPublicType("const double[3]"));
        Assert.AreEqual("double*", BindingTypeMapper.ToCSharpInteropType("const double[3]"));
        Assert.AreEqual("const double*", BindingTypeMapper.ToCppExportType("const double[3]"));
        Assert.AreEqual("double", BindingTypeMapper.GetArrayElementType("const double[3]"));
        Assert.IsTrue(BindingTypeMapper.IsSupportedType("unsigned char*"));
        Assert.IsTrue(BindingTypeMapper.IsSupportedType("const unsigned char[4]"));
        Assert.AreEqual("byte*", BindingTypeMapper.ToCSharpPublicType("unsigned char*"));
        Assert.AreEqual("ReadOnlySpan<byte>", BindingTypeMapper.ToCSharpPublicType("const unsigned char[4]"));
        Assert.AreEqual("const unsigned char*", BindingTypeMapper.ToCppExportType("const unsigned char[4]"));
        Assert.IsTrue(BindingTypeMapper.IsSupportedType("unsigned char const[4]"));
        Assert.AreEqual("ReadOnlySpan<byte>", BindingTypeMapper.ToCSharpPublicType("unsigned char const[4]"));
        Assert.AreEqual("const unsigned char*", BindingTypeMapper.ToCppExportType("unsigned char const[4]"));
    }

    [TestMethod]
    public void RejectsUnsupportedTypes()
    {
        Assert.IsFalse(BindingTypeMapper.IsSupportedType("std::ostream&"));
        Assert.IsFalse(BindingTypeMapper.IsSupportedType("const vtkVector3d&"));
    }
}
