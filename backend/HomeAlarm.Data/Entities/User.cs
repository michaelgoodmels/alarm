using System.ComponentModel.DataAnnotations;

namespace HomeAlarm.Data.Entities;

public sealed class User
{
    public int Id { get; set; }

    [MaxLength(64)]
    public string UserName { get; set; } = "";

    /// <summary>
    /// BCrypt-Hash des PIN-Codes.
    /// </summary>
    [MaxLength(120)]
    public string PinHash { get; set; } = "";

    public bool IsAdmin { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
