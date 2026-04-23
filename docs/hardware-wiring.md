# Hardware-Verdrahtung

Alle Pin-Nummern sind BCM-Nummern (nicht Header-Pins). Die Zuordnung ist in
`backend/HomeAlarm.Api/appsettings.json` konfigurierbar.

## Default-GPIO-Belegung

### Sensoren (Inputs, active high, +3.3 V Signal-Pegel)

| Id         | Beschreibung      | Zone         | Typ  | GPIO |
|------------|-------------------|--------------|------|------|
| pir-eg-1   | EG Wohnzimmer     | GroundFloor  | PIR  | 17   |
| pir-eg-2   | EG Flur           | GroundFloor  | PIR  | 27   |
| pir-og-1   | OG Flur           | UpperFloor   | PIR  | 22   |
| radar-p-1  | Garten Nord       | Perimeter    | Radar| 23   |
| radar-p-2  | Garten Süd        | Perimeter    | Radar| 24   |

Wichtige Regeln:
- PIR- und Radar-Module müssen 3,3-V-logisch sein (oder per Level-Shifter
  angepasst). 5-V-Signale zerstören den Pi.
- GND aller Module mit dem Pi-GND verbinden.
- Kabelführung: verdrillte Aderpaare, Masse möglichst nah am Signal.

### Outputs (Relais, active high = Relais zieht an)

| Id       | Beschreibung    | Typ              | GPIO |
|----------|-----------------|------------------|------|
| siren-1  | Sirene Dach     | Siren            | 5    |
| siren-2  | Sirene Keller   | Siren            | 6    |
| alarm-1  | Alarmgeber A    | AlarmTransmitter | 13   |
| alarm-2  | Alarmgeber B    | AlarmTransmitter | 19   |

Zwingend: Relaismodul mit Optokoppler verwenden, eigene 5-V-Versorgung für
die Relais-Spulen, nicht die 5-V-Rail des Pi belasten. Wenn dein Modul
"low-aktiv" ist, setze `ActiveHigh: false` im betreffenden Output-Eintrag.

### Keypads (4x3 Matrix)

**Eingang EG (`kp-eingang`)**
- Rows (Outputs):  GPIO 26, 16, 20, 21
- Cols (Inputs):   GPIO 12, 25, 7

**OG Treppe (`kp-og`)**
- Rows (Outputs):  GPIO  4, 18, 14, 15
- Cols (Inputs):   GPIO  8, 11, 9

Verdrahtung:
```
        Col1   Col2   Col3
Row1 ── 1 ──── 2 ──── 3
Row2 ── 4 ──── 5 ──── 6
Row3 ── 7 ──── 8 ──── 9
Row4 ── * ──── 0 ──── #
```
Reihen sind getriebene Outputs, Spalten liegen per Software auf Pull-Up.
Taste drücken verbindet eine gerade auf LOW gezogene Row mit der Col.

Tipp: 10 kΩ-Serienwiderstände in jede Col-Leitung schützen den Pi für den
Fall, dass du versehentlich zwei Reihen gleichzeitig auf LOW ziehst.

## Zusammenstellungsliste

| Teil                         | Anzahl | Hinweis                                   |
|------------------------------|--------|-------------------------------------------|
| Raspberry Pi 5 (4 GB+)       | 1      | Pi 4 funktioniert auch                    |
| 7" offizielles Touchdisplay  | 1      | DSI, löst 1024×600                        |
| USV-HAT (z.B. UPS PLUS)      | 1      | gegen Stromausfall                        |
| 8-Kanal-Relaismodul          | 1      | optoisoliert, 5 V, active-high            |
| PIR-Melder (3,3 V TTL-out)   | 3      | z.B. HC-SR501 *mit* Level-Shifter         |
| Radar-Modul (RCWL-0516)      | 2      | 3,3-V-Logik-kompatibel                    |
| 4×3 Matrix-Tastatur          | 2      | vergossene Außen-Variante                 |
| Sirene 12 V                  | 2      | über Relais schalten                      |
| Externer Alarmgeber          | 2      | GSM-Dialer / Blitzleuchte / stiller Alarm |
| Ferrit-Core auf Langleitung  | n      | EMV-Maßnahme                              |

## Stromversorgung

Der Pi hängt an der USV. Die Relais-Last (Sirenen/Alarmgeber) sollte über ein
eigenes 12/24-V-Netzteil mit Akkupufferung laufen, nicht über den Pi. Nur die
Steuerseite der Relais wird vom Pi versorgt (via Optokoppler).
