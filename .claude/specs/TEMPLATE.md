---
feature: <slug-curto>             # ex: batch-archive
title: <Título legível>            # ex: Arquivamento de lotes liberados
status: draft                      # draft | approved | implemented
owner: <quem-pediu>
date: YYYY-MM-DD
---

# 1. Resumo

Uma frase. O que essa feature faz e pra quem.

# 2. Motivação (Why)

Por que isso existe agora. Qual problema do laboratório/operação resolve.
Se substituir/expande algo, citar o quê.

# 3. Escopo

**Dentro:**
- ...

**Fora (explicitamente):**
- ...

# 4. Domain Touchpoints

Quais entidades, enums, events e migrations são afetados.

- **Entidades:** `Batch`, ...
- **Enums:** `BatchStatus` (novo valor `Archived`?)
- **Events:** `BatchArchivedEvent`?
- **Migration:** sim/não — campos novos

# 5. Contrato HTTP

Listar cada endpoint novo/alterado com: método, rota, role, request, response, status codes.

### `POST /batches/{id}/archive`
- **Auth:** `AdminOnly`
- **Request:** _(vazio)_ ou `{ reason: string }`
- **Response 200:** `{ id, status, archivedAt }`
- **Erros:**
  - `400` se lote ainda não está em `Released` ou `Rejected`
  - `401` sem token
  - `403` role diferente de Admin
  - `404` lote inexistente

# 6. Regras de Negócio / Validação

Regras que serão verificadas em validator/handler/endpoint.

- Só permite arquivar lotes em estado terminal (`Released` / `Rejected`).
- `reason` (se enviado) deve ter entre 5 e 500 caracteres.
- ...

# 7. Máquina de Estados

Se mexe em transições, mostrar antes/depois (snippet do `ValidTransitions`).

# 8. Observability

- **Evento publicado:** `BatchArchivedEvent(BatchId, ArchivedAt, Reason)`
- **Métrica:** `lims_batch_archived_total`
- **StatusHistory:** registra transição com nota "Lote arquivado: {reason}"

# 9. Critérios de Aceitação (cenários testáveis)

> Cada bullet vira teste. Use Given/When/Then. Linguagem em português.
> Marque `[unit]` ou `[integration]` no início.

- `[integration]` **Dado** um lote em `Released`, **quando** Admin chama `POST /batches/{id}/archive`, **então** retorna `200` e `status = Archived`.
- `[integration]` **Dado** um lote em `Growth`, **quando** Admin tenta arquivar, **então** retorna `400`.
- `[integration]` **Dado** um lote em `Released`, **quando** usuário com role `Lab` tenta arquivar, **então** retorna `403`.
- `[integration]` **Dado** um lote inexistente, **quando** Admin tenta arquivar, **então** retorna `404`.
- `[unit]` Validator rejeita `reason` com menos de 5 caracteres.
- `[unit]` Validator aceita `reason` vazio (campo opcional).

# 10. Out-of-scope explícito

O que **não** vai ser testado/implementado nessa entrega (deixa explícito pra não inflar PR).

- UI no front (vem em PR separado).
- Bulk archive — só single batch.

# 11. Notas de implementação (opcional)

Pontos onde a implementação pode ter armadilha. Não é design completo — só pistas.

- Atualizar `ValidTransitions` em `BatchesController.cs`.
- Verificar se `RollupService` precisa ignorar lotes arquivados.
