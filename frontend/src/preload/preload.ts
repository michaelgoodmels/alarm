import { contextBridge } from "electron";

// Aktuell brauchen wir keine Native-Bruecke – der Renderer spricht direkt
// per HTTPS/SignalR mit http://127.0.0.1:5080. Platzhalter fuer spaeter.
contextBridge.exposeInMainWorld("homeAlarm", {
  apiBase: "http://127.0.0.1:5080",
  hubUrl:  "http://127.0.0.1:5080/hubs/alarm"
});
