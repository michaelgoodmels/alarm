using Microsoft.AspNetCore.SignalR;

namespace HomeAlarm.Api.Hubs;

/// <summary>
/// SignalR-Hub, ueber den das Frontend live Events empfaengt.
/// Der Server pushed "eventOccurred" und "stateChanged" Nachrichten.
/// </summary>
public sealed class AlarmHub : Hub
{
}
