namespace FluxCurator.Tests;

using global::FluxCurator.Core.Domain;
using global::FluxCurator.Core.Infrastructure.PII;

public class CuratorPIIDetectorTests
{
    private const string Text = "Contact employee EMP-123456 for details.";

    [Fact]
    public void RegisterPIIDetector_MasksWithTheDetector()
    {
        var curator = new Curator()
            .WithPIIMasking()
            .RegisterPIIDetector(new EmployeeIdDetector());

        Assert.Equal("Contact employee [PII] for details.", curator.MaskPII(Text).MaskedText);
    }

    [Fact]
    public void WithPIIMasking_AfterRegisterPIIDetector_KeepsTheDetector()
    {
        // Reconfiguring the options rebuilds the masker; a registered detector must not silently drop out.
        var curator = new Curator()
            .WithPIIMasking()
            .RegisterPIIDetector(new EmployeeIdDetector())
            .WithPIIMasking(PIIMaskingOptions.ForLanguage("ko"));

        Assert.Equal("Contact employee [PII] for details.", curator.MaskPII(Text).MaskedText);
    }

    private sealed class EmployeeIdDetector : PIIDetectorBase
    {
        public override PIIType PIIType => PIIType.Custom;
        public override string Name => "Employee ID Detector";
        protected override string Pattern => @"EMP-\d{6}";

        protected override bool ValidateMatch(string value, out float confidence)
        {
            confidence = 0.95f;
            return true;
        }
    }
}
