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
  beforeEach(() => authStore.clear());

  it("começa desautenticado", () => {
    expect(authStore.isAuthenticated()).toBe(false);
    expect(authStore.getState().accessToken).toBeNull();
  });

  it("setTokens autentica e persiste só o refresh no localStorage", () => {
    authStore.setTokens(sampleTokens, "test@user.com");

    expect(authStore.isAuthenticated()).toBe(true);
    expect(authStore.getState().accessToken).toBe("access-xyz");
    expect(authStore.getState().email).toBe("test@user.com");
    expect(localStorage.getItem("lims.refreshToken")).toBe("refresh-abc");
    // access token NÃO vai pro localStorage — só refresh
    expect(localStorage.getItem("lims.accessToken")).toBeNull();
  });

  it("clear remove tudo", () => {
    authStore.setTokens(sampleTokens, "test@user.com");
    authStore.clear();

    expect(authStore.isAuthenticated()).toBe(false);
    expect(authStore.getRefreshToken()).toBeNull();
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
