# 🌿 Seed-to-Sale LIMS (Laboratory Information Management System)

Sistema de rastreabilidade e análise química para cultivo orgânico, focado em conformidade e performance.

## 🚀 O que este sistema faz?
Este projeto resolve o problema de monitoramento de grandes volumes de dados de sensores e validação de laudos laboratoriais usando:

- **Ingestão de Dados:** Simulação de sensores de temperatura/umidade via Background Services.
- **Logica de Rollup:** Processamento em segundo plano para consolidar milhões de logs em médias diárias (Performance de Banco de Dados).
- **Validação Química:** Uso de `FluentValidation` para garantir que lotes só sejam liberados se estiverem dentro das normas (ex: limite de THC).
- **Rastreabilidade Total:** Certificado de análise que cruza dados de cultivo com dados de laboratório.

## 🛠️ Tech Stack
- **Backend:** .NET 8/9 (Minimal APIs)
- **Banco de Dados:** PostgreSQL rodando em Docker
- **ORM:** Entity Framework Core
- **Validation:** FluentValidation
- **Tools:** DBeaver, Swagger (OpenAPI), Bogus (Data Seeding)

## 🐳 Como rodar o projeto?
1. **Subir o Banco:**
   `docker-compose up -d`
2. **Atualizar Migrations:**
   `dotnet ef database update`
3. **Popular Dados de Teste:**
   Acesse o Swagger em `/swagger` e execute o endpoint `/debug/populate`.

---
> **Curiosidade Técnica:** O sistema utiliza `IServiceScopeFactory` para gerenciar contextos de banco de dados dentro de Singletons (BackgroundWorkers), evitando vazamentos de memória e conflitos de concorrência.