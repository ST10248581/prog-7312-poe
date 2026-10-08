// =============================================================================
// CODE ATTRIBUTION — Generics (Technical and Language Requirement 1)
//
// The generic wrapper class, its `where T : struct` constraint and the
// allocation-free payload access in this file were written with reference to
// the sources below.
//
// Code Attribution [1]
// Author: Microsoft
// Year: 2026
// Title: Generic types and methods – C#
// Type: [Source code]
// Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/types/generics>
// Accessed: [Accessed 13 September 2026]
// Modifications: Adapted the generic class declaration pattern into
//   TelemetryPacket<T>, a sealed wrapper that carries one typed sensor reading
//   with its sensor id, reading type, unit, timestamp and quality flags,
//   exposed through the non-generic ITelemetryPacket interface so mixed
//   payloads share one list.
// Reference: Microsoft, 2026. Generic types and methods – C# [Source code] Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/types/generics> [Accessed 13 September 2026].
//
// Code Attribution [2]
// Author: Microsoft
// Year: 2025
// Title: Constraints on type parameters – C#
// Type: [Source code]
// Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/generics/constraints-on-type-parameters>
// Accessed: [Accessed 13 September 2026]
// Modifications: Applied the `where T : struct` constraint from the documented
//   examples so a packet only accepts non-nullable value types (float, double,
//   int, long, bool).
// Reference: Microsoft, 2025. Constraints on type parameters – C# [Source code] Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/generics/constraints-on-type-parameters> [Accessed 13 September 2026].
//
// Code Attribution [3]
// Author: Microsoft
// Year: 2025
// Title: Boxing and Unboxing – C#
// Type: [Source code]
// Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/types/boxing-and-unboxing>
// Accessed: [Accessed 13 September 2026]
// Modifications: No code copied. The documented cost of boxing was used to
//   decide to hold the payload in a strongly typed `T` field rather than an
//   `object` field.
// Reference: Microsoft, 2025. Boxing and Unboxing – C# [Source code] Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/types/boxing-and-unboxing> [Accessed 13 September 2026].
//
// Code Attribution [4]
// Author: Microsoft
// Year: 2025
// Title: Unsafe.As Method (System.Runtime.CompilerServices)
// Type: [Source code]
// Available at: <https://learn.microsoft.com/en-us/dotnet/api/system.runtime.compilerservices.unsafe.as>
// Accessed: [Accessed 13 September 2026]
// Modifications: Adapted the Unsafe.As<TFrom,TTo>(ref TFrom) reinterpret-cast
//   into TryGetNumeric and TryGetBoolean, guarding every cast with a `typeof(T)
//   == typeof(X)` check so it is only taken when the types match.
// Reference: Microsoft, 2025. Unsafe.As Method (System.Runtime.CompilerServices) [Source code] Available at: <https://learn.microsoft.com/en-us/dotnet/api/system.runtime.compilerservices.unsafe.as> [Accessed 13 September 2026].
// =============================================================================

using System.Runtime.CompilerServices;

namespace SmartX.Api.Models.Telemetry;

/// <summary>
/// How a gateway encoded a metric on the wire. The mesh is deliberately mixed:
/// cheap environmental nodes push 32-bit floats, meters push whole-unit counters,
/// and smart switches push a single trigger bit.
/// </summary>
public enum TelemetryPayloadKind
{
    Float,
    Double,
    Int,
    Bool
}

/// <summary>
/// Type-agnostic face of a packet. <see cref="TelemetryPacket{T}"/> is a reference
/// type, so packets carrying different payloads can sit in one
/// <c>List&lt;ITelemetryPacket&gt;</c> while each still holds its payload in a
/// strongly typed <c>T</c> field — the interface never boxes the value itself.
/// </summary>
public interface ITelemetryPacket
{
    Guid SensorProfileId { get; }
    ReadingType ReadingType { get; }
    string Unit { get; }
    DateTime TimestampUtc { get; }
    ReadingQuality Quality { get; }
    bool IsAnomaly { get; }

    /// <summary>The CLR type of the payload, for diagnostics and logging.</summary>
    Type PayloadType { get; }

    /// <summary>Projects the packet onto the persistence model.</summary>
    TelemetryReading ToReading();
}

