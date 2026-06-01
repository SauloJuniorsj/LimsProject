import { useSyncExternalStore } from "react";
import { authStore } from "@/lib/auth";
import { api } from "@/lib/api";
import type { AuthTokens } from "@/types/api";

export function useAuth() {
  const state = useSyncExternalStore(
    (fn) => authStore.subscribe(fn),
    () => authStore.getState(),
  );

  return {
    ...state,
    isAuthenticated: !!state.accessToken,
    async login(email: string, password: string) {
      // Backend seta cookie HttpOnly com refresh; só o access vai pro response body
      const tokens = await api<AuthTokens>("/auth/login", {
        method: "POST",
        body: { email, password },
      });
      authStore.setTokens(tokens, email);
    },
    async logout() {
      try {
        // Backend lê o refresh do cookie e o revoga + limpa o cookie
        await api("/auth/logout", { method: "POST", retryOn401: false });
      } catch {
        // logout é idempotente — se falhar, só limpa local mesmo
      }
      authStore.clear();
    },
  };
}
