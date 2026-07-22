import { apiFetch } from "@/lib/api/client";

export function login(email: string, password: string) {
  return apiFetch<{ token: string; header: string }>("/api/auth/login", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ email, password }),
  });
}
