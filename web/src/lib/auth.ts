import type { AuthTokens } from "@/types/api";

// Store de auth — access token vive APENAS em memória (sai quando fecha a aba).
// Refresh token em localStorage por simplicidade (TODO migrar pra cookie HttpOnly).
// Listeners notificam React sobre mudanças (login/logout) sem precisar de Context global.

const REFRESH_KEY = "lims.refreshToken";

interface AuthState {
  accessToken: string | null;
  accessTokenExpiresAt: Date | null;
  email: string | null;
}

class AuthStore {
  private state: AuthState = {
    accessToken: null,
    accessTokenExpiresAt: null,
    email: null,
  };
  private listeners = new Set<() => void>();

  getState(): AuthState {
    return this.state;
  }

  subscribe(fn: () => void): () => void {
    this.listeners.add(fn);
    return () => this.listeners.delete(fn);
  }

  private notify() {
    this.listeners.forEach((fn) => fn());
  }

  setTokens(tokens: AuthTokens, email?: string) {
    this.state = {
      accessToken: tokens.accessToken,
      accessTokenExpiresAt: new Date(tokens.accessTokenExpiresAt),
      email: email ?? this.state.email ?? this.parseEmailFromJwt(tokens.accessToken),
    };
    localStorage.setItem(REFRESH_KEY, tokens.refreshToken);
    this.notify();
  }

  clear() {
    this.state = { accessToken: null, accessTokenExpiresAt: null, email: null };
    localStorage.removeItem(REFRESH_KEY);
    this.notify();
  }

  getRefreshToken(): string | null {
    return localStorage.getItem(REFRESH_KEY);
  }

  isAuthenticated(): boolean {
    return !!this.state.accessToken;
  }

  private parseEmailFromJwt(token: string): string | null {
    try {
      const [, payload] = token.split(".");
      const decoded = JSON.parse(atob(payload.replace(/-/g, "+").replace(/_/g, "/")));
      return (
        decoded["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress"] ??
        decoded.email ??
        null
      );
    } catch {
      return null;
    }
  }
}

export const authStore = new AuthStore();