/// <summary>
/// Reusable generic wrapper for one incoming metric.
/// <para>
/// <c>T</c> is constrained to <c>struct</c> and stored in a strongly typed field,
/// so a <see cref="TelemetryPacket{T}"/> of <c>float</c>, <c>int</c> or <c>bool</c>
/// never boxes its payload onto the heap. The JIT compiles a separate, specialised
/// body for every value-type instantiation, which matters on a resource-constrained
/// gateway ingesting tens of thousands of samples per batch: an <c>object</c>-based
/// wrapper would allocate once per reading and hand all of it to the GC.
/// </para>
/// </summary>
/// <typeparam name="T">The value type carried by the packet (float, double, int, bool, ...).</typeparam>
// Generic class declaration and the `where T : struct` (non-nullable value type)
// constraint follow the patterns documented in [1] and [2]; the reason for keeping
// the payload in a `T` field rather than an `object` field is set out in [3].
public sealed class TelemetryPacket<T> : ITelemetryPacket where T : struct
{
    public TelemetryPacket(
        Guid sensorProfileId,
        ReadingType readingType,
        T value,
        string unit,
        DateTime timestampUtc,
        ReadingQuality quality = ReadingQuality.Good,
        bool isAnomaly = false)
    {
        SensorProfileId = sensorProfileId;
        ReadingType = readingType;
        Value = value;
        Unit = unit;
        TimestampUtc = timestampUtc;
        Quality = quality;
        IsAnomaly = isAnomaly;
    }

    public Guid SensorProfileId { get; }
    public ReadingType ReadingType { get; }

    /// <summary>The payload, held as <c>T</c> rather than <c>object</c>.</summary>
    public T Value { get; }

    public string Unit { get; }
    public DateTime TimestampUtc { get; }
    public ReadingQuality Quality { get; }
    public bool IsAnomaly { get; }

    public Type PayloadType => typeof(T);

    /// <summary>
    /// Reads the payload as a double without boxing.
    /// <para>
    /// The <c>typeof(T) ==</c> tests are resolved by the JIT when it specialises
    /// this method for each value type, so the branches collapse to a constant and
    /// <see cref="Unsafe.As{TFrom,TTo}(ref TFrom)"/> reinterprets the field in
    /// place. The alternative — <c>Convert.ToDouble((object)Value)</c> — would box
    /// on every single sample.
    /// </para>
    /// </summary>
    // The `typeof(T) == typeof(X)` test followed by Unsafe.As<TFrom,TTo>(ref TFrom)
    // is the reinterpret-cast pattern documented in [4] (conceptually C++'s
    // reinterpret_cast); it replaces a cast through `object`, which would box on
    // every sample as described in [3].
    public bool TryGetNumeric(out double numeric)
    {
        var value = Value;

        if (typeof(T) == typeof(double))
        {
            numeric = Unsafe.As<T, double>(ref value);
            return true;
        }

        if (typeof(T) == typeof(float))
        {
            numeric = Unsafe.As<T, float>(ref value);
            return true;
        }

        if (typeof(T) == typeof(int))
        {
            numeric = Unsafe.As<T, int>(ref value);
            return true;
        }

        if (typeof(T) == typeof(long))
        {
            numeric = Unsafe.As<T, long>(ref value);
            return true;
        }

        if (typeof(T) == typeof(short))
        {
            numeric = Unsafe.As<T, short>(ref value);
            return true;
        }

        if (typeof(T) == typeof(bool))
        {
            numeric = Unsafe.As<T, bool>(ref value) ? 1d : 0d;
            return true;
        }

        numeric = default;
        return false;
    }

    /// <summary>Reads a switch-style payload as a bool without boxing.</summary>
    public bool TryGetBoolean(out bool flag)
    {
        var value = Value;

        if (typeof(T) == typeof(bool))
        {
            flag = Unsafe.As<T, bool>(ref value);
            return true;
        }

        flag = default;
        return false;
    }

    public TelemetryReading ToReading()
    {
        var reading = new TelemetryReading
        {
            SensorProfileId = SensorProfileId,
            ReadingType = ReadingType,
            Unit = Unit,
            TimestampUtc = TimestampUtc,
            Quality = Quality,
            IsAnomaly = IsAnomaly
        };

        // A switch trigger is stored as a bool; everything else keeps its numeric
        // form so the existing charting and threshold code needs no special case.
        if (TryGetBoolean(out var flag))
        {
            reading.BooleanValue = flag;
            reading.NumericValue = flag ? 1d : 0d;
        }
        else if (TryGetNumeric(out var numeric))
        {
            reading.NumericValue = numeric;
        }
        else
        {
            reading.TextValue = Value.ToString();
            reading.Quality = ReadingQuality.Suspect;
        }

        return reading;
    }

    public override string ToString() =>
        $"{ReadingType} {Value} {Unit} @ {TimestampUtc:u} [{typeof(T).Name}]";
}

/// <summary>
/// Non-generic companion so packets can be built with type inference:
/// <c>TelemetryPacket.Create(id, ReadingType.Temperature, 21.4f, "°C", now)</c>.
/// </summary>
public static class TelemetryPacket
{
    public static TelemetryPacket<T> Create<T>(
        Guid sensorProfileId,
        ReadingType readingType,
        T value,
        string unit,
        DateTime timestampUtc,
        ReadingQuality quality = ReadingQuality.Good,
        bool isAnomaly = false) where T : struct
    {
        return new TelemetryPacket<T>(
            sensorProfileId, readingType, value, unit, timestampUtc, quality, isAnomaly);
    }
}
