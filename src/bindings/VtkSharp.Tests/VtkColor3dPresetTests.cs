using System;
using System.IO;
using System.Reflection;

namespace VtkSharp.Tests;

[TestClass]
public sealed class VtkColor3dPresetTests
{
    [TestMethod]
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

    [TestMethod]
    [DataRow("Cyan", 0, 255, 255)]
    [DataRow("GoldenRod", 218, 165, 32)]
    [DataRow("LightGray", 211, 211, 211)]
    [DataRow("Magenta", 255, 0, 255)]
    [DataRow("RebeccaPurple", 102, 51, 153)]
    [DataRow("LightCyan", 224, 255, 255)]
    [DataRow("SlateGray", 112, 128, 144)]
    [DataRow("FireBrick", 178, 34, 34)]
    [DataRow("BurlyWood", 222, 184, 135)]
    [DataRow("Eggshell", 252, 230, 201)]
    public void Presets_IncludeWebColorsAndDistinctVtkColors(string name, int r, int g, int b)
    {
        var color = GetPreset(name);

        Assert.AreEqual(r / 255.0, color.R, 12);
        Assert.AreEqual(g / 255.0, color.G, 12);
        Assert.AreEqual(b / 255.0, color.B, 12);
    }

    [TestMethod]
    [DataRow("SlateGrey")]
    [DataRow("DimGrey")]
    [DataRow("Goldenrod")]
    [DataRow("LightGrey")]
    [DataRow("CyanWhite")]
    [DataRow("GoldenrodDark")]
    [DataRow("ParaViewBlueGrayBkg")]
    [DataRow("Firebrick")]
    [DataRow("Burlywood")]
    public void Presets_ExcludeLegacyAliasesAndParaViewColors(string name)
    {
        Assert.IsNull(GetPresetField(name));
    }

    private static VtkColor3d GetPreset(string name)
    {
        var field = GetPresetField(name);
        Assert.IsNotNull(field);
        return Assert.IsInstanceOfType<VtkColor3d>(field.GetValue(null));
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
