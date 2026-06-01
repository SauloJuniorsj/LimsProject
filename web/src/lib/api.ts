import { authStore } from "./auth";
import type { AuthTokens } from "@/types/api";

export class ApiError extends Error {
  status: number;
  detail?: string;
  problemDetails?: Record<string, unknown>;

  constructor(
    message: string,
    status: number,
    detail?: string,
    problemDetails?: Record<string, unknown>,
  ) {
    super(message);
    this.status = status;
    this.detail = detail;
    this.problemDetails = problemDetails;
  }
}

// Várias requests com 401 simultâneas compartilham UMA chamada de refresh,
// evitando race condition que invalidaria os tokens novos
let refreshInFlight: Promise<AuthTokens | null> | null = null;

async function attemptRefresh(): Promise<AuthTokens | null> {
  if (refreshInFlight) return refreshInFlight;

  refreshInFlight = (async () => {
    try {
      // credentials: "include" garante envio do cookie HttpOnly mesmo se a base url
      // mudar pra cross-origin no futuro. Backend lê o refresh do cookie.
      const resp = await fetch("/auth/refresh", {
        method: "POST",
        credentials: "include",
      });
      if (!resp.ok) {
        authStore.clear();
        return null;
      }
      const tokens = (await resp.json()) as AuthTokens;
      authStore.setTokens(tokens);
      return tokens;
    } catch {
      authStore.clear();
      return null;
    } finally {
      refreshInFlight = null;
    }
  })();

  return refreshInFlight;
}

interface ApiRequestOptions extends Omit<RequestInit, "body"> {
  body?: unknown;
  retryOn401?: boolean;
}

export async function api<T = unknown>(
  path: string,
  options: ApiRequestOptions = {},
): Promise<T> {
  const { body, retryOn401 = true, headers, ...rest } = options;

  const accessToken = authStore.getState().accessToken;
  const finalHeaders: Record<string, string> = {
    Accept: "application/json",
    ...(body !== undefined ? { "Content-Type": "application/json" } : {}),
    ...(accessToken ? { Authorization: `Bearer ${accessToken}` } : {}),
    ...((headers as Record<string, string>) ?? {}),
  };

  const resp = await fetch(path, {
    ...rest,
    headers: finalHeaders,
    credentials: "include", // sempre envia cookies da mesma origem
    body: body !== undefined ? JSON.stringify(body) : undefined,
  });

  // 401 → tenta refresh + retry uma vez
  if (resp.status === 401 && retryOn401 && accessToken) {
    const refreshed = await attemptRefresh();
    if (refreshed) {
      return api<T>(path, { ...options, retryOn401: false });
    }
  }

  if (!resp.ok) {
    const text = await resp.text();
    let problemDetails: Record<string, unknown> | undefined;
    let detail: string | undefined;
    try {
      problemDetails = JSON.parse(text);
      detail = (problemDetails?.detail as string) ?? (problemDetails?.title as string);
    } catch {
      detail = text;
    }
    throw new ApiError(detail ?? `HTTP ${resp.status}`, resp.status, detail, problemDetails);
  }

  if (resp.status === 204) return undefined as T;
  const contentType = resp.headers.get("content-type") ?? "";
  if (contentType.includes("application/json")) {
    return (await resp.json()) as T;
  }
  return undefined as T;
}
