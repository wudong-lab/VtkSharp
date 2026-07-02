using VtkSharp.Generator.Core.Generation;

namespace VtkSharp.Generator.Tests;

public sealed class BindingTypeMapperTests
{
    [Theory]
    [InlineData("void", "void", "void", "void")]
    [InlineData("char", "char", "char", "char")]
    [InlineData("unsigned int", "uint", "uint", "unsigned int")]
    [InlineData("unsigned long", "ulong", "ulong", "std::uint64_t")]
    [InlineData("long long", "long", "long", "long long")]
    [InlineData("vtkTypeBool", "bool", "int", "vtkTypeBool")]
    [InlineData("vtkIdType", "long", "long", "vtkIdType")]
    [InlineData("const char*", "string", "nint", "const char*")]
    [InlineData("void*", "nint", "nint", "void*")]
    [InlineData("HWND", "nint", "nint", "void*")]
    public void MapsScalarTypes(string type, string csharpPublic, string csharpInterop, string cppExport)
    {
        Assert.True(BindingTypeMapper.IsSupportedType(type));
        Assert.Equal(csharpPublic, BindingTypeMapper.ToCSharpPublicType(type));
        Assert.Equal(csharpInterop, BindingTypeMapper.ToCSharpInteropType(type));
        Assert.Equal(cppExport, BindingTypeMapper.ToCppExportType(type));
    }

    [Fact]
    public void MapsVtkClassPointer()
    {
        Assert.True(BindingTypeMapper.IsSupportedType("vtkActor*"));
        Assert.Equal("vtkActor", BindingTypeMapper.ToCSharpPublicType("vtkActor*"));
        Assert.Equal("nint", BindingTypeMapper.ToCSharpInteropType("vtkActor*"));
        Assert.Equal("vtkActor*", BindingTypeMapper.ToCppExportType("vtkActor*"));
    }

    [Fact]
    public void MapsPrimitivePointersAndFixedArrays()
    {
        Assert.True(BindingTypeMapper.IsSupportedType("double*"));
        Assert.True(BindingTypeMapper.IsSupportedType("const double[3]"));
        Assert.Equal("double*", BindingTypeMapper.ToCSharpPublicType("double*"));
        Assert.Equal("ReadOnlySpan<double>", BindingTypeMapper.ToCSharpPublicType("const double[3]"));
        Assert.Equal("double*", BindingTypeMapper.ToCSharpInteropType("const double[3]"));
        Assert.Equal("const double*", BindingTypeMapper.ToCppExportType("const double[3]"));
        Assert.Equal("double", BindingTypeMapper.GetArrayElementType("const double[3]"));
    }

    [Fact]
    public void RejectsUnsupportedTypes()
    {
        Assert.False(BindingTypeMapper.IsSupportedType("std::ostream&"));
        Assert.False(BindingTypeMapper.IsSupportedType("const vtkVector3d&"));
    }
}
