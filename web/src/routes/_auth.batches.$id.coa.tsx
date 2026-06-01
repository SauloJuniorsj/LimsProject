import { createFileRoute, useNavigate } from "@tanstack/react-router";
import { useBatchCoA } from "@/hooks/useBatches";
import { Button } from "@/components/ui/button";
import { BatchStatusBadge } from "@/components/BatchStatusBadge";
import { BatchStatusLabel } from "@/types/api";
import { ArrowLeft, Printer } from "lucide-react";

export const Route = createFileRoute("/_auth/batches/$id/coa")({ component: CoAView });

function CoAView() {
  const { id } = Route.useParams();
  const navigate = useNavigate();
  const { data: coa, isLoading } = useBatchCoA(id);

  if (isLoading || !coa) return <div className="p-8 text-muted-foreground">Carregando...</div>;

  return (
    <div className="space-y-6 p-8">
      <div className="flex items-center justify-between print:hidden">
        <Button variant="ghost" onClick={() => navigate({ to: "/batches/$id", params: { id } })}>
          <ArrowLeft className="h-4 w-4" /> Voltar
        </Button>
        <Button onClick={() => window.print()}>
          <Printer className="h-4 w-4" /> Imprimir
        </Button>
      </div>

      <div className="mx-auto max-w-4xl rounded-lg border border-border bg-card p-8 print:border-0 print:p-0">
        <header className="mb-6 border-b border-border pb-4">
          <h1 className="text-2xl font-bold">Certificate of Analysis</h1>
          <p className="text-sm text-muted-foreground">
            Emitido em {new Date(coa.issuedAt).toLocaleString("pt-BR")}
          </p>
        </header>

        <Section title="Identificação do lote">
          <Grid>
            <Field label="Strain" value={coa.strain} />
            <Field label="ID" value={<code className="text-xs">{coa.batchId}</code>} />
            <Field label="Status" value={<BatchStatusBadge status={coa.status} />} />
            <Field label="Criado em" value={new Date(coa.batchCreatedAt).toLocaleDateString("pt-BR")} />
          </Grid>
        </Section>

        <Section title="Compliance">
          <Grid>
            <Field label="Tem análise aprovada" value={coa.compliance.hasPassingAnalysis ? "Sim" : "Não"} />
            <Field
              label="Hemp compliant (THC ≤ 0.3%)"
              value={
                <span className={coa.compliance.hempCompliant ? "text-emerald-500" : "text-red-500"}>
                  {coa.compliance.hempCompliant ? "✓ Conforme" : "✗ Não conforme"}
                </span>
              }
            />
            <Field label="Total de análises" value={String(coa.compliance.analysisCount)} />
            <Field
              label="Última análise"
              value={coa.compliance.lastAnalysisDate
                ? new Date(coa.compliance.lastAnalysisDate).toLocaleDateString("pt-BR")
                : "—"}
            />
          </Grid>
        </Section>

        <Section title="Condições ambientais agregadas">
          <Grid>
            <Field label="Dias monitorados" value={String(coa.environmental.daysMonitored)} />
            <Field label="Leituras totais" value={String(coa.environmental.totalReadings)} />
            <Field
              label="Temperatura média"
              value={coa.environmental.overallAvgTemperature != null ? `${coa.environmental.overallAvgTemperature.toFixed(1)}°C` : "—"}
            />
            <Field
              label="Mín / Máx"
              value={
                coa.environmental.overallMinTemperature != null
                  ? `${coa.environmental.overallMinTemperature.toFixed(1)}°C / ${coa.environmental.overallMaxTemperature?.toFixed(1)}°C`
                  : "—"
              }
            />
          </Grid>
        </Section>

        <Section title={`Análises laboratoriais (${coa.analyses.length})`}>
          {coa.analyses.length === 0 ? (
            <p className="text-sm text-muted-foreground">Nenhuma análise registrada.</p>
          ) : (
            <table className="w-full border-collapse text-sm">
              <thead className="border-b border-border">
                <tr className="text-left text-xs uppercase text-muted-foreground">
                  <th className="py-2 pr-3">Data</th>
                  <th className="py-2 pr-3">THC</th>
                  <th className="py-2 pr-3">CBD</th>
                  <th className="py-2 pr-3">Terpenes</th>
                  <th className="py-2">Resultado</th>
                </tr>
              </thead>
              <tbody>
                {coa.analyses.map((a) => (
                  <tr key={a.id} className="border-b border-border/30">
                    <td className="py-2 pr-3">{new Date(a.analysisDate).toLocaleDateString("pt-BR")}</td>
                    <td className="py-2 pr-3">{a.thc}%</td>
                    <td className="py-2 pr-3">{a.cbd}%</td>
                    <td className="py-2 pr-3">{a.terpenes || "—"}</td>
                    <td className="py-2">
                      <span className={a.isPassed ? "text-emerald-500" : "text-red-500"}>
                        {a.isPassed ? "✓ Aprovada" : "✗ Reprovada"}
                      </span>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </Section>

        <Section title={`Ciclo de vida (${coa.lifecycle.length})`}>
          <ol className="space-y-2 text-sm">
            {coa.lifecycle.map((h) => (
              <li key={h.id} className="flex items-baseline gap-3">
                <span className="font-mono text-xs text-muted-foreground">
                  {new Date(h.changedAt).toLocaleString("pt-BR")}
                </span>
                <span>
                  {h.fromStatus !== null ? `${BatchStatusLabel[h.fromStatus]} → ` : ""}
                  <strong>{BatchStatusLabel[h.toStatus]}</strong>
                </span>
                <span className="text-xs text-muted-foreground">por {h.changedBy}</span>
                {h.reason && <span className="text-xs italic text-muted-foreground">"{h.reason}"</span>}
              </li>
            ))}
          </ol>
        </Section>
      </div>
    </div>
  );
}

function Section({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <section className="mb-6">
      <h2 className="mb-3 text-sm font-semibold uppercase tracking-wide text-muted-foreground">{title}</h2>
      {children}
    </section>
  );
}

function Grid({ children }: { children: React.ReactNode }) {
  return <div className="grid grid-cols-2 gap-x-6 gap-y-3 md:grid-cols-4">{children}</div>;
}

function Field({ label, value }: { label: string; value: React.ReactNode }) {
  return (
    <div>
      <div className="text-xs text-muted-foreground">{label}</div>
      <div className="text-sm font-medium">{value}</div>
    </div>
  );
}
