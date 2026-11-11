using System;
using System.Threading.Tasks;
using BudsConnect.Core;

namespace BudsConnect.Services;

public interface IBudsService
{
    event Action<BudsState>? Changed;
    Task SetAncAsync(bool enabled);
    void Reconnect();
}
