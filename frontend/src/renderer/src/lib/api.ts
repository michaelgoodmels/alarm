// Preload exponiert die Base-URLs. Falls in Vite-Dev (ohne Electron) geladen,
// fallen wir zurueck auf Defaults.
const w = window as any;
export const API_BASE: string = w.homeAlarm?.apiBase ?? "http://127.0.0.1:5080";
export const HUB_URL:  string = w.homeAlarm?.hubUrl  ?? "http://127.0.0.1:5080/hubs/alarm";

export async function apiGet<T>(path: string): Promise<T> {
  const r = await fetch(`${API_BASE}${path}`);
  if (!r.ok) throw new Error(`${r.status} ${r.statusText}`);
  return r.json();
}

export async function apiPost<T>(path: string, body: unknown): Promise<T> {
  const r = await fetch(`${API_BASE}${path}`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body)
  });
  if (!r.ok) throw new Error(`${r.status} ${r.statusText}`);
  return r.json();
}
