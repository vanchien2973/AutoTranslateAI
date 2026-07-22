export const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:8080";

const API_KEY_HEADER = "X-Api-Key";
const API_KEY_STORAGE_KEY = "ata.apiKey";

export class ApiError extends Error {
  constructor(
    readonly status: number,
    message: string,
  ) {
    super(message);
    this.name = "ApiError";
  }

  get isUnauthorized() {
    return this.status === 401 || this.status === 403;
  }
}

export function getApiKey(): string | null {
  if (typeof window !== "undefined") {
    const stored = window.localStorage.getItem(API_KEY_STORAGE_KEY);
    if (stored) return stored;
  }
  return process.env.NEXT_PUBLIC_API_KEY ?? null;
}

export function setApiKey(key: string | null) {
  if (typeof window === "undefined") return;
  if (key) window.localStorage.setItem(API_KEY_STORAGE_KEY, key);
  else window.localStorage.removeItem(API_KEY_STORAGE_KEY);
}

export function hasSession(): boolean {
  return typeof window !== "undefined" && Boolean(window.localStorage.getItem(API_KEY_STORAGE_KEY));
}

export async function apiFetch<T>(path: string, init: RequestInit = {}): Promise<T> {
  const headers = new Headers(init.headers);
  headers.set("Accept", "application/json");

  const key = getApiKey();
  if (key) headers.set(API_KEY_HEADER, key);

  let response: Response;
  try {
    response = await fetch(`${API_BASE_URL}${path}`, { ...init, headers });
  } catch {
    throw new ApiError(0, `Cannot reach the API at ${API_BASE_URL}. Is it running?`);
  }

  if (!response.ok) {
    if ((response.status === 401 || response.status === 403) && hasSession()) {
      setApiKey(null);
      if (typeof window !== "undefined" && window.location.pathname !== "/login") {
        window.location.assign("/login");
      }
    }
    throw new ApiError(response.status, await readError(response));
  }

  return response.status === 204 ? (undefined as T) : ((await response.json()) as T);
}

async function readError(response: Response) {
  if (response.status === 401 || response.status === 403) {
    return "Not signed in — the API rejected the key.";
  }

  const body = (await response.text()).trim();
  if (!body) return `Request failed with status ${response.status}.`;

  return parseProblem(body) ?? body;
}

function parseProblem(body: string): string | null {
  try {
    const parsed: unknown = JSON.parse(body);

    if (Array.isArray(parsed)) return parsed.join(" ");

    if (parsed && typeof parsed === "object") {
      const problem = parsed as { title?: string; errors?: Record<string, string[]> };
      const messages = Object.values(problem.errors ?? {}).flat();
      if (messages.length > 0) return messages.join(" ");
      if (problem.title) return problem.title;
    }
  } catch {
    return null;
  }

  return null;
}
