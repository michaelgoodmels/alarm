using HomeAlarm.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace HomeAlarm.Data;

public sealed class UserService
{
    private readonly AlarmDbContext _db;

    public UserService(AlarmDbContext db) { _db = db; }

    /// <summary>
    /// Prueft einen PIN und gibt bei Erfolg den User zurueck.
    /// Timing-Attacken werden durch BCrypt.Verify abgefangen.
    /// </summary>
    public async Task<User?> AuthenticateAsync(string pin, CancellationToken ct = default)
    {
        // PIN ist system-weit eindeutig (jeder Benutzer hat einen eigenen).
        // Wir laden alle aktiven Hashes und pruefen gegen jeden.
        // Bei <100 Nutzern im Eigenheim voellig ok.
        var users = await _db.Users.Where(u => u.IsActive).ToListAsync(ct);
        foreach (var u in users)
        {
            try
            {
                if (BCrypt.Net.BCrypt.Verify(pin, u.PinHash)) return u;
            }
            catch { /* ungueltiger Hash in DB ignorieren */ }
        }
        return null;
    }

    public async Task<User> CreateAsync(string userName, string pin, bool isAdmin, CancellationToken ct = default)
    {
        var hash = BCrypt.Net.BCrypt.HashPassword(pin, workFactor: 11);
        var user = new User { UserName = userName, PinHash = hash, IsAdmin = isAdmin };
        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);
        return user;
    }

    public Task<List<User>> ListAsync(CancellationToken ct = default) =>
        _db.Users.AsNoTracking().OrderBy(u => u.UserName).ToListAsync(ct);

    public async Task<bool> DeactivateAsync(int id, CancellationToken ct = default)
    {
        var u = await _db.Users.FindAsync(new object[] { id }, ct);
        if (u is null) return false;
        u.IsActive = false;
        await _db.SaveChangesAsync(ct);
        return true;
    }
}
