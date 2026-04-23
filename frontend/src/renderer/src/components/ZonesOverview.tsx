import { Badge, Card, Flex, Heading, Text } from "@radix-ui/themes";
import type { AlarmEvent } from "../hooks/useAlarmHub";

/**
 * Zeigt fuer jede Zone den letzten Sensor-Trigger der letzten 60s an.
 */
export function ZonesOverview({ events }: { events: AlarmEvent[] }) {
  const now = Date.now();
  const recent = events.filter(e =>
    e.type === "SensorTriggered" && (now - new Date(e.timestamp).getTime() < 60_000));

  const zones = ["GroundFloor", "UpperFloor", "Perimeter"] as const;
  const label: Record<(typeof zones)[number], string> = {
    GroundFloor: "Erdgeschoss",
    UpperFloor:  "Obergeschoss",
    Perimeter:   "Umgebung"
  };

  return (
    <Card size="3">
      <Flex direction="column" gap="3">
        <Heading size="3">Zonen</Heading>
        <Flex gap="3">
          {zones.map(z => {
            const active = recent.some(e => e.zone === z);
            return (
              <Card key={z} style={{ flex: 1 }}>
                <Flex direction="column" align="center" gap="1" py="2">
                  <Text size="2" color="gray">{label[z]}</Text>
                  <Badge
                    size="2"
                    color={active ? "red" : "green"}
                    variant={active ? "solid" : "soft"}>
                    {active ? "BEWEGUNG" : "ruhig"}
                  </Badge>
                </Flex>
              </Card>
            );
          })}
        </Flex>
      </Flex>
    </Card>
  );
}
