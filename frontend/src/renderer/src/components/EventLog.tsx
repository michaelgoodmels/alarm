import { Badge, Card, Flex, Heading, ScrollArea, Text } from "@radix-ui/themes";
import type { AlarmEvent } from "../hooks/useAlarmHub";

const TYPE_COLOR: Record<string, "blue" | "red" | "amber" | "green" | "gray"> = {
  StateChanged:     "blue",
  SensorTriggered:  "amber",
  KeypadInput:      "gray",
  AuthSuccess:      "green",
  AuthFailure:      "red",
  OutputChanged:    "red",
  SystemInfo:       "blue",
  SystemError:      "red"
};

export function EventLog({ events }: { events: AlarmEvent[] }) {
  return (
    <Card size="3">
      <Flex direction="column" gap="2" style={{ height: "100%" }}>
        <Heading size="3">Ereignisse</Heading>
        <ScrollArea type="hover" scrollbars="vertical" style={{ height: 420 }}>
          <Flex direction="column" gap="2" pr="3">
            {events.length === 0 && <Text color="gray">Noch keine Ereignisse.</Text>}
            {events.map((e, i) => (
              <Flex key={i} gap="2" align="center">
                <Text size="1" color="gray" style={{ width: 72, fontVariantNumeric: "tabular-nums" }}>
                  {new Date(e.timestamp).toLocaleTimeString()}
                </Text>
                <Badge color={TYPE_COLOR[e.type] ?? "gray"} variant="soft">{e.type}</Badge>
                <Text size="2">{e.message}</Text>
              </Flex>
            ))}
          </Flex>
        </ScrollArea>
      </Flex>
    </Card>
  );
}
