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
      const tokens = await api<AuthTokens>("/auth/login", {
        method: "POST",
        body: { email, password },
      });
      authStore.setTokens(tokens, email);
    },
    async logout() {
      const refreshToken = authStore.getRefreshToken();
      if (refreshToken) {
        try {
          await api("/auth/logout", {
            method: "POST",
            body: { refreshToken },
            retryOn401: false,
          });
        } catch {
          // logout é idempotente — se falhar, só limpa local mesmo
        }
      }
      authStore.clear();
    },
  };
}
