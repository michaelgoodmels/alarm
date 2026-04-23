using HomeAlarm.Api.Services;
using HomeAlarm.Data;
using Microsoft.AspNetCore.Mvc;

namespace HomeAlarm.Api.Controllers;

[ApiController]
[Route("api/alarm")]
public sealed class AlarmController : ControllerBase
{
    private readonly AlarmService _alarm;
    private readonly EventLogService _log;

    public AlarmController(AlarmService alarm, EventLogService log)
    {
        _alarm = alarm;
        _log = log;
    }

    public sealed record PinRequest(string Pin);

    [HttpGet("state")]
    public IActionResult GetState() => Ok(new { state = _alarm.CurrentState.ToString() });

    [HttpPost("arm")]
    public async Task<IActionResult> Arm([FromBody] PinRequest req)
    {
        var ok = await _alarm.ArmFromUiAsync(req.Pin);
        return ok ? Ok(new { state = _alarm.CurrentState.ToString() }) : Unauthorized();
    }

    [HttpPost("disarm")]
    public async Task<IActionResult> Disarm([FromBody] PinRequest req)
    {
        var ok = await _alarm.DisarmFromUiAsync(req.Pin);
        return ok ? Ok(new { state = _alarm.CurrentState.ToString() }) : Unauthorized();
    }

    [HttpGet("events")]
    public async Task<IActionResult> Events([FromQuery] int take = 100, CancellationToken ct = default)
    {
        var events = await _log.GetRecentAsync(Math.Clamp(take, 1, 1000), ct);
        return Ok(events);
    }
}
