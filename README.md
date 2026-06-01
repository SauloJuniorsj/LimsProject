# 🌿 Seed-to-Sale LIMS

**Laboratory Information Management System** para empresas de cannabis: rastreabilidade do cultivo à liberação do lote, com auditoria de mudanças de status, ingestão de sensores e análises laboratoriais com validação de compliance (cânhamo).

[![CI](https://github.com/saulocintra/LimsProject/actions/workflows/ci.yml/badge.svg)](https://github.com/saulocintra/LimsProject/actions)
![.NET](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet)
![Postgres](https://img.shields.io/badge/PostgreSQL-16-336791?logo=postgresql)
![Tests](https://img.shields.io/badge/tests-111%20passing-brightgreen)

---

## 🎯 O que o sistema resolve

Empresas de cannabis precisam provar — para reguladores e clientes — onde cada grama veio, como foi cultivado e quais análises liberaram o lote para venda. Este LIMS implementa:

- **Ciclo de vida do lote** como máquina de estados: `Germination → Growth → Harvested → Testing → Released | Rejected`. Transições inválidas retornam **422 Unprocessable Entity**.
- **Audit trail completo**: toda mudança de status grava quem, quando e por quê (incluindo a criação inicial e mudanças automáticas via análise laboratorial).
- **Compliance de cânhamo**: lote com THC > 0.3% não pode ser aprovado (regra de validação na camada de aplicação).
- **Rollup em background**: consolida milhões de leituras de sensores em sumários diários (min/max/avg/count), processando apenas lotes ativos.
- **Telemetria de cultivo**: cada leitura de temperatura atualiza `Batch.CurrentTemperature` e fica disponível paginada.
- **Certificate of Analysis (CoA)**: endpoint único que agrega lote + análises + condições ambientais agregadas + ciclo de vida + compliance — o documento padrão da indústria.

---

## 🏗️ Arquitetura

Onion / Clean Architecture com 4 camadas:

```
Domain        ← entidades puras (Batch, LabAnalysis, SensorData, BatchStatusHistory…)
Application   ← interfaces, validators, services, models (DTOs), workers
Infrastructure ← EF Core DbContext, AuthService, Identity
API           ← Minimal API endpoints + composition root (Program.cs)
```

Nenhuma camada interna conhece as externas. `ILimsDbContext` abstrai persistência para os serviços de aplicação.

---

## 🛠️ Tech Stack

| Camada | Tecnologia |
|---|---|
| Runtime | .NET 10, ASP.NET Core Minimal APIs |
| Persistência | PostgreSQL 16 + Entity Framework Core 10 (Npgsql) |
| Autenticação | ASP.NET Core Identity + JWT Bearer (access 1h) + **refresh tokens com rotation + reuse detection (OWASP)** + Roles (`Admin`, `Lab`) |
| Validação | FluentValidation (validators no Application layer) |
| Background | `BackgroundService` + `IServiceScopeFactory` para scopes seguros |
| Rate limiting | `Microsoft.AspNetCore.RateLimiting` (fixed window no `/auth/login`) |
| Observabilidade | Health checks (`/health`) + **OpenTelemetry** (traces ASP.NET Core + métricas custom de domínio + runtime metrics, Console exporter) |
| Messaging | **RabbitMQ** topic exchange (`lims.events`) + **Outbox pattern** com worker de relay, retry e dead-letter implícito |
| Documentação | Swashbuckle (Swagger UI com botão de auth Bearer) |
| Testes | xUnit + FluentAssertions + NSubstitute + EF InMemory + WebApplicationFactory |
| Infra | Docker multi-stage + docker-compose (Postgres + API + RabbitMQ) |
| CI/CD | GitHub Actions: build + test + coverage |

---

## 🔐 Auth & Permissões

### Fluxo de tokens (production-grade)

- `POST /auth/register` — anônimo, registra usuário com role `Lab` ou `Admin`.
- `POST /auth/login` — anônimo, **rate-limited** (30 req/min, configurável), retorna `{ accessToken, refreshToken, accessTokenExpiresAt, refreshTokenExpiresAt }`. Access token vive **1h**, refresh token vive **30 dias**.
- `POST /auth/refresh` — anônimo, **rate-limited**, body `{ refreshToken }`, retorna novos tokens. **Rotation:** o refresh antigo é invalidado a cada uso (one-time-use).
- `POST /auth/logout` — anônimo, body `{ refreshToken }`, idempotente (204 sempre). Revoga o refresh token.

**Segurança:**
- Refresh tokens são 64 bytes random base64 (~88 chars URL-safe)
- Persistidos como **SHA-256 hash** — DB comprometido ≠ tokens vazados
- **Reuse detection (OWASP):** se um token já revogado é apresentado, TODA a cadeia daquele usuário é revogada (proteção contra roubo de token)
- Replay do mesmo refresh duas vezes → segunda chamada retorna 401

### Matriz de permissões

| Endpoint | Permissão |
|---|---|
| `POST /batches` | `Admin` |
| `GET /batches`, `/batches/{id}/summary` | autenticado |
| `PATCH /batches/{id}/status` | `Admin` |
| `DELETE /batches/{id}` | `Admin` (rejeita lotes em Testing/Released) |
| `POST /batches/{id}/analysis` | `Lab` ou `Admin` |
| `GET /batches/{id}/analyses` | `Lab` ou `Admin` |
| `POST /batches/{id}/sensor-data` | `Lab` ou `Admin` |
| `GET /batches/{id}/sensor-data` | autenticado |
| `GET /batches/{id}/daily-summaries` | autenticado |
| `GET /batches/{id}/status-history` | autenticado |
| `GET /batches/{id}/certificate-of-analysis` | autenticado |
| `POST /debug/populate-elegant` | `Admin` (apenas em Development/Testing) |

---

## 🐳 Como rodar

### Opção 1: docker-compose (tudo em um comando)

```bash
docker-compose up -d
```

Sobe Postgres (com healthcheck), API (com auto-migration on startup) e RabbitMQ broker. API disponível em `http://localhost:8080`, Swagger em `http://localhost:8080/swagger`.

### Opção 2: dev local

```bash
docker-compose up -d db                # só Postgres
dotnet ef database update              # aplica migrations
dotnet run                             # API em https://localhost:5xxx
```

Configurações relevantes em `appsettings.json` ou variáveis de ambiente:

```json
{
  "ConnectionStrings": { "Default": "Host=...;Database=lims_db;..." },
  "Jwt": { "Key": "...", "Issuer": "LimsProject", "Audience": "LimsProject" },
  "Rollup": { "IntervalSeconds": 60 },
  "RateLimit": { "LoginPerMinute": 30 }
}
```

---

## 🧪 Tests

```bash
dotnet test
```

90 testes cobrindo:

- **Unit tests** (validators): `BatchValidator`, `LabAnalysisValidator`, `SensorReadingValidator`, `RollupService`, `RollupWorker` (com NSubstitute)
- **Integration tests** (TestServer + EF InMemory): auth, batches CRUD/paginação/filtros/transições, análises, sensor data, daily summaries, status history, segurança (401/403)

Coverage com exclusões (migrations, generated files) via `coverlet.runsettings`.

---

## 🚨 Errors: Problem Details (RFC 7807) em toda a API

Toda resposta de erro segue [RFC 7807](https://datatracker.ietf.org/doc/html/rfc7807) — JSON estruturado com `type`, `title`, `status`, `detail`, e extensões custom quando aplicável. Exemplo de transição de status inválida:

```json
HTTP/1.1 422 Unprocessable Entity
Content-Type: application/problem+json

{
  "title": "Invalid status transition",
  "status": 422,
  "detail": "Transição inválida: Germination → Harvested.",
  "currentStatus": "Germination",
  "requestedStatus": "Harvested",
  "allowedTransitions": ["Growth"]
}
```

Factory centralizada em [`API/Problems.cs`](API/Problems.cs) evita strings de erro espalhadas e garante consistência.

---

## 🪪 Correlation IDs

Todo request carrega um `X-Correlation-Id` (recebido do cliente ou gerado server-side). O middleware:
- Devolve o ID no response header (cliente consegue referenciar)
- Substitui `HttpContext.TraceIdentifier`
- Empurra como **log scope** — toda linha de log do request carrega `CorrelationId={id}`

Pronto pra correlacionar logs entre microsserviços / camadas / loadbalancers.

---

## 📨 Eventos de domínio (RabbitMQ)

Eventos publicados no exchange topic `lims.events` (durável), com routing keys `lims.<eventname>`:

| Routing key | Evento | Disparado por |
|---|---|---|
| `lims.batchcreatedevent` | `BatchCreatedEvent(BatchId, Strain, OccurredAt)` | `POST /batches` |
| `lims.batchstatuschangedevent` | `BatchStatusChangedEvent(BatchId, From, To, ChangedBy, Reason, OccurredAt)` | `PATCH /batches/{id}/status` e mudança automática via análise |
| `lims.analysiscompletedevent` | `AnalysisCompletedEvent(BatchId, AnalysisId, Thc, Cbd, Passed, OccurredAt)` | `POST /batches/{id}/analysis` |

### Outbox pattern — zero dual-write, zero perda de evento

`IEventPublisher` é abstração no Application layer com duas implementações:

- **`OutboxEventPublisher`** (broker on) — escreve `OutboxMessage` no DbContext **na mesma transação** da entidade de domínio. Endpoints chamam `events.PublishAsync(...)` *antes* de `SaveChangesAsync` — o save commita batch + outbox row atomicamente.
- **`NullEventPublisher`** (Testing / broker off) — no-op.

O **`OutboxRelayWorker`** (BackgroundService singleton) polla a cada `Outbox:PollIntervalSeconds` (default 2s), pega `Outbox:BatchSize` mensagens pendentes ordenadas por `CreatedAt`, e despacha pro RabbitMQ via `IRabbitMqClient` (cliente AMQP puro, sem responsabilidade de domain events). Em falha: `Attempts++` e `LastError = ex.Message`. Após `Outbox:MaxAttempts` (default 5) a mensagem fica órfã — dead-letter implícito, fácil de inspecionar por query SQL.

**Resultados:**
- Processo crasha entre commit do batch e publish? Evento já está na tabela → worker pega depois.
- Broker offline? Mensagens acumulam até voltar (eventually consistent).
- Worker requer broker ativo; endpoints **não dependem** do broker em runtime.

Habilitado via `RabbitMq__Enabled=true`. O `docker-compose up` sobe o broker e habilita; rodando local sem broker, `NullEventPublisher` é usado automaticamente.

---

## 📊 Métricas de domínio (OpenTelemetry)

Métricas customizadas expostas via `Meter` "LimsProject":

| Métrica | Tipo | Tags | Descrição |
|---|---|---|---|
| `lims.batches.created` | Counter | — | Lotes criados |
| `lims.analyses.completed` | Counter | `passed=true\|false` | Análises laboratoriais finalizadas |
| `lims.status.transitions` | Counter | `from`, `to` | Mudanças de status de lote |

Mais traces HTTP do ASP.NET Core e métricas de runtime (.NET GC, heap, thread pool). Console exporter por padrão; em produção, configurar OTLP via env var `OTEL_EXPORTER_OTLP_ENDPOINT`. Desativado no ambiente `Testing` para não poluir output.

---

## 📋 Decisões de design dignas de nota

- **InMemory provider em Testing**: `Program.cs` só registra Npgsql fora do ambiente `Testing`, evitando o conflito `IDatabaseProvider` quando o `WebApplicationFactory` injeta o InMemory provider.
- **`IServiceScopeFactory` no worker**: `RollupWorker` é singleton mas precisa de `DbContext` (scoped) — cria um scope por iteração, com try/catch e intervalo configurável.
- **`StatusHistoryRecorder` estático**: cross-cutting concern simples para gravar auditoria sem ServiceLocator nem MediatR; recebe `ClaimsPrincipal` para capturar o usuário sem acoplar à `HttpContext`.
- **Máquina de estados explícita**: dicionário de transições válidas em `BatchEndpoints` evita lógica espalhada e devolve mensagem clara das opções permitidas.

---

> 💡 **Curiosidade**: a coluna `AvarageTemperature` no Postgres preserva um typo legado para mostrar mapeamento explícito via `HasColumnName` — código limpo (`Batch.AverageTemperature`) com schema legado intacto.
