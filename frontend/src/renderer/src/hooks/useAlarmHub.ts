import { useEffect, useState } from "react";
import * as signalR from "@microsoft/signalr";
import { HUB_URL } from "../lib/api";

export interface AlarmEvent {
  timestamp: string;
  type: string;
  message: string;
  zone?: string | null;
  sourceId?: string | null;
  stateBefore?: string | null;
  stateAfter?: string | null;
  currentState?: string;
}

export type AlarmState = "Disarmed" | "Armed" | "Alarm";

export function useAlarmHub() {
  const [state, setState] = useState<AlarmState>("Disarmed");
  const [events, setEvents] = useState<AlarmEvent[]>([]);
  const [connected, setConnected] = useState(false);

  useEffect(() => {
    const c = new signalR.HubConnectionBuilder()
      .withUrl(HUB_URL)
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    c.on("eventOccurred", (e: AlarmEvent) => {
      if (e.currentState) setState(e.currentState as AlarmState);
      setEvents(prev => [e, ...prev].slice(0, 200));
    });

    c.onreconnected(() => setConnected(true));
    c.onclose(()       => setConnected(false));

    c.start()
      .then(() => setConnected(true))
      .catch(err => console.error("SignalR connect failed", err));

    return () => { c.stop(); };
  }, []);

  return { state, events, connected, setState };
}
