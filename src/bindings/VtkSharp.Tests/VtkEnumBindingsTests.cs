using System;

namespace VtkSharp.Tests;

[TestClass]
public sealed class VtkEnumBindingsTests
{
    [TestMethod]
    public void ImageInteractionMode_EnumAndConvenienceMethodsAgree()
    {
        using var style = vtkInteractorStyleImage.New();
        Assert.AreEqual(vtkInteractorStyleImage.InteractionMode.Image2D, style.GetInteractionMode());
        foreach (var mode in Enum.GetValues<vtkInteractorStyleImage.InteractionMode>())
        {
            style.SetInteractionMode(mode);
            Assert.AreEqual(mode, style.GetInteractionMode());
        }
        style.SetInteractionModeToImage3D();
        Assert.AreEqual(vtkInteractorStyleImage.InteractionMode.Image3D, style.GetInteractionMode());
        style.SetInteractionModeToImage2D();
        Assert.AreEqual(vtkInteractorStyleImage.InteractionMode.Image2D, style.GetInteractionMode());
        style.SetInteractionModeToImageSlicing();
        Assert.AreEqual(vtkInteractorStyleImage.InteractionMode.ImageSlicing, style.GetInteractionMode());
    }

    [TestMethod]
    public void ImageInteractionMode_UsesOriginalNativeClampBehavior()
    {
        using var style = vtkInteractorStyleImage.New();
        style.SetInteractionMode((vtkInteractorStyleImage.InteractionMode)int.MinValue);
        Assert.AreEqual(vtkInteractorStyleImage.InteractionMode.Image2D, style.GetInteractionMode());
        style.SetInteractionMode((vtkInteractorStyleImage.InteractionMode)int.MaxValue);
        Assert.AreEqual(vtkInteractorStyleImage.InteractionMode.ImageSlicing, style.GetInteractionMode());
    }

    [TestMethod]
    public void ArrowOrigin_NativeEnumRoundTripsAndPreservesUnnamedValue()
    {
        using var arrow = vtkArrowSource.New();
        foreach (var origin in Enum.GetValues<vtkArrowSource.ArrowOrigin>())
        {
            arrow.SetArrowOrigin(origin);
            Assert.AreEqual(origin, arrow.GetArrowOrigin());
        }
        arrow.SetArrowOriginToCenter();
        Assert.AreEqual(vtkArrowSource.ArrowOrigin.Center, arrow.GetArrowOrigin());
        // 此原生 enum class 的固定底层类型为 int，setter 不做范围限制。
        // 这里只验证传值，不在无效模式下执行几何生成。
        arrow.SetArrowOrigin((vtkArrowSource.ArrowOrigin)123);
        Assert.AreEqual(123, (int)arrow.GetArrowOrigin());
        arrow.SetArrowOriginToDefault();
        Assert.AreEqual(vtkArrowSource.ArrowOrigin.Default, arrow.GetArrowOrigin());
    }
}
