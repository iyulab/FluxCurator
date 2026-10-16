namespace FluxCurator.Tests.PII;

using System.Reflection;
using System.Text.RegularExpressions;
using global::FluxCurator.Core.Infrastructure.PII;

/// <summary>
/// Every built-in detector matches with a source-generated regex built from its own pattern and options. A regex constructed
/// at run time is interpreted under Native AOT (<see cref="RegexOptions.Compiled"/> needs code generation at run time), so
/// a detector added without one would quietly mask more slowly in a native executable.
/// </summary>
public class GeneratedRegexDetectorTests
{
    private static readonly PropertyInfo CompiledPattern =
        typeof(PIIDetectorBase).GetProperty("CompiledPattern", BindingFlags.Instance | BindingFlags.NonPublic)!;

    private static readonly PropertyInfo Pattern =
        typeof(PIIDetectorBase).GetProperty("Pattern", BindingFlags.Instance | BindingFlags.NonPublic)!;

    private static readonly PropertyInfo Options =
        typeof(PIIDetectorBase).GetProperty("RegexOptions", BindingFlags.Instance | BindingFlags.NonPublic)!;

    public static TheoryData<string> BuiltInDetectors()
    {
        var data = new TheoryData<string>();
        foreach (var type in BuiltInDetectorTypes())
            data.Add(type.FullName!);
        return data;
    }

    private static IEnumerable<Type> BuiltInDetectorTypes() =>
        typeof(PIIDetectorBase).Assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.IsPublic && typeof(PIIDetectorBase).IsAssignableFrom(t))
            .OrderBy(t => t.FullName, StringComparer.Ordinal);

    [Fact]
    public void TheScanSeesTheDetectors()
    {
        // Positive control: an empty enumeration would make the theory below pass vacuously.
        Assert.True(BuiltInDetectorTypes().Count() >= 20);
    }

    [Theory]
    [MemberData(nameof(BuiltInDetectors))]
    public void BuiltInDetector_UsesASourceGeneratedRegex_OfItsPatternAndOptions(string typeName)
    {
        var detector = (PIIDetectorBase)Activator.CreateInstance(typeof(PIIDetectorBase).Assembly.GetType(typeName)!)!;

        var regex = (Regex)CompiledPattern.GetValue(detector)!;
        var pattern = (string)Pattern.GetValue(detector)!;
        var options = (RegexOptions)Options.GetValue(detector)!;

        Assert.NotEqual(typeof(Regex), regex.GetType());
        Assert.Equal(pattern, regex.ToString());
        Assert.Equal(options & ~RegexOptions.Compiled, regex.Options & ~RegexOptions.Compiled);
    }

    [Fact]
    public void ACustomDetector_StillGetsARegexBuiltFromItsPattern()
    {
        var detector = new CustomDetector();

        var match = Assert.Single(detector.Detect("ticket ZX-9 opened"));
        Assert.Equal("ZX-9", match.Value);
    }

    private sealed class CustomDetector : PIIDetectorBase
    {
        public override global::FluxCurator.Core.Domain.PIIType PIIType => global::FluxCurator.Core.Domain.PIIType.Custom;

        public override string Name => "Custom";

        protected override string Pattern => @"ZX-\d";
    }
}
