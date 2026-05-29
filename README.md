# 🌿 Seed-to-Sale LIMS

**Laboratory Information Management System** para empresas de cannabis: rastreabilidade do cultivo à liberação do lote, com auditoria de mudanças de status, ingestão de sensores e análises laboratoriais com validação de compliance (cânhamo).

[![CI](https://github.com/saulocintra/LimsProject/actions/workflows/ci.yml/badge.svg)](https://github.com/saulocintra/LimsProject/actions)
![.NET](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet)
![Postgres](https://img.shields.io/badge/PostgreSQL-16-336791?logo=postgresql)
![Tests](https://img.shields.io/badge/tests-98%20passing-brightgreen)

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
| Autenticação | ASP.NET Core Identity + JWT Bearer + Roles (`Admin`, `Lab`) |
| Validação | FluentValidation (validators no Application layer) |
| Background | `BackgroundService` + `IServiceScopeFactory` para scopes seguros |
| Rate limiting | `Microsoft.AspNetCore.RateLimiting` (fixed window no `/auth/login`) |
| Observabilidade | Health checks (`/health`) com check do DbContext |
| Documentação | Swashbuckle (Swagger UI com botão de auth Bearer) |
| Testes | xUnit + FluentAssertions + NSubstitute + EF InMemory + WebApplicationFactory |
| Infra | Docker multi-stage + docker-compose (Postgres + API + RabbitMQ) |
| CI/CD | GitHub Actions: build + test + coverage |

---

## 🔐 Auth & Permissões

- `POST /auth/register` — anônimo, registra usuário com role `Lab` ou `Admin`.
- `POST /auth/login` — anônimo, **rate-limited** (30 req/min/servidor, configurável), retorna JWT (HMAC-SHA256, 8h).

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

## 📋 Decisões de design dignas de nota

- **InMemory provider em Testing**: `Program.cs` só registra Npgsql fora do ambiente `Testing`, evitando o conflito `IDatabaseProvider` quando o `WebApplicationFactory` injeta o InMemory provider.
- **`IServiceScopeFactory` no worker**: `RollupWorker` é singleton mas precisa de `DbContext` (scoped) — cria um scope por iteração, com try/catch e intervalo configurável.
- **`StatusHistoryRecorder` estático**: cross-cutting concern simples para gravar auditoria sem ServiceLocator nem MediatR; recebe `ClaimsPrincipal` para capturar o usuário sem acoplar à `HttpContext`.
- **Máquina de estados explícita**: dicionário de transições válidas em `BatchEndpoints` evita lógica espalhada e devolve mensagem clara das opções permitidas.

---

> 💡 **Curiosidade**: a coluna `AvarageTemperature` no Postgres preserva um typo legado para mostrar mapeamento explícito via `HasColumnName` — código limpo (`Batch.AverageTemperature`) com schema legado intacto.
