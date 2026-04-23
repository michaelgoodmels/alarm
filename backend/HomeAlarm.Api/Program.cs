using HomeAlarm.Api.Configuration;
using HomeAlarm.Api.Hubs;
using HomeAlarm.Api.Services;
using HomeAlarm.Core.Abstractions;
using HomeAlarm.Core.StateMachine;
using HomeAlarm.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// --- Config ---
builder.Services.Configure<AlarmConfig>(builder.Configuration.GetSection("Alarm"));

// --- Data ---
var connString = builder.Configuration.GetConnectionString("Mysql")
    ?? throw new InvalidOperationException("ConnectionStrings:Mysql fehlt in appsettings.json");

builder.Services.AddDbContextFactory<AlarmDbContext>(opt =>
    opt.UseMySql(connString, ServerVersion.AutoDetect(connString)));
builder.Services.AddScoped(sp =>
    sp.GetRequiredService<IDbContextFactory<AlarmDbContext>>().CreateDbContext());
builder.Services.AddScoped<UserService>();
builder.Services.AddSingleton<EventLogService>();

// --- Core ---
builder.Services.AddSingleton<IEventBus, EventBus>();
builder.Services.AddSingleton<AlarmStateMachine>();

// --- Hardware ---
builder.Services.AddHomeAlarmHardware();

// --- AlarmService als Hintergrunddienst UND singleton fuer Controller-Zugriff ---
builder.Services.AddSingleton<AlarmService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<AlarmService>());

// --- Web-Schicht ---
builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS: Electron-Renderer laedt per file:// und schickt Origin "file://" bzw. "null".
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.SetIsOriginAllowed(_ => true).AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

var app = builder.Build();

// --- Migrationen + Default-Admin sicherstellen ---
using (var scope = app.Services.CreateScope())
{
    var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AlarmDbContext>>();
    await using var db = await factory.CreateDbContextAsync();
    await db.Database.EnsureCreatedAsync();

    if (!db.Users.Any())
    {
        db.Users.Add(new HomeAlarm.Data.Entities.User
        {
            UserName = "admin",
            PinHash = BCrypt.Net.BCrypt.HashPassword("1234", 11),
            IsAdmin = true,
            IsActive = true
        });
        await db.SaveChangesAsync();
        app.Logger.LogWarning("Default-Admin 'admin' mit PIN '1234' angelegt. BITTE AENDERN.");
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.MapControllers();
app.MapHub<AlarmHub>("/hubs/alarm");
app.MapGet("/", () => Results.Ok(new { service = "HomeAlarm.Api", status = "ok" }));

app.Run();
