import type { AuthTokens } from "@/types/api";

// Auth store — access token vive APENAS em memória (sai quando fecha a aba ou recarrega).
// Refresh token é HttpOnly cookie gerenciado pelo backend — JavaScript NÃO acessa.
// Boot: AuthBootstrap chama /auth/refresh; o navegador anexa o cookie automaticamente.

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
    this.notify();
  }

  clear() {
    this.state = { accessToken: null, accessTokenExpiresAt: null, email: null };
    this.notify();
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
