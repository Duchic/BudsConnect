using System;
using NAudio.CoreAudioApi;

namespace BudsConnect.Services;

public sealed class AudioService
{
    // Never change speakers or another headset: only the default Buds3 render endpoint.
    public int? ReadVolume()
    {
        try
        {
            using var enumerator = new MMDeviceEnumerator();
            using var device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            return BluetoothBudsService.IsBuds3(device.FriendlyName)
                ? (int)Math.Round(device.AudioEndpointVolume.MasterVolumeLevelScalar * 100) : null;
        }
        catch (Exception) { return null; }
    }
    public void SetVolume(int value)
    {
        using var enumerator = new MMDeviceEnumerator();
        using var device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
        if (!BluetoothBudsService.IsBuds3(device.FriendlyName))
            throw new InvalidOperationException("Nejdřív ve Windows zvol Galaxy Buds3 jako zvukový výstup.");
        device.AudioEndpointVolume.MasterVolumeLevelScalar = Math.Clamp(value, 0, 100) / 100f;
    }
}
