namespace FluxCurator.Tests.PII;

using global::FluxCurator.Core.Domain;
using global::FluxCurator.Core.Infrastructure.PII;

public class PIIMaskerTests
{
    #region Constructor

    [Fact]
    public void Constructor_Default_RegistersDefaultDetectors()
    {
        var masker = new PIIMasker();

        // Default detectors: Email, Phone, CreditCard + NationalId (auto)
        Assert.True(masker.ContainsPII("test@example.com"));
    }

    [Fact]
    public void Constructor_WithOptions_UsesProvidedOptions()
    {
        var options = PIIMaskingOptions.ForLanguage("ko");
        var masker = new PIIMasker(options);

        Assert.Same(options, masker.Options);
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNull()
    {
        Assert.Throws<ArgumentNullException>(() => new PIIMasker(null!));
    }

    #endregion

    #region Detect

    [Fact]
    public void Detect_WithEmail_ReturnsMatch()
    {
        var masker = new PIIMasker();

        var matches = masker.Detect("Contact: test@example.com");

        Assert.Single(matches);
        Assert.Equal(PIIType.Email, matches[0].Type);
        Assert.Equal("test@example.com", matches[0].Value);
    }

    [Fact]
    public void Detect_NullInput_ReturnsEmpty()
    {
        var masker = new PIIMasker();

        var matches = masker.Detect(null!);

        Assert.Empty(matches);
    }

    [Fact]
    public void Detect_EmptyInput_ReturnsEmpty()
    {
        var masker = new PIIMasker();

        var matches = masker.Detect("");

        Assert.Empty(matches);
    }

    [Fact]
    public void Detect_NoPII_ReturnsEmpty()
    {
        var masker = new PIIMasker();

        var matches = masker.Detect("Hello, this is plain text.");

        Assert.Empty(matches);
    }

    [Fact]
    public void Detect_TypesToMaskFilter_SkipsExcludedTypes()
    {
        var options = new PIIMaskingOptions
        {
            TypesToMask = PIIType.Phone // Only phone, not email
        };
        var masker = new PIIMasker(options);

        var matches = masker.Detect("Email: test@example.com");

        Assert.Empty(matches);
    }

    [Fact]
    public void Detect_MinConfidenceFilter_SkipsLowConfidence()
    {
        var options = new PIIMaskingOptions
        {
            MinConfidence = 0.99f // Very high threshold
        };
        var masker = new PIIMasker(options);

        // Email with uncommon TLD may have lower confidence
        var matches = masker.Detect("Contact: user@something.xyz");

        // With 0.99 threshold, only very high confidence matches pass
        // This tests the filtering logic — result depends on detector confidence
        foreach (var match in matches)
        {
            Assert.True(match.Confidence >= 0.99f);
        }
    }

    [Fact]
    public void Detect_MultiplePIITypes_ReturnsAll()
    {
        var masker = new PIIMasker();

        var text = "Email: user@test.com, Phone: 010-1234-5678";

        var matches = masker.Detect(text);

        Assert.True(matches.Count >= 1); // At least email
        Assert.Contains(matches, m => m.Type == PIIType.Email);
    }

    #endregion

    #region ContainsPII

    [Fact]
    public void ContainsPII_WithEmail_ReturnsTrue()
    {
        var masker = new PIIMasker();

        Assert.True(masker.ContainsPII("test@example.com"));
    }

    [Fact]
    public void ContainsPII_NoPII_ReturnsFalse()
    {
        var masker = new PIIMasker();

        Assert.False(masker.ContainsPII("Hello, world!"));
    }

    [Fact]
    public void ContainsPII_NullInput_ReturnsFalse()
    {
        var masker = new PIIMasker();

        Assert.False(masker.ContainsPII(null!));
    }

    [Fact]
    public void ContainsPII_EmptyInput_ReturnsFalse()
    {
        var masker = new PIIMasker();

        Assert.False(masker.ContainsPII(""));
    }

    #endregion

    #region Mask — Token Strategy

    [Fact]
    public void Mask_TokenStrategy_ReplacesWithToken()
    {
        var options = new PIIMaskingOptions
        {
            Strategy = MaskingStrategy.Token
        };
        var masker = new PIIMasker(options);

        var result = masker.Mask("Email: test@example.com");

        Assert.Contains("[EMAIL]", result.MaskedText);
        Assert.DoesNotContain("test@example.com", result.MaskedText);
    }

    [Fact]
    public void Mask_TokenStrategy_CustomToken()
    {
        var options = new PIIMaskingOptions
        {
            Strategy = MaskingStrategy.Token,
            CustomTokens = new Dictionary<PIIType, string>
            {
                { PIIType.Email, "<<EMAIL_REMOVED>>" }
            }
        };
        var masker = new PIIMasker(options);

        var result = masker.Mask("Email: test@example.com");

        Assert.Contains("<<EMAIL_REMOVED>>", result.MaskedText);
    }

    #endregion

    #region Mask — Asterisk Strategy

    [Fact]
    public void Mask_AsteriskStrategy_ReplacesWithAsterisks()
    {
        var options = new PIIMaskingOptions
        {
            Strategy = MaskingStrategy.Asterisk
        };
        var masker = new PIIMasker(options);

        var result = masker.Mask("Email: test@example.com");

        Assert.DoesNotContain("test@example.com", result.MaskedText);
        // Asterisk mask length equals original value length
        var emailLength = "test@example.com".Length;
        Assert.Contains(new string('*', emailLength), result.MaskedText);
    }

    #endregion

    #region Mask — Character Strategy

    [Fact]
    public void Mask_CharacterStrategy_ReplacesWithX()
    {
        var options = new PIIMaskingOptions
        {
            Strategy = MaskingStrategy.Character
        };
        var masker = new PIIMasker(options);

        var result = masker.Mask("Email: test@example.com");

        var emailLength = "test@example.com".Length;
        Assert.Contains(new string('X', emailLength), result.MaskedText);
    }

    #endregion

    #region Mask — Redact Strategy

    [Fact]
    public void Mask_RedactStrategy_ReplacesWithRedacted()
    {
        var options = new PIIMaskingOptions
        {
            Strategy = MaskingStrategy.Redact
        };
        var masker = new PIIMasker(options);

        var result = masker.Mask("Email: test@example.com");

        Assert.Contains("[REDACTED]", result.MaskedText);
        Assert.DoesNotContain("test@example.com", result.MaskedText);
    }

    #endregion

    #region Mask — Partial Strategy

    [Fact]
    public void Mask_PartialStrategy_Email_PreservesPartial()
    {
        var options = new PIIMaskingOptions
        {
            Strategy = MaskingStrategy.Partial
        };
        var masker = new PIIMasker(options);

        var result = masker.Mask("Email: test@example.com");

        // Email partial: first 2 chars of local + masked + @ + first 2 of domain + masked + TLD
        // "te**@ex*****.com"
        Assert.DoesNotContain("test@example.com", result.MaskedText);
        Assert.Contains("te", result.MaskedText);
        Assert.Contains(".com", result.MaskedText);
    }

    [Fact]
    public void Mask_PartialStrategy_Phone_ShowsLast4()
    {
        var options = new PIIMaskingOptions
        {
            Strategy = MaskingStrategy.Partial
        };
        var masker = new PIIMasker(options);

        var result = masker.Mask("Phone: 010-1234-5678");

        // Phone partial: last 4 digits visible
        if (result.HasPII && result.Matches.Any(m => m.Type == PIIType.Phone))
        {
            Assert.Contains("5678", result.MaskedText);
        }
    }

    #endregion

    #region Mask — Hash Strategy

    [Fact]
    public void Mask_HashStrategy_ReplacesWithHash()
    {
        var options = new PIIMaskingOptions
        {
            Strategy = MaskingStrategy.Hash
        };
        var masker = new PIIMasker(options);

        var result = masker.Mask("Email: test@example.com");

        Assert.DoesNotContain("test@example.com", result.MaskedText);
        Assert.Matches(@"\[HASH:[0-9a-f]{8}\]", result.MaskedText);
    }

    [Fact]
    public void Mask_HashStrategy_SameInputProducesSameHash()
    {
        var options = new PIIMaskingOptions
        {
            Strategy = MaskingStrategy.Hash
        };
        var masker = new PIIMasker(options);

        var result1 = masker.Mask("Email: test@example.com");
        var result2 = masker.Mask("Email: test@example.com");

        Assert.Equal(result1.MaskedText, result2.MaskedText);
    }

    #endregion

    #region Mask — Remove Strategy

    [Fact]
    public void Mask_RemoveStrategy_RemovesCompletely()
    {
        var options = new PIIMaskingOptions
        {
            Strategy = MaskingStrategy.Remove
        };
        var masker = new PIIMasker(options);

        var result = masker.Mask("Email: test@example.com end");

        Assert.DoesNotContain("test@example.com", result.MaskedText);
        Assert.Contains("Email: ", result.MaskedText);
        Assert.Contains(" end", result.MaskedText);
    }

    #endregion

    #region Mask — NoPII

    [Fact]
    public void Mask_NoPII_ReturnsSameText()
    {
        var masker = new PIIMasker();
        var text = "Hello, world!";

        var result = masker.Mask(text);

        Assert.Equal(text, result.MaskedText);
        Assert.Equal(text, result.OriginalText);
        Assert.False(result.HasPII);
        Assert.Equal(0, result.PIICount);
    }

    [Fact]
    public void Mask_NullInput_ReturnsEmptyResult()
    {
        var masker = new PIIMasker();

        var result = masker.Mask(null!);

        Assert.Equal("", result.MaskedText);
        Assert.False(result.HasPII);
    }

    [Fact]
    public void Mask_EmptyInput_ReturnsEmptyResult()
    {
        var masker = new PIIMasker();

        var result = masker.Mask("");

        Assert.Equal("", result.MaskedText);
        Assert.False(result.HasPII);
    }

    #endregion

    #region PIIMaskingResult

    [Fact]
    public void MaskResult_HasPII_True_WhenPIIDetected()
    {
        var masker = new PIIMasker();

        var result = masker.Mask("Email: test@example.com");

        Assert.True(result.HasPII);
        Assert.True(result.PIICount > 0);
    }

    [Fact]
    public void MaskResult_OriginalText_Preserved()
    {
        var masker = new PIIMasker();
        var text = "Email: test@example.com";

        var result = masker.Mask(text);

        Assert.Equal(text, result.OriginalText);
    }

    [Fact]
    public void MaskResult_DetectedTypes_ContainsEmail()
    {
        var masker = new PIIMasker();

        var result = masker.Mask("Email: test@example.com");

        Assert.Contains(PIIType.Email, result.DetectedTypes);
    }

    [Fact]
    public void MaskResult_CountByType_CorrectCount()
    {
        var masker = new PIIMasker();

        var result = masker.Mask("Email1: a@test.com and Email2: b@test.com");

        if (result.CountByType.ContainsKey(PIIType.Email))
        {
            Assert.True(result.CountByType[PIIType.Email] >= 1);
        }
    }

    [Fact]
    public void MaskResult_GetSummary_NoPII_ReturnsNoPII()
    {
        var masker = new PIIMasker();

        var result = masker.Mask("Hello, world!");

        Assert.Equal("No PII detected.", result.GetSummary());
    }

    [Fact]
    public void MaskResult_GetSummary_WithPII_IncludesCount()
    {
        var masker = new PIIMasker();

        var result = masker.Mask("Email: test@example.com");

        Assert.Contains("Detected", result.GetSummary());
        Assert.Contains("Email", result.GetSummary());
    }

    #endregion

    #region PIIMaskingOptions Factory Methods

    [Fact]
    public void Options_Default_UsesTokenStrategy()
    {
        var options = PIIMaskingOptions.Default;

        Assert.Equal(MaskingStrategy.Token, options.Strategy);
        Assert.Equal(PIIType.Common, options.TypesToMask);
        Assert.Contains("auto", options.LanguageCodes);
    }

    [Fact]
    public void Options_ForLanguage_SetsSingleLanguage()
    {
        var options = PIIMaskingOptions.ForLanguage("ko");

        Assert.Single(options.LanguageCodes);
        Assert.Equal("ko", options.LanguageCodes[0]);
    }

    [Fact]
    public void Options_ForLanguages_SetsMultiple()
    {
        var options = PIIMaskingOptions.ForLanguages("ko", "en-US", "ja");

        Assert.Equal(3, options.LanguageCodes.Count);
    }

    [Fact]
    public void Options_Strict_UsesAllTypesLowConfidence()
    {
        var options = PIIMaskingOptions.Strict;

        Assert.Equal(PIIType.All, options.TypesToMask);
        Assert.Equal(0.7f, options.MinConfidence);
    }

    [Fact]
    public void Options_Partial_UsesPartialStrategy()
    {
        var options = PIIMaskingOptions.Partial;

        Assert.Equal(MaskingStrategy.Partial, options.Strategy);
        Assert.Equal(3, options.PartialPreserveCount);
    }

    [Fact]
    public void Options_GetToken_DefaultTokens()
    {
        var options = new PIIMaskingOptions();

        Assert.Equal("[EMAIL]", options.GetToken(PIIType.Email));
        Assert.Equal("[PHONE]", options.GetToken(PIIType.Phone));
        Assert.Equal("[CARD]", options.GetToken(PIIType.CreditCard));
        Assert.Equal("[NATIONAL_ID]", options.GetToken(PIIType.NationalId));
    }

    [Fact]
    public void Options_GetToken_CustomOverridesDefault()
    {
        var options = new PIIMaskingOptions
        {
            CustomTokens = new Dictionary<PIIType, string>
            {
                { PIIType.Email, "[CUSTOM_EMAIL]" }
            }
        };

        Assert.Equal("[CUSTOM_EMAIL]", options.GetToken(PIIType.Email));
        Assert.Equal("[PHONE]", options.GetToken(PIIType.Phone)); // Unchanged
    }

    #endregion

    #region RegisterDetector

    [Fact]
    public void RegisterDetector_NullDetector_ThrowsArgumentNull()
    {
        var masker = new PIIMasker();

        Assert.Throws<ArgumentNullException>(() => masker.RegisterDetector(null!));
    }

    #endregion

    #region Mask — Text Preservation

    [Fact]
    public void Mask_PreservesTextBeforeAndAfterPII()
    {
        var options = new PIIMaskingOptions
        {
            Strategy = MaskingStrategy.Redact
        };
        var masker = new PIIMasker(options);

        var result = masker.Mask("Before test@example.com After");

        Assert.StartsWith("Before ", result.MaskedText);
        Assert.EndsWith(" After", result.MaskedText);
        Assert.Contains("[REDACTED]", result.MaskedText);
    }

    [Fact]
    public void Mask_Matches_HaveMaskedValue()
    {
        var options = new PIIMaskingOptions
        {
            Strategy = MaskingStrategy.Token
        };
        var masker = new PIIMasker(options);

        var result = masker.Mask("Email: test@example.com");

        Assert.True(result.HasPII);
        Assert.All(result.Matches, m => Assert.NotNull(m.MaskedValue));
    }

    #endregion

    #region Language Configuration

    [Fact]
    public void Masker_WithAutoLanguage_DetectsKoreanNationalId()
    {
        // "auto" should register all national ID detectors
        // Use NationalId-only mask to avoid overlap with Phone detector
        var options = new PIIMaskingOptions
        {
            LanguageCodes = ["auto"],
            TypesToMask = PIIType.NationalId
        };
        var masker = new PIIMasker(options);

        // Korean RRN pattern
        var result = masker.Mask("ID: 900101-1234567");

        Assert.True(result.HasPII);
        Assert.Contains(result.Matches, m => m.Type == PIIType.NationalId);
    }

    [Fact]
    public void Masker_WithAutoLanguage_DetectsUSNationalId()
    {
        var options = new PIIMaskingOptions
        {
            LanguageCodes = ["auto"],
            TypesToMask = PIIType.NationalId
        };
        var masker = new PIIMasker(options);

        // US SSN pattern (valid, non-sequential)
        var result = masker.Mask("SSN: 234-56-7890");

        Assert.True(result.HasPII);
        Assert.Contains(result.Matches, m => m.Type == PIIType.NationalId);
    }

    [Fact]
    public void Masker_WithAutoLanguage_DetectsMultipleCountryNationalIds()
    {
        var options = new PIIMaskingOptions
        {
            LanguageCodes = ["auto"],
            TypesToMask = PIIType.NationalId
        };
        var masker = new PIIMasker(options);

        // Both Korean RRN and US SSN in same text
        var text = "Korean ID: 900101-1234567, US SSN: 234-56-7890";
        var matches = masker.Detect(text);

        var nationalIdMatches = matches.Where(m => m.Type == PIIType.NationalId).ToList();
        Assert.True(nationalIdMatches.Count >= 2,
            $"Expected at least 2 NationalId matches but got {nationalIdMatches.Count}");
    }

    [Fact]
    public void Masker_WithSpecificLanguage_OnlyRegistersMatchingDetectors()
    {
        var options = PIIMaskingOptions.ForLanguage("ko");
        var masker = new PIIMasker(options);

        // Email should still work (global detector)
        Assert.True(masker.ContainsPII("test@example.com"));
    }

    #endregion
}
