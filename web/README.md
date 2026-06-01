# LIMS Web

Frontend React + Vite + TypeScript do LIMS cannabis backend (`../`).

## Stack

| Camada | Tecnologia |
|---|---|
| Build | Vite 8 |
| Framework | React 19 + TypeScript |
| Styling | Tailwind CSS v4 (sem config — só `@import`) + variáveis OKLCH dark/light |
| Componentes | shadcn-style escritos diretamente (Button, Input, Card, Label, Badge) — sem CLI |
| Roteamento | TanStack Router (file-based, type-safe, code-splitting auto) |
| Server state | TanStack Query (cache, retry, devtools) |
| Forms | React Hook Form + Zod resolver |
| Ícones | Lucide React |
| Gráficos | Recharts (instalado, ainda não usado — pra sensor data) |
| Lint/format | Biome (all-in-one) |

## Como rodar

```bash
# Em um terminal: backend
cd ..
docker-compose up -d        # ou `dotnet run`

# Em outro: frontend
cd web
npm install
npm run dev                 # http://localhost:5173
```

O `vite.config.ts` proxya `/auth`, `/batches`, `/health`, `/swagger`, `/debug` pro backend em `localhost:8080` — **mesma origin pro browser**, sem CORS.

## Auth

- Access token (1h) fica em **memória apenas** — sai quando fecha a aba
- Refresh token (30d) em `localStorage` (TODO migrar pra cookie HttpOnly)
- `src/lib/api.ts` tem interceptor: **401 → `/auth/refresh` automático → re-tenta a request original**
- Refresh deduplicado: várias requests 401 simultâneas compartilham UMA chamada de refresh

## Páginas

| Rota | Descrição |
|---|---|
| `/login` | Login full-page com Zod + React Hook Form |
| `/` | Dashboard: KPIs + distribuição por status |
| `/batches` | Lista paginada com filtros (strain + status) + criar lote |
| `/batches/$id` | Detalhe: status atual, transições permitidas, análises, trilha de auditoria |
| `/batches/$id/coa` | Certificate of Analysis (printable via `window.print()`) |

Rotas protegidas vivem em `/_auth/*` — o layout `_auth.tsx` redireciona pra `/login` se não autenticado.

## Estrutura

```
src/
├── main.tsx              # bootstrap (QueryClient + Router)
├── routes/               # file-based routing (TanStack Router)
│   ├── __root.tsx
│   ├── login.tsx
│   ├── _auth.tsx         # layout protegido com AppShell
│   ├── _auth.index.tsx   # dashboard
│   └── _auth.batches.{...}
├── components/
│   ├── ui/               # primitives shadcn-style
│   ├── layout/           # AppShell (sidebar + main)
│   └── BatchStatusBadge.tsx
├── hooks/                # TanStack Query wrappers + useAuth + useTheme
├── lib/                  # api client, auth store, cn helper
└── types/                # tipos espelhando os DTOs do backend
```

## Scripts

```bash
npm run dev      # dev server com HMR
npm run build    # type-check + bundle produção
npm run preview  # preview do build
npm run lint     # Biome check
npm run format   # Biome format --write
```

## Próximos passos

- Form pra criar análise (`POST /batches/{id}/analysis`) com regra de hemp compliance visível
- Gráfico Recharts de sensor data com daily summaries
- Página admin pra listar/criar usuários e atribuir roles
- Migrar refresh token pro cookie HttpOnly (requer ajuste no backend)
- Vitest + Playwright pra testes
