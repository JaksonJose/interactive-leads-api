using PhoneNumbers;

namespace InteractiveLeads.Application.Common.PhoneNumbers;

/// <summary>
/// Normalizes phone numbers to E.164 digits-only (no leading '+') for consistent storage and WhatsApp routing.
/// Uses Google's libphonenumber; for Brazil, applies the legacy mobile rule (8 national digits starting with 6–9 → prefix 9).
/// </summary>
public static class PhoneNumberNormalizer
{
    private static readonly PhoneNumberUtil Util = PhoneNumberUtil.GetInstance();

    /// <param name="raw">Any common format (+55…, 55…, national DDD+number, with spaces/punctuation).</param>
    /// <param name="defaultRegion">ISO 3166-1 alpha-2 region when number has no country code (default BR).</param>
    /// <returns>E.164 without '+', or empty if nothing parseable.</returns>
    public static string ToNormalizedDigits(string? raw, string defaultRegion = "BR")
    {
        if (string.IsNullOrWhiteSpace(raw))
            return string.Empty;

        var trimmed = raw.Trim();
        var digitsOnly = new string(trimmed.Where(char.IsDigit).ToArray());
        if (string.IsNullOrEmpty(digitsOnly))
            return string.Empty;

        if (digitsOnly.StartsWith("00", StringComparison.Ordinal))
            digitsOnly = digitsOnly[2..];

        var region = string.IsNullOrWhiteSpace(defaultRegion) ? "BR" : defaultRegion.Trim();
        digitsOnly = ApplyBrazilLegacyMobileNineDigitRule(digitsOnly, region);

        if (TryFormatE164Digits(digitsOnly, trimmed, region, out var e164))
            return e164;

        // Last resort: return digit-only string we have (may still be useful for logging / manual fix).
        return digitsOnly;
    }

    private static string ApplyBrazilLegacyMobileNineDigitRule(string digits, string defaultRegion)
    {
        if (!defaultRegion.Equals("BR", StringComparison.OrdinalIgnoreCase))
            return digits;

        // Already includes country code 55
        if (digits.StartsWith("55", StringComparison.Ordinal))
        {
            // 55 + DDD(2) + 8-digit mobile (legacy) → insert 9 after DDD
            if (digits.Length == 12)
            {
                var local = digits[4..];
                if (local.Length == 8 && IsBrazilLegacyMobileLocalFirstDigit(local[0]))
                    return string.Concat(digits.AsSpan(0, 4), "9", local);
            }

            return digits;
        }

        // National: DDD (2) + 8-digit mobile (legacy)
        if (digits.Length == 10)
        {
            var local = digits[2..];
            if (local.Length == 8 && IsBrazilLegacyMobileLocalFirstDigit(local[0]))
                return string.Concat(digits.AsSpan(0, 2), "9", local);
        }

        return digits;
    }

    private static bool IsBrazilLegacyMobileLocalFirstDigit(char c) => c is >= '6' and <= '9';

    private static bool TryFormatE164Digits(string digitsOnly, string originalTrimmed, string defaultRegion, out string e164Digits)
    {
        e164Digits = string.Empty;

        // Prefer international form when country code is present.
        if (digitsOnly.StartsWith("55", StringComparison.Ordinal) && digitsOnly.Length >= 12)
        {
            try
            {
                var n = Util.Parse("+" + digitsOnly, defaultRegion);
                if (Util.IsValidNumber(n))
                {
                    e164Digits = Util.Format(n, PhoneNumberFormat.E164).TrimStart('+');
                    return true;
                }
            }
            catch (NumberParseException)
            {
                // fall through
            }
        }

        // National or other formats: let lib parse with default region.
        try
        {
            var n = Util.Parse(originalTrimmed, defaultRegion);
            if (Util.IsValidNumber(n))
            {
                e164Digits = Util.Format(n, PhoneNumberFormat.E164).TrimStart('+');
                return true;
            }
        }
        catch (NumberParseException)
        {
            // try digits-only as national
        }

        try
        {
            var n = Util.Parse(digitsOnly, defaultRegion);
            if (Util.IsValidNumber(n))
            {
                e164Digits = Util.Format(n, PhoneNumberFormat.E164).TrimStart('+');
                return true;
            }
        }
        catch (NumberParseException)
        {
            return false;
        }

        return false;
    }
}
