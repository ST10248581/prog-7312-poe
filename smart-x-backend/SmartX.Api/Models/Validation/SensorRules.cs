using System.Text.RegularExpressions;

namespace SmartX.Api.Models.Validation;

/// <summary>
/// The registration rules for a sensor, in one place. The request models use
/// these patterns in their validation attributes, and the frontend mirrors the
/// same expressions (src/utils/validation.ts) so a value is judged identically
/// on both sides.
/// </summary>
public static class SensorRules
{
    /// <summary>Six hex octets separated by colons or hyphens, e.g. 5C:A1:2A:2C:3C:6F.</summary>
    public const string MacAddressPattern = "^([0-9A-Fa-f]{2}[:-]){5}[0-9A-Fa-f]{2}$";

    /// <summary>Category prefix and a number, e.g. ENV-016 or NODE-0042.</summary>
    public const string NodeIdPattern = "^[A-Za-z]{2,5}-[0-9]{3,4}$";

    /// <summary>A letter or digit first, then letters, digits, spaces and . _ - ( ).</summary>
    public const string NamePattern = @"^[A-Za-z0-9][A-Za-z0-9 ._\-()]*$";

    /// <summary>A letter or digit first, then letters, digits, spaces and . - /.</summary>
    public const string RoomPattern = @"^[A-Za-z0-9][A-Za-z0-9 .\-/]*$";

    /// <summary>The mesh is partitioned into lettered zones, e.g. Zone A.</summary>
    public const string ZonePattern = "^Zone [A-Z]$";

    public const int NameMinLength = 3;
    public const int NameMaxLength = 60;
    public const int RoomMinLength = 2;
    public const int RoomMaxLength = 40;
}

/// <summary>Parsing and canonical formatting for MAC addresses.</summary>
public static partial class MacAddress
{
    [GeneratedRegex(SensorRules.MacAddressPattern)]
    private static partial Regex Pattern();

    /// <summary>
    /// The canonical form - upper case, colon separated - or null if the value is
    /// not a MAC address. "5c-a1-2a-2c-3c-6f" and "5C:A1:2A:2C:3C:6F" are the same
    /// device, so everything is compared in this form.
    /// </summary>
    public static string? Normalise(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return Pattern().IsMatch(trimmed)
            ? trimmed.Replace('-', ':').ToUpperInvariant()
            : null;
    }
}
