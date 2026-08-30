using System;

namespace VtkSharp.Tests;

public sealed class VtkEnumBindingsTests
{
    [Fact]
    public void ImageInteractionMode_EnumAndConvenienceMethodsAgree()
    {
        using var style = vtkInteractorStyleImage.New();
        Assert.Equal(vtkInteractorStyleImage.InteractionMode.Image2D, style.GetInteractionMode());
        foreach (var mode in Enum.GetValues<vtkInteractorStyleImage.InteractionMode>())
        {
            style.SetInteractionMode(mode);
            Assert.Equal(mode, style.GetInteractionMode());
        }
        style.SetInteractionModeToImage3D();
        Assert.Equal(vtkInteractorStyleImage.InteractionMode.Image3D, style.GetInteractionMode());
        style.SetInteractionModeToImage2D();
        Assert.Equal(vtkInteractorStyleImage.InteractionMode.Image2D, style.GetInteractionMode());
        style.SetInteractionModeToImageSlicing();
        Assert.Equal(vtkInteractorStyleImage.InteractionMode.ImageSlicing, style.GetInteractionMode());
    }

    [Fact]
    public void ImageInteractionMode_UsesOriginalNativeClampBehavior()
    {
        using var style = vtkInteractorStyleImage.New();
        style.SetInteractionMode((vtkInteractorStyleImage.InteractionMode)int.MinValue);
        Assert.Equal(vtkInteractorStyleImage.InteractionMode.Image2D, style.GetInteractionMode());
        style.SetInteractionMode((vtkInteractorStyleImage.InteractionMode)int.MaxValue);
        Assert.Equal(vtkInteractorStyleImage.InteractionMode.ImageSlicing, style.GetInteractionMode());
    }

    [Fact]
    public void ArrowOrigin_NativeEnumRoundTripsAndPreservesUnnamedValue()
    {
        using var arrow = vtkArrowSource.New();
        foreach (var origin in Enum.GetValues<vtkArrowSource.ArrowOrigin>())
        {
            arrow.SetArrowOrigin(origin);
            Assert.Equal(origin, arrow.GetArrowOrigin());
        }
        arrow.SetArrowOriginToCenter();
        Assert.Equal(vtkArrowSource.ArrowOrigin.Center, arrow.GetArrowOrigin());
        // 此原生 enum class 的固定底层类型为 int，setter 不做范围限制。
        // 这里只验证传值，不在无效模式下执行几何生成。
        arrow.SetArrowOrigin((vtkArrowSource.ArrowOrigin)123);
        Assert.Equal(123, (int)arrow.GetArrowOrigin());
        arrow.SetArrowOriginToDefault();
        Assert.Equal(vtkArrowSource.ArrowOrigin.Default, arrow.GetArrowOrigin());
    }
}
