using VtkSharp.Generator.Core.Whitelist;

namespace VtkSharp.Generator.Tests;

[TestClass]
public sealed class WhitelistLoaderTests
{
    [TestMethod]
    public void LoadFile_ReadsWhitelistDocument()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "TestData", "whitelist", "vtkRenderingCore.yml");
        var loader = new WhitelistLoader();

        var document = loader.LoadFile(path);

        Assert.AreEqual("vtkRenderingCore", document.Module);
        var actor = Enumerable.Single(document.Classes);
        Assert.AreEqual("vtkActor", actor.Name);
        var function = Enumerable.Single(actor.Functions);
        Assert.AreEqual("SetMapper", function.Name);
        Assert.AreEqual("void", function.Return.Type);
        Assert.AreEqual("vtkMapper*", function.Parameters[0].Type);
    }
}
