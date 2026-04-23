using HomeAlarm.Data;
using Microsoft.AspNetCore.Mvc;

namespace HomeAlarm.Api.Controllers;

/// <summary>
/// Benutzerverwaltung. Nur fuer Admins gedacht.
/// Auth ist hier bewusst schlank – UI darf diesen Controller nur ausgeliefert bekommen,
/// wenn der aktuelle User Admin ist; eine Lean Auth via PIN-Header waere die naechste Stufe.
/// </summary>
[ApiController]
[Route("api/users")]
public sealed class UsersController : ControllerBase
{
    private readonly UserService _users;

    public UsersController(UserService users) { _users = users; }

    public sealed record CreateUserRequest(string UserName, string Pin, bool IsAdmin);

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var users = await _users.ListAsync(ct);
        return Ok(users.Select(u => new { u.Id, u.UserName, u.IsAdmin, u.IsActive, u.CreatedAt }));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.UserName) || string.IsNullOrWhiteSpace(req.Pin))
            return BadRequest("UserName und Pin erforderlich");
        if (req.Pin.Length < 4) return BadRequest("PIN zu kurz (min. 4 Ziffern)");

        var user = await _users.CreateAsync(req.UserName, req.Pin, req.IsAdmin, ct);
        return Ok(new { user.Id, user.UserName, user.IsAdmin });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Deactivate(int id, CancellationToken ct)
    {
        var ok = await _users.DeactivateAsync(id, ct);
        return ok ? NoContent() : NotFound();
    }
}
