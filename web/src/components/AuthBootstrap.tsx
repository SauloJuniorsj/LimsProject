import { useEffect, useState } from "react";
import { Sprout } from "lucide-react";
import { authStore } from "@/lib/auth";
import type { AuthTokens } from "@/types/api";

/**
 * Reidrata a sessão na primeira carga do app: se houver refresh token no localStorage,
 * tenta /auth/refresh ANTES de renderizar as rotas. Sem isso, dar F5 derrubaria o usuário
 * pra /login mesmo com refresh token válido (o access token vive em memória).
 */
export function AuthBootstrap({ children }: { children: React.ReactNode }) {
  const [bootstrapping, setBootstrapping] = useState(() => !!authStore.getRefreshToken());

  useEffect(() => {
    const refreshToken = authStore.getRefreshToken();
    if (!refreshToken) {
      setBootstrapping(false);
      return;
    }

    (async () => {
      try {
        const resp = await fetch("/auth/refresh", {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ refreshToken }),
        });
        if (resp.ok) {
          const tokens = (await resp.json()) as AuthTokens;
          authStore.setTokens(tokens);
        } else {
          authStore.clear();
        }
      } catch {
        authStore.clear();
      } finally {
        setBootstrapping(false);
      }
    })();
  }, []);

  if (bootstrapping) {
    return (
      <div className="flex h-screen w-screen items-center justify-center bg-background">
        <div className="flex items-center gap-3 text-muted-foreground">
          <Sprout className="h-5 w-5 animate-pulse text-emerald-500" />
          <span className="text-sm">Carregando sessão...</span>
        </div>
      </div>
    );
  }

  return <>{children}</>;
}
