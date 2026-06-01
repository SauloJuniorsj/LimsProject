import { useEffect, useState } from "react";
import { Sprout } from "lucide-react";
import { authStore } from "@/lib/auth";
import type { AuthTokens } from "@/types/api";

/**
 * Reidrata a sessão na primeira carga: tenta /auth/refresh — o navegador anexa
 * o cookie HttpOnly automaticamente se houver sessão ativa. 200 = logado, 401 = anônimo.
 * Pequeno splash enquanto faz o round-trip.
 */
export function AuthBootstrap({ children }: { children: React.ReactNode }) {
  const [bootstrapping, setBootstrapping] = useState(true);

  useEffect(() => {
    (async () => {
      try {
        const resp = await fetch("/auth/refresh", {
          method: "POST",
          credentials: "include",
        });
        if (resp.ok) {
          const tokens = (await resp.json()) as AuthTokens;
          authStore.setTokens(tokens);
        }
      } catch {
        // sem sessão / offline — segue como anônimo, AuthGuard redireciona pra /login
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
