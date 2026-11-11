using System;

namespace BudsConnect.Core;

public sealed record BudsState(bool Connected = false, string Name = "Galaxy Buds3",
    int? Left = null, int? Right = null, int? Case = null, bool? Anc = null,
    DateTimeOffset? Updated = null, string Status = "Hledám spárovaná Galaxy Buds3…")
{
    public int? TrayBattery => !Connected ? null : Left.HasValue && Right.HasValue
        ? Math.Min(Left.Value, Right.Value) : Left ?? Right;
    public static string Percent(int? value) => value.HasValue ? $"{value} %" : "—";
}
