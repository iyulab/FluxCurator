using FluxCurator.Core.Infrastructure.PII;

// One masking call through the default registration of detectors. Exits non-zero if any value survives, so a trimmed-away
// detector or a regex that fails in a native build reads as a failure rather than as a run that printed something.
const string phone = "010-1234-5678";
const string email = "kim@example.com";
const string residentNumber = "900101-1234568";
var line = $"user=김철수 phone={phone} mail={email} rrn={residentNumber} action=login";

var result = new PIIMasker().Mask(line);
Console.WriteLine(result.MaskedText);

string[] survivors = [.. new[] { phone, email, residentNumber }.Where(result.MaskedText.Contains)];
if (survivors.Length > 0)
{
    Console.Error.WriteLine($"Not masked: {string.Join(", ", survivors)}");
    return 1;
}

return 0;
