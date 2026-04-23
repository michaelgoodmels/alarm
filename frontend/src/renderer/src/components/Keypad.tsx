import { useState } from "react";
import { Button, Card, Flex, Grid, Heading, Text } from "@radix-ui/themes";
import { apiPost } from "../lib/api";

/**
 * Numerische Bildschirm-Tastatur fuer PIN-Eingabe ueber Touchdisplay.
 * Nutzt Radix UI Buttons. Die hardware-seitigen Keypads sind davon unabhaengig.
 */
export function Keypad({ state, onResult }: {
  state: "Disarmed" | "Armed" | "Alarm";
  onResult: (ok: boolean, msg: string) => void;
}) {
  const [pin, setPin] = useState("");
  const [busy, setBusy] = useState(false);

  const keys = [["1","2","3"],["4","5","6"],["7","8","9"],["C","0","⌫"]];

  const append = (k: string) => {
    if (k === "C") { setPin(""); return; }
    if (k === "⌫") { setPin(p => p.slice(0, -1)); return; }
    setPin(p => (p.length < 8 ? p + k : p));
  };

  async function submit(action: "arm" | "disarm") {
    if (!pin) return;
    setBusy(true);
    try {
      await apiPost(`/api/alarm/${action}`, { pin });
      onResult(true, action === "arm" ? "Scharf geschaltet" : "Unscharf geschaltet");
    } catch (e: any) {
      onResult(false, "Ungueltiger PIN");
    } finally {
      setPin("");
      setBusy(false);
    }
  }

  return (
    <Card size="3">
      <Flex direction="column" gap="3">
        <Heading size="3">PIN-Eingabe</Heading>

        <Flex align="center" justify="center" style={{
          height: 48, borderRadius: 8, background: "#1a1d23",
          fontSize: 28, letterSpacing: 8, fontVariantNumeric: "tabular-nums"
        }}>
          <Text>{pin ? "•".repeat(pin.length) : <Text color="gray">—</Text>}</Text>
        </Flex>

        <Grid columns="3" gap="2">
          {keys.flat().map(k => (
            <Button key={k} size="4" variant="soft" onClick={() => append(k)} disabled={busy}>
              {k}
            </Button>
          ))}
        </Grid>

        <Flex gap="2">
          <Button
            size="3" color="amber" style={{ flex: 1 }}
            disabled={busy || state !== "Disarmed"}
            onClick={() => submit("arm")}>
            Scharf
          </Button>
          <Button
            size="3" color="green" style={{ flex: 1 }}
            disabled={busy || state === "Disarmed"}
            onClick={() => submit("disarm")}>
            Unscharf
          </Button>
        </Flex>
      </Flex>
    </Card>
  );
}
