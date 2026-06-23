using VtkSharp.Generator.Core.Whitelist;

namespace VtkSharp.Generator.Tests;

public sealed class WhitelistLoaderTests
{
    [Fact]
    public void LoadFile_ReadsWhitelistDocument()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "TestData", "whitelist", "vtkRenderingCore.yml");
        var loader = new WhitelistLoader();

        var document = loader.LoadFile(path);

        Assert.Equal("vtkRenderingCore", document.Module);
        var actor = Assert.Single(document.Classes);
        Assert.Equal("vtkActor", actor.Name);
        var function = Assert.Single(actor.Functions);
        Assert.Equal("SetMapper", function.Name);
        Assert.Equal("void", function.Return.Type);
        Assert.Equal("vtkMapper*", function.Parameters[0].Type);
    }
}
