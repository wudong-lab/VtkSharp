using System;
using System.IO;
using System.Reflection;

namespace VtkSharp.Tests;

public sealed class VtkColor3dPresetTests
{
    [Fact]
    public void PresetSource_UsesRgbDivisionExpressions()
    {
        var sourcePath = Path.Combine(
            FindRepositoryRoot(),
            "src",
            "bindings",
            "VtkSharp",
            "Core",
            "VtkColor3d.cs");
        var source = File.ReadAllText(sourcePath);

        Assert.Contains("public static readonly VtkColor3d AliceBlue = new(240/255D, 248/255D, 255/255D);", source);
    }

    [Theory]
    [InlineData("Cyan", 0, 255, 255)]
    [InlineData("Magenta", 255, 0, 255)]
    [InlineData("LightCyan", 224, 255, 255)]
    [InlineData("SlateGray", 112, 128, 144)]
    [InlineData("FireBrick", 178, 34, 34)]
    [InlineData("BurlyWood", 222, 184, 135)]
    [InlineData("Eggshell", 252, 230, 201)]
    public void Presets_IncludeWebColorsAndDistinctVtkColors(string name, int r, int g, int b)
    {
        var color = GetPreset(name);

        Assert.Equal(r / 255.0, color.R, 12);
        Assert.Equal(g / 255.0, color.G, 12);
        Assert.Equal(b / 255.0, color.B, 12);
    }

    [Theory]
    [InlineData("SlateGrey")]
    [InlineData("DimGrey")]
    [InlineData("CyanWhite")]
    [InlineData("GoldenrodDark")]
    [InlineData("ParaViewBlueGrayBkg")]
    [InlineData("Firebrick")]
    [InlineData("Burlywood")]
    public void Presets_ExcludeLegacyAliasesAndParaViewColors(string name)
    {
        Assert.Null(GetPresetField(name));
    }

    private static VtkColor3d GetPreset(string name)
    {
        var field = GetPresetField(name);
        Assert.NotNull(field);
        return Assert.IsType<VtkColor3d>(field.GetValue(null));
    }

    private static FieldInfo? GetPresetField(string name)
        => typeof(VtkColor3d).GetField(name, BindingFlags.Public | BindingFlags.Static);

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "AGENTS.md")) &&
                Directory.Exists(Path.Combine(directory.FullName, "src")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root.");
    }
}
