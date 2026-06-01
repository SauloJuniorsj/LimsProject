import { describe, expect, it, beforeEach } from "vitest";
import { authStore } from "../auth";
import type { AuthTokens } from "@/types/api";

const sampleTokens: AuthTokens = {
  accessToken: "access-xyz",
  refreshToken: "refresh-abc",
  accessTokenExpiresAt: new Date(Date.now() + 3600_000).toISOString(),
  refreshTokenExpiresAt: new Date(Date.now() + 30 * 24 * 3600_000).toISOString(),
};

describe("authStore", () => {
  beforeEach(() => {
    authStore.clear();
    localStorage.clear();
  });

  it("começa desautenticado", () => {
    expect(authStore.isAuthenticated()).toBe(false);
    expect(authStore.getState().accessToken).toBeNull();
  });

  it("setTokens autentica e NUNCA persiste tokens no localStorage (refresh é cookie HttpOnly)", () => {
    authStore.setTokens(sampleTokens, "test@user.com");

    expect(authStore.isAuthenticated()).toBe(true);
    expect(authStore.getState().accessToken).toBe("access-xyz");
    expect(authStore.getState().email).toBe("test@user.com");
    // Invariante crítica de segurança: NENHUM token toca o localStorage.
    // Access fica em memória, refresh fica em cookie HttpOnly (XSS-proof).
    expect(localStorage.getItem("lims.refreshToken")).toBeNull();
    expect(localStorage.getItem("lims.accessToken")).toBeNull();
  });

  it("clear remove tudo da memória", () => {
    authStore.setTokens(sampleTokens, "test@user.com");
    authStore.clear();
    expect(authStore.isAuthenticated()).toBe(false);
    expect(authStore.getState().accessToken).toBeNull();
    expect(authStore.getState().email).toBeNull();
  });

  it("notifica subscribers em setTokens e clear", () => {
    let calls = 0;
    const unsub = authStore.subscribe(() => calls++);

    authStore.setTokens(sampleTokens);
    authStore.clear();

    expect(calls).toBe(2);
    unsub();
  });

  it("unsubscribe para de notificar", () => {
    let calls = 0;
    const unsub = authStore.subscribe(() => calls++);
    unsub();
    authStore.setTokens(sampleTokens);
    expect(calls).toBe(0);
  });
});
