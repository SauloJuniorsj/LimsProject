using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using LimsProject.Domain.Entities;
using LimsProject.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LimsProjectTests.Integration;

public class CertificateOfAnalysisTests(LimsWebApplicationFactory factory)
    : IClassFixture<LimsWebApplicationFactory>
{
    private async Task<(HttpClient admin, HttpClient lab, Guid id)> SetupAsync(string strain = "CoAStrain")
    {
        var admin = await factory.CreateAuthenticatedClientAsync("Admin");
        var lab = await factory.CreateAuthenticatedClientAsync("Lab");
        var resp = await admin.PostAsJsonAsync("/batches", new { strain });
        var id = (await resp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        return (admin, lab, id);
    }

    [Fact]
    public async Task GET_CoA_Retorna404_QuandoLoteNaoExiste()
    {
        var client = await factory.CreateAuthenticatedClientAsync("Admin");
        var response = await client.GetAsync($"/batches/{Guid.NewGuid()}/certificate-of-analysis");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GET_CoA_Retorna401_SemToken()
    {
        var client = factory.CreateClient();
        var response = await client.GetAsync($"/batches/{Guid.NewGuid()}/certificate-of-analysis");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GET_CoA_LoteRecemCriado_RetornaEstruturaBaseComLifecycle()
    {
        var (admin, _, id) = await SetupAsync("Fresh CoA Strain");

        var response = await admin.GetAsync($"/batches/{id}/certificate-of-analysis");
        var coa = await response.Content.ReadFromJsonAsync<JsonElement>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        coa.GetProperty("batchId").GetGuid().Should().Be(id);
        coa.GetProperty("strain").GetString().Should().Be("Fresh CoA Strain");
        coa.GetProperty("status").GetInt32().Should().Be(0); // Germination
        coa.GetProperty("analyses").GetArrayLength().Should().Be(0);
        coa.GetProperty("lifecycle").GetArrayLength().Should().Be(1); // criação inicial

        var env = coa.GetProperty("environmental");
        env.GetProperty("daysMonitored").GetInt32().Should().Be(0);
        env.GetProperty("totalReadings").GetInt32().Should().Be(0);
        env.GetProperty("overallAvgTemperature").ValueKind.Should().Be(JsonValueKind.Null);

        var compliance = coa.GetProperty("compliance");
        compliance.GetProperty("hasPassingAnalysis").GetBoolean().Should().BeFalse();
        compliance.GetProperty("analysisCount").GetInt32().Should().Be(0);
        compliance.GetProperty("lastAnalysisDate").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task GET_CoA_ComAnaliseAprovada_CompliancePositiva()
    {
        var (admin, lab, id) = await SetupAsync("Hemp Strain");

        await lab.PostAsJsonAsync($"/batches/{id}/analysis",
            new { thc = 0.25, cbd = 8.0, terpenes = "myrcene", isPassed = true });

        var coa = await admin.GetFromJsonAsync<JsonElement>($"/batches/{id}/certificate-of-analysis");

        coa.GetProperty("analyses").GetArrayLength().Should().Be(1);
        coa.GetProperty("status").GetInt32().Should().Be(4); // Released

        var compliance = coa.GetProperty("compliance");
        compliance.GetProperty("hasPassingAnalysis").GetBoolean().Should().BeTrue();
        compliance.GetProperty("hempCompliant").GetBoolean().Should().BeTrue(); // THC <= 0.3
        compliance.GetProperty("analysisCount").GetInt32().Should().Be(1);
        compliance.GetProperty("lastAnalysisDate").ValueKind.Should().NotBe(JsonValueKind.Null);

        // Lifecycle deve ter 2 entradas: criação + transição via análise
        coa.GetProperty("lifecycle").GetArrayLength().Should().Be(2);
    }

    [Fact]
    public async Task GET_CoA_ComAnaliseReprovada_NaoTemAnalisePassante()
    {
        var (admin, lab, id) = await SetupAsync("Failed Strain");

        await lab.PostAsJsonAsync($"/batches/{id}/analysis",
            new { thc = 0.2, cbd = 1.0, terpenes = "skunk", isPassed = false });

        var coa = await admin.GetFromJsonAsync<JsonElement>($"/batches/{id}/certificate-of-analysis");

        coa.GetProperty("status").GetInt32().Should().Be(5); // Rejected

        var compliance = coa.GetProperty("compliance");
        compliance.GetProperty("hasPassingAnalysis").GetBoolean().Should().BeFalse();
        compliance.GetProperty("hempCompliant").GetBoolean().Should().BeTrue(); // sem aprovações = trivialmente compliant
        compliance.GetProperty("analysisCount").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task GET_CoA_AgregaSumariosAmbientais_QuandoExistemRollups()
    {
        var (admin, _, id) = await SetupAsync("Env CoA Strain");

        // Insere sumários direto no DbContext (o rollup worker está desabilitado em testes)
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var today = DateTime.UtcNow.Date;
            db.BatchesDailySummaries.AddRange(
                new BatchDailySummary
                {
                    BatchId = id, Date = today.AddDays(-2),
                    AvgTemperature = 22m, MinTemperature = 18m, MaxTemperature = 26m, ReadingCount = 24
                },
                new BatchDailySummary
                {
                    BatchId = id, Date = today.AddDays(-1),
                    AvgTemperature = 24m, MinTemperature = 20m, MaxTemperature = 28m, ReadingCount = 24
                },
                new BatchDailySummary
                {
                    BatchId = id, Date = today,
                    AvgTemperature = 23m, MinTemperature = 19m, MaxTemperature = 30m, ReadingCount = 12
                });
            await db.SaveChangesAsync();
        }

        var coa = await admin.GetFromJsonAsync<JsonElement>($"/batches/{id}/certificate-of-analysis");
        var env = coa.GetProperty("environmental");

        env.GetProperty("daysMonitored").GetInt32().Should().Be(3);
        env.GetProperty("totalReadings").GetInt32().Should().Be(60); // 24 + 24 + 12
        env.GetProperty("overallAvgTemperature").GetDecimal().Should().Be(23m); // (22+24+23)/3
        env.GetProperty("overallMinTemperature").GetDecimal().Should().Be(18m);
        env.GetProperty("overallMaxTemperature").GetDecimal().Should().Be(30m);
    }

    [Fact]
    public async Task GET_CoA_LifecycleOrdenadoCronologicamenteAsc()
    {
        var (admin, _, id) = await SetupAsync();

        await admin.PatchAsJsonAsync($"/batches/{id}/status", new { status = 1, reason = "Vegetativo" });
        await admin.PatchAsJsonAsync($"/batches/{id}/status", new { status = 2, reason = "Colheita" });

        var coa = await admin.GetFromJsonAsync<JsonElement>($"/batches/{id}/certificate-of-analysis");
        var lifecycle = coa.GetProperty("lifecycle");

        lifecycle.GetArrayLength().Should().Be(3);
        // ASC no CoA (cronológico natural): criação → Growth → Harvested
        lifecycle[0].GetProperty("toStatus").GetInt32().Should().Be(0);
        lifecycle[1].GetProperty("toStatus").GetInt32().Should().Be(1);
        lifecycle[2].GetProperty("toStatus").GetInt32().Should().Be(2);
    }

    [Fact]
    public async Task GET_CoA_IssuedAt_EhPreenchidoComUtcNow()
    {
        var (admin, _, id) = await SetupAsync();
        var before = DateTime.UtcNow.AddSeconds(-1);

        var coa = await admin.GetFromJsonAsync<JsonElement>($"/batches/{id}/certificate-of-analysis");
        var after = DateTime.UtcNow.AddSeconds(1);

        var issuedAt = coa.GetProperty("issuedAt").GetDateTime();
        issuedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }
}
