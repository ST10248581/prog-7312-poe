// =============================================================================
// CODE ATTRIBUTION — Operator Overloading (Technical and Language Requirement 2)
//
// The overloaded arithmetic, comparison and conversion operators below, and the
// equality/ordering members kept consistent with them, were written with
// reference to:
//
//   [5] Microsoft Learn, "Operator overloading - Define unary, arithmetic,
//       equality, and comparison operators - C# reference" (the `Fraction`
//       struct example).
//       https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/operator-overloading
//   [6] Microsoft Learn, "Operator Overloads - Framework Design Guidelines"
//       (overload in a symmetric fashion; conversion-operator guidance).
//       https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/operator-overloads
//   [7] Microsoft Learn, "IEquatable<T> Interface" (Notes to Implementers:
//       override Equals/GetHashCode and overload ==/!= consistently).
//       https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1
//   [8] Microsoft Learn, "Structure types - C# reference" (`readonly struct`).
//       https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/struct
// =============================================================================

namespace SmartX.Api.Models.Telemetry;

/// <summary>
/// The measured load of one or more meters, as a value type that can be combined
/// arithmetically: <c>var total = meter1 + meter2;</c>.
/// <para>
/// Aggregation across a zone is the single most common operation in the dashboard,
/// so it is expressed as an operator rather than a helper method. The unit travels
/// with the value, and the operators refuse to mix units — adding kW to mm/s is a
/// bug, not a number.
/// </para>
/// </summary>
// Declared as an immutable `readonly struct` per the recommendation in [8], and as
// a small numeric-style value type — the case where [6] says operator overloads
// are appropriate.
public readonly struct SensorLoad : IEquatable<SensorLoad>, IComparable<SensorLoad>
{
    public SensorLoad(double value, string unit, int sampleCount = 1)
    {
        Value = value;
        Unit = unit ?? string.Empty;
        SampleCount = sampleCount;
    }

    /// <summary>The additive identity. Combining it with any load adopts that load's unit.</summary>
    public static SensorLoad Zero { get; } = new(0d, string.Empty, 0);

    /// <summary>Magnitude of the load, expressed in <see cref="Unit"/>.</summary>
    public double Value { get; }

    /// <summary>Unit of measure, e.g. "kW". Empty only on <see cref="Zero"/>.</summary>
    public string Unit { get; }

    /// <summary>How many meter readings were folded into this value.</summary>
    public int SampleCount { get; }

    /// <summary>Mean contribution per meter, handy when comparing zones of different sizes.</summary>
    public double Average => SampleCount == 0 ? 0d : Value / SampleCount;

    public bool IsEmpty => SampleCount == 0;

    // --- Arithmetic -------------------------------------------------------
    // `public static` binary/unary operator declarations, with at least one
    // operand of the declaring type, follow the rules and the `Fraction` example
    // in [5].

    /// <summary>Aggregate load: <c>Meter3 = Meter1 + Meter2</c>.</summary>
    public static SensorLoad operator +(SensorLoad left, SensorLoad right)
    {
        var unit = ReconcileUnit(left, right);
        return new SensorLoad(left.Value + right.Value, unit, left.SampleCount + right.SampleCount);
    }

    /// <summary>Delta comparison: how much more one meter is drawing than another.</summary>
    public static SensorLoad operator -(SensorLoad left, SensorLoad right)
    {
        var unit = ReconcileUnit(left, right);
        return new SensorLoad(left.Value - right.Value, unit, Math.Max(left.SampleCount, right.SampleCount));
    }

    /// <summary>Negation, so a delta can be flipped without rebuilding it.</summary>
    public static SensorLoad operator -(SensorLoad load) =>
        new(-load.Value, load.Unit, load.SampleCount);

    /// <summary>Scales a load, e.g. projecting an hourly draw across a shift.</summary>
    public static SensorLoad operator *(SensorLoad load, double factor) =>
        new(load.Value * factor, load.Unit, load.SampleCount);

    // --- Comparison -------------------------------------------------------
    // C# requires the comparison operators to be overloaded in pairs (`<`/`>`,
    // `<=`/`>=`, `==`/`!=`) — see [5] — which is also the symmetry guideline in
    // [6]. The conversion to double is `explicit` rather than implicit because
    // dropping the unit is a lossy conversion ([6]).

    public static bool operator >(SensorLoad left, SensorLoad right) => left.CompareTo(right) > 0;

    public static bool operator <(SensorLoad left, SensorLoad right) => left.CompareTo(right) < 0;

    public static bool operator >=(SensorLoad left, SensorLoad right) => left.CompareTo(right) >= 0;

    public static bool operator <=(SensorLoad left, SensorLoad right) => left.CompareTo(right) <= 0;

    public static bool operator ==(SensorLoad left, SensorLoad right) => left.Equals(right);

    public static bool operator !=(SensorLoad left, SensorLoad right) => !left.Equals(right);

    /// <summary>Explicit, because dropping the unit should be a deliberate act.</summary>
    public static explicit operator double(SensorLoad load) => load.Value;

    // --- Equality and ordering -------------------------------------------
    // [7] (Notes to Implementers) directs that a value type implementing
    // IEquatable<T> should also override Equals(object) and GetHashCode and
    // overload ==/!=, so that every equality test returns a consistent result,
    // and should implement IComparable<T> when instances can be ordered.

    public int CompareTo(SensorLoad other)
    {
        GuardUnits(this, other);
        return Value.CompareTo(other.Value);
    }

    public bool Equals(SensorLoad other) =>
        Value.Equals(other.Value) &&
        string.Equals(Unit, other.Unit, StringComparison.OrdinalIgnoreCase);

    public override bool Equals(object? obj) => obj is SensorLoad other && Equals(other);

    public override int GetHashCode() =>
        HashCode.Combine(Value, Unit.ToLowerInvariant());

    public override string ToString() =>
        SampleCount <= 1
            ? $"{Value:0.###} {Unit}".Trim()
            : $"{Value:0.###} {Unit} across {SampleCount} meters".Trim();

    // --- Unit safety ------------------------------------------------------

    private static string ReconcileUnit(SensorLoad left, SensorLoad right)
    {
        if (left.IsEmpty || string.IsNullOrEmpty(left.Unit))
        {
            return right.Unit;
        }

        if (right.IsEmpty || string.IsNullOrEmpty(right.Unit))
        {
            return left.Unit;
        }

        GuardUnits(left, right);
        return left.Unit;
    }

    private static void GuardUnits(SensorLoad left, SensorLoad right)
    {
        if (left.IsEmpty || right.IsEmpty ||
            string.IsNullOrEmpty(left.Unit) || string.IsNullOrEmpty(right.Unit))
        {
            return;
        }

        if (!string.Equals(left.Unit, right.Unit, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Cannot combine sensor loads measured in '{left.Unit}' and '{right.Unit}'.");
        }
    }
}
