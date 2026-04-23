# HomeAlarm

Alarmanlage für das Eigenheim, lauffähig auf einem Raspberry Pi 4/5. Überwacht
Erdgeschoss (2 PIR), Obergeschoss (1 PIR) und die Umgebung (2 Radar). Auslösung
aktiviert 2 Sirenen und 2 Alarmgeber. Bedienung per 2 GPIO-Matrix-Tastaturen und/oder
Touchdisplay.

## Architektur

```
┌──────────────────────────────┐        HTTP/SignalR       ┌───────────────────────────┐
│  Electron + React + Radix UI │ ◄───────────────────────► │  ASP.NET Core 8 (C#)      │
│  (Kiosk am Touchdisplay)     │        localhost:5080     │  HomeAlarm.Api            │
└──────────────────────────────┘                           │    ├─ StateMachine         │
                                                           │    ├─ AlarmService (Bg)    │
                                                           │    ├─ Hardware-Layer       │
                                                           │    │   (System.Device.Gpio)│
                                                           │    └─ EF Core + MySQL      │
                                                           └───────────────────────────┘
                                                                      │
                                                                      ▼
                                                     ┌──────────────────────────────┐
                                                     │  Raspberry Pi GPIO            │
                                                     │  PIR / Radar / Relais / Keys  │
                                                     └──────────────────────────────┘
```

Das Backend ist die "C#-DLL-Logik", nur eben als eigenständiger lokaler Service
(HTTP + SignalR) statt als direkt in den Browser geladene DLL. Das ist die saubere
Variante aus der vorherigen Diskussion.

## Projektstruktur

```
home-alarm-system/
├── backend/
│   ├── HomeAlarm.sln
│   ├── HomeAlarm.Core/        Domain, State Machine, Abstractions
│   ├── HomeAlarm.Hardware/    GPIO, Keypad-Matrix, Sensoren, Outputs (+ Mock)
│   ├── HomeAlarm.Data/        EF Core + MySQL, User & Event-Log
│   └── HomeAlarm.Api/         REST, SignalR, AlarmService, appsettings.json
├── frontend/
│   ├── package.json
│   ├── vite.config.ts
│   └── src/
│       ├── main/              Electron Main
│       ├── preload/           Electron Preload
│       └── renderer/          React + Radix UI
├── docs/
│   └── hardware-wiring.md     GPIO-Belegung & Verdrahtung
├── scripts/
│   └── setup-dev.md           Entwicklungs-Setup
├── docker-compose.yml         MySQL für Entwicklung
└── README.md
```

## Entwicklung (Windows/Mac, ohne Pi)

1. Docker starten und MySQL hochfahren:
   ```bash
   docker compose up -d
   ```
2. Backend (läuft im Mock-GPIO-Modus aus `appsettings.json`):
   ```bash
   cd backend
   dotnet restore
   dotnet run --project HomeAlarm.Api
   ```
   Der Service hört auf http://127.0.0.1:5080. Swagger unter `/swagger`.
   Ein Default-Admin `admin` mit PIN `1234` wird beim ersten Start angelegt –
   **danach bitte ändern** (siehe `POST /api/users`).

3. Frontend:
   ```bash
   cd frontend
   npm install
   npm run electron:dev
   ```

## Deployment auf den Raspberry Pi

1. MySQL auf dem Pi installieren (`sudo apt install mariadb-server`) und
   Datenbank/User wie in `docker-compose.yml` anlegen, oder Connection String in
   `appsettings.json` anpassen.

2. Backend publishen und kopieren:
   ```bash
   cd backend
   dotnet publish HomeAlarm.Api -c Release -r linux-arm64 --self-contained false -o publish
   scp -r publish pi@alarmpi:/opt/homealarm/
   ```
   In `appsettings.json` auf dem Pi setzen:
   ```json
   "Alarm": { "GpioMode": "real", ... }
   ```
   Dann via `dotnet /opt/homealarm/HomeAlarm.Api.dll` starten, später via systemd.

3. Frontend:
   ```bash
   cd frontend
   npm run build:pi
   ```
   Erzeugt ein AppImage/.deb für `linux-arm64`.

4. GPIO-Zugriff: Der Service muss in der Gruppe `gpio` sein
   (`sudo usermod -aG gpio alarm`). Relais niemals direkt an GPIO — immer
   über ein Relaismodul mit Optokoppler.

## HTTP-API (Kurzreferenz)

| Methode | Pfad                | Zweck                                |
|---------|---------------------|--------------------------------------|
| GET     | /api/alarm/state    | aktueller Zustand                    |
| POST    | /api/alarm/arm      | scharfschalten (Body: {"pin":"..."}) |
| POST    | /api/alarm/disarm   | unscharfschalten                     |
| GET     | /api/alarm/events   | letzte Events (Query: take)          |
| GET     | /api/users          | Benutzer auflisten                   |
| POST    | /api/users          | Benutzer anlegen                     |
| DELETE  | /api/users/{id}     | Benutzer deaktivieren                |
| HUB     | /hubs/alarm         | SignalR, pusht `eventOccurred`       |

## Sicherheits­hinweise

- Default-PIN `1234` ist nur zum Erstzugriff. Sofort ersetzen.
- Service nur an `127.0.0.1` binden (ist voreingestellt), damit ohne Reverse-Proxy
  niemand aus dem LAN rankommt.
- Für Remote-Zugriff: WireGuard + TLS-Termination (Caddy/nginx) davor.
- BCrypt-Hash für PINs ist bewusst langsam; ersetze das nicht durch MD5/SHA.
- Stromausfall: Pi an USV und Alarmlogik bei Reboot auf "Disarmed" – das ist die
  aktuelle Default-Logik. Wenn du "State persistent" möchtest, speichere den
  letzten State in der DB und lese ihn beim Startup.
