import { useEffect, useState } from "react";
import { Box, Container, Flex, Grid, Heading, Text, Callout } from "@radix-ui/themes";
import { useAlarmHub } from "./hooks/useAlarmHub";
import { StatusPanel } from "./components/StatusPanel";
import { Keypad } from "./components/Keypad";
import { EventLog } from "./components/EventLog";
import { ZonesOverview } from "./components/ZonesOverview";
import { apiGet } from "./lib/api";

export function App() {
  const { state, events, connected, setState } = useAlarmHub();
  const [toast, setToast] = useState<{ ok: boolean; msg: string } | null>(null);

  // Initial-State nach Reload aus REST holen.
  useEffect(() => {
    apiGet<{ state: string }>("/api/alarm/state")
      .then(r => setState(r.state as any))
      .catch(() => {});
  }, [setState]);

  useEffect(() => {
    if (!toast) return;
    const t = setTimeout(() => setToast(null), 2500);
    return () => clearTimeout(t);
  }, [toast]);

  return (
    <Container size="4" p="4" style={{ height: "100vh", overflow: "hidden" }}>
      <Flex direction="column" gap="3" style={{ height: "100%" }}>
        <Flex align="center" justify="between">
          <Heading size="6">HomeAlarm</Heading>
          <Text color="gray" size="2">Eigenheim-Ueberwachung</Text>
        </Flex>

        {toast && (
          <Callout.Root color={toast.ok ? "green" : "red"}>
            <Callout.Text>{toast.msg}</Callout.Text>
          </Callout.Root>
        )}

        <Grid columns="3" gap="3" style={{ flex: 1, minHeight: 0 }}>
          <Flex direction="column" gap="3">
            <StatusPanel state={state} connected={connected} />
            <ZonesOverview events={events} />
          </Flex>

          <Box>
            <Keypad state={state} onResult={(ok, msg) => setToast({ ok, msg })} />
          </Box>

          <Box>
            <EventLog events={events} />
          </Box>
        </Grid>
      </Flex>
    </Container>
  );
}
