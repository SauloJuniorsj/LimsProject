---
name: write-tests-from-spec
description: Gera testes xUnit (unit + integration) a partir de uma spec em .claude/specs/. Lê os critérios de aceitação marcados [unit]/[integration] e produz arquivos de teste FALHANDO, seguindo as convenções do LimsProject (FluentAssertions, LimsWebApplicationFactory, nomes em português). Use quando o usuário disser "gera os testes da spec X" ou "/write-tests-from-spec X".
---

# write-tests-from-spec

Transforma uma spec aprovada em testes que falham (red phase do TDD), prontos pra implementação dirigir a passar.

## Quando usar

- Usuário pede explicitamente: `/write-tests-from-spec batch-archive` ou "escreve os testes da spec batch-archive".
- Existe uma spec em `.claude/specs/<slug>.md` com `status: approved` (ou draft, se o usuário insistir).

## Quando NÃO usar

- Spec ainda não escrita → primeiro pedir pra criar com o template em `.claude/specs/TEMPLATE.md`.
- Usuário pediu pra implementar a feature em si — essa skill só gera testes.

## Passos

### 1. Localizar e ler a spec

- Argumento típico: slug (ex: `batch-archive`). Procurar em `.claude/specs/<slug>.md`.
- Se o argumento for caminho completo, usar direto.
- Se não achar, listar specs disponíveis e parar.

### 2. Parsear seções relevantes

Da spec, extrair:

- **Seção 5 (Contrato HTTP)**: cada endpoint + status codes esperados.
- **Seção 6 (Validação)**: regras pra unit tests de validator.
- **Seção 9 (Critérios)**: cada bullet com tag `[unit]` ou `[integration]` vira um teste.

### 3. Decidir destinos

Convenção do projeto:

- **Integration tests**: `LimsProjectTests/Integration/<Feature>EndpointsTests.cs`
  - Herdam de `IClassFixture<LimsWebApplicationFactory>`.
  - Usam `factory.CreateAuthenticatedClientAsync("Admin"|"Lab"|...)` pra autenticar.
  - Asserções com `FluentAssertions`: `response.StatusCode.Should().Be(HttpStatusCode.X)`.
- **Unit tests**: `LimsProjectTests/Unit/<Validator>Tests.cs`
  - Instanciam o validator direto.
  - `[Theory]` + `[InlineData]` pra range/boundary, `[Fact]` pra casos pontuais.

Se o arquivo já existe, **adicionar métodos** ao final da classe (não sobrescrever). Se não existe, criar novo.

### 4. Gerar nomes de teste em português

Padrão observado no repo:

- `METODO_Rota_Retorna<Status>_<Condição>` para endpoints. Ex: `POST_Batches_Retorna201_ComLoteValido`.
- `<Campo>_<Condição>_<Resultado>` para validators. Ex: `THC_ForaDoIntervalo_Invalido`.

### 5. Estrutura de cada teste de integração

```csharp
[Fact]
public async Task POST_BatchesArchive_Retorna200_QuandoLoteEmReleased()
{
    var client = await factory.CreateAuthenticatedClientAsync("Admin");
    // arrange: criar lote, mover até Released
    // act
    var response = await client.PostAsync($"/batches/{id}/archive", null);
    // assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
}
```

Pra cenários que exigem setup repetido (criar lote + mover status), criar um helper privado no topo da classe — espelhar o que `BatchEndpointsTests.CriarLoteAsync` faz.

### 6. Estrutura de cada teste de unit (validator)

```csharp
[Fact]
public async Task Reason_MenosDe5Caracteres_Invalido()
{
    var input = Build(reason: "abc");
    var result = await _validator.ValidateAsync(input);
    result.IsValid.Should().BeFalse();
    result.Errors.Should().Contain(e => e.PropertyName == "Reason");
}
```

### 7. Marcar testes que ainda não compilam

Se o endpoint/validator referenciado pela spec **ainda não existe** (situação esperada — TDD), o build vai falhar. Isso é **intencional**: comunicar isso no relatório final ao usuário, junto com o que precisa ser implementado pra build passar.

Não usar `Skip = "..."` nem comentar os testes. O ponto é o build vermelho dirigir a implementação.

### 8. Relatório final

Ao terminar, mostrar ao usuário:

1. Arquivos criados/modificados.
2. Quantos testes [unit] e quantos [integration].
3. Lista de símbolos que precisam existir pra compilar (ex: "endpoint `POST /batches/{id}/archive`", "enum value `BatchStatus.Archived`", "evento `BatchArchivedEvent`").
4. Comando pra rodar: `dotnet test --filter "FullyQualifiedName~<FeatureName>"`.

## Convenções do repo (não improvisar)

- `using FluentAssertions;` + `using Xunit;` sempre.
- Asserções de status: `response.StatusCode.Should().Be(HttpStatusCode.X)` — **não** usar `Assert.Equal`.
- JSON: ler com `await response.Content.ReadFromJsonAsync<JsonElement>()` e navegar com `.GetProperty(...)`.
- Roles existentes: `Admin`, `Lab` (confirmar em `LimsWebApplicationFactory.CreateAuthenticatedClientAsync`).
- Namespace de integration: `LimsProjectTests.Integration`. Unit: `LimsProjectTests.Unit`.

## Falhar cedo

- Spec sem seção 9 (critérios) → parar e pedir pro usuário preencher.
- Critério sem tag `[unit]`/`[integration]` → parar, listar os bullets ambíguos, pedir clarificação.
- Endpoint sem método HTTP claro → parar.
