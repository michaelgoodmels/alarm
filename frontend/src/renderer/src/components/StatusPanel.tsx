import { Badge, Card, Flex, Heading, Text } from "@radix-ui/themes";
import type { AlarmState } from "../hooks/useAlarmHub";

const COLOR: Record<AlarmState, "green" | "amber" | "red"> = {
  Disarmed: "green",
  Armed: "amber",
  Alarm: "red"
};

const LABEL: Record<AlarmState, string> = {
  Disarmed: "UNSCHARF",
  Armed: "SCHARF",
  Alarm: "ALARM"
};

export function StatusPanel({ state, connected }: { state: AlarmState; connected: boolean }) {
  return (
    <Card size="3">
      <Flex direction="column" gap="2">
        <Flex align="center" justify="between">
          <Heading size="3">Status</Heading>
          <Badge color={connected ? "green" : "gray"} variant="soft">
            {connected ? "verbunden" : "offline"}
          </Badge>
        </Flex>
        <Flex align="center" justify="center" py="5">
          <Badge size="3" color={COLOR[state]} variant="solid" radius="full">
            <Text size="6" weight="bold">{LABEL[state]}</Text>
          </Badge>
        </Flex>
      </Flex>
    </Card>
  );
}
