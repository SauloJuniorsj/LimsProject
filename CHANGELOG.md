# Changelog

Todas as mudanças notáveis deste projeto são documentadas aqui.
Formato baseado em [Keep a Changelog](https://keepachangelog.com/pt-BR/1.0.0/).

## [Unreleased]

### Added
- Simulador de sensor sob demanda (`POST /batches/{id}/sensor-data/simulate`): dispara um burst de 12 leituras sintéticas (random walk com spikes propositais fora da faixa 18-30°C) em background, com gráfico e tabela atualizando ao vivo no frontend enquanto a simulação roda — pensado pra provar a telemetria + `RollupWorker` numa demo sem precisar de hardware real.
- Login com contas de demonstração (Admin / Lab) — um clique, sem cadastro, pra facilitar avaliação do projeto.

### Fixed
- Removida seção de variáveis de ambiente de ferramentas de IA (`.env.example`) e diretórios de scaffolding do framework AIOX que não fazem parte da aplicação — incluindo um arquivo de memória de agente que continha dados pessoais e nunca deveria ter sido versionado.

## [1.0.0] — 2026-06-17

Primeira versão "completa" do LIMS: API .NET com Clean/Onion Architecture, frontend React, autenticação JWT de produção, mensageria transacional, observabilidade e deploy free-tier funcional.

### Added
- **Domínio LIMS**: CRUD de lotes (batches), leituras de sensor, análises laboratoriais, Certificado de Análise (CoA) — a feature "matadora" do domínio.
- **Autenticação e autorização**: JWT + ASP.NET Identity, roles `Admin`/`Lab`, refresh token rotation com detecção de reuso (revogação de toda a cadeia), cookie HttpOnly para o refresh, RFC 7807 problem details, correlation IDs.
- **Mensageria**: eventos de domínio publicados no RabbitMQ; depois substituído por um **transactional Outbox pattern** para eliminar a janela fire-and-forget.
- **Consolidação de dados**: `RollupWorker` gera resumos diários (min/média/máx) de telemetria por lote.
- **Auditoria**: soft delete e campos de auditoria via interceptor do `SaveChangesAsync`; trilha de histórico de status do lote.
- **Observabilidade**: OpenTelemetry (traces + métricas de domínio + métricas de runtime), health checks customizados.
- **API**: versionamento de API, rate limiting, cache em memória, paginação/ordenação/filtro nas listagens.
- **Qualidade**: suíte de testes xUnit (unit + integration + fitness de arquitetura), gate de cobertura no CI, migração de Minimal APIs para Controllers + DTOs por testabilidade.
- **Frontend**: SPA em React 19 + Vite + TanStack Router/Query, dashboard com gráficos (Recharts), formulário de compliance, tabela de leituras de sensor, gestão de usuários, sessão persistente, layout responsivo, folha de estilo de impressão para o CoA, 14 testes com Vitest.
- **Deploy**: containerização via Docker, credenciais fora do compose (env-driven), setup de deploy free-tier — Render (API) + Neon (Postgres) + Vercel (frontend).

## [0.1.0] — 2026-03-17 a 2026-05-29

Fundação do backend.

### Added
- Modelo de dados inicial (EF Core) e validação de lotes.
- Entidade de resumo diário e lógica de rollup.
- Mudança de arquitetura para Clean/Onion (Domain/Application/Infrastructure/API).
- CI/CD inicial com cobertura de testes, migrações de schema, índices e foreign keys.
- API de leituras de sensor, resumos diários, rate limiting.
