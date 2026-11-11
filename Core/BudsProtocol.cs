using System;
using System.Collections.Generic;

namespace BudsConnect.Core;

public sealed record BudsPacket(byte Id, byte[] Payload, bool Response);

// Samsung SPP, Buds3 generation. See docs/protocol.md for wire format sources.
public sealed class BudsProtocol
{
    private readonly List<byte> buffer = new();
    public static ushort Crc(ReadOnlySpan<byte> bytes)
    {
        ushort crc = 0;
        foreach (byte b in bytes)
        {
            crc ^= (ushort)(b << 8);
            for (int bit = 0; bit < 8; bit++)
                crc = (ushort)((crc & 0x8000) != 0 ? (crc << 1) ^ 0x1021 : crc << 1);
        }
        return crc;
    }

    public static byte[] Encode(byte id, ReadOnlySpan<byte> payload, bool response = false)
    {
        int size = payload.Length + 3;
        if (size > 1023) throw new ArgumentOutOfRangeException(nameof(payload));
        var bytes = new byte[size + 4];
        bytes[0] = 0xFD;
        bytes[1] = (byte)size;
        bytes[2] = (byte)((size >> 8) | (response ? 0x10 : 0));
        bytes[3] = id;
        payload.CopyTo(bytes.AsSpan(4));
        ushort crc = Crc(bytes.AsSpan(3, payload.Length + 1));
        bytes[^3] = (byte)crc;
        bytes[^2] = (byte)(crc >> 8);
        bytes[^1] = 0xDD;
        return bytes;
    }

    public IReadOnlyList<BudsPacket> Feed(ReadOnlySpan<byte> data)
    {
        foreach (byte b in data) buffer.Add(b);
        var packets = new List<BudsPacket>();
        while (buffer.Count >= 3)
        {
            if (buffer[0] != 0xFD) { buffer.RemoveAt(0); continue; }
            int header = buffer[1] | buffer[2] << 8;
            int size = header & 0x3FF;
            if (size < 3 || (header & 0xCC00) != 0) { buffer.RemoveAt(0); continue; }
            if (buffer.Count < size + 4) break;
            byte[] frame = buffer.GetRange(0, size + 4).ToArray();
            ushort expected = (ushort)(frame[^3] | frame[^2] << 8);
            if (frame[^1] != 0xDD || Crc(frame.AsSpan(3, size - 2)) != expected)
            { buffer.RemoveAt(0); continue; }
            // Fragmented transfers are not status/control messages; do not interpret them.
            if ((header & 0x2000) == 0)
                packets.Add(new(frame[3], frame.AsSpan(4, size - 3).ToArray(), (header & 0x1000) != 0));
            buffer.RemoveRange(0, size + 4);
        }
        return packets;
    }

    public static BudsState Apply(BudsState previous, BudsPacket packet, DateTimeOffset now)
    {
        byte[] p = packet.Payload;
        if (packet.Id == 0x60 && p.Length >= 7)
            return previous with { Left = Battery(p[1]), Right = Battery(p[2]), Case = Battery(p[6]), Updated = now };
        if (packet.Id == 0x61 && p.Length >= 8)
            return previous with { Left = Battery(p[2]), Right = Battery(p[3]), Case = Battery(p[7]),
                Anc = p.Length > 12 ? AncMode(p[12]) : null, Updated = now };
        if (packet.Id == 0x77 && p.Length >= 1)
            return previous with { Anc = AncMode(p[0]) };
        return previous;
    }
    private static int? Battery(byte value) => value <= 100 ? value : null;
    private static bool? AncMode(byte value) => value switch { 0 => false, 1 => true, _ => null };
}
