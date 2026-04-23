# Entwicklungs-Setup

## Voraussetzungen

- .NET SDK 8.0
- Node.js 20+
- Docker Desktop
- (nur Pi) Raspberry Pi OS 64-bit

## Erste Schritte

```bash
# 1. MySQL starten (Container fuer die Entwicklung)
docker compose up -d

# 2. Backend
cd backend
dotnet restore
dotnet run --project HomeAlarm.Api

# 3. Frontend in neuem Terminal
cd frontend
npm install
npm run electron:dev
```

Der Backend-Service antwortet auf http://127.0.0.1:5080. Swagger-UI ist unter
http://127.0.0.1:5080/swagger erreichbar. Der Mock-GPIO-Modus wird per Default
aktiviert – Sensor-Events lassen sich damit vom Test-Code aus simulieren, indem
man ein Test-Projekt hinzufügt und `MockGpioController.Simulate(pin, true)` aufruft.

## Ersten Admin-PIN setzen

```bash
curl -X POST http://127.0.0.1:5080/api/users \
  -H "Content-Type: application/json" \
  -d '{"userName":"michael","pin":"482913","isAdmin":true}'

# danach Default-Admin deaktivieren
curl -X DELETE http://127.0.0.1:5080/api/users/1
```
