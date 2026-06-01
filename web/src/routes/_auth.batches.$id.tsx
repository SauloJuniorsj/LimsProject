import { createFileRoute, Link, useNavigate } from "@tanstack/react-router";
import {
  useBatchAnalyses,
  useBatchStatusHistory,
  useBatchSummary,
  useDeleteBatch,
  useUpdateBatchStatus,
} from "@/hooks/useBatches";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { BatchStatusBadge } from "@/components/BatchStatusBadge";
import { ApiError } from "@/lib/api";
import { BatchStatus, BatchStatusLabel, type BatchStatusValue } from "@/types/api";
import { ArrowLeft, FileText, Trash2 } from "lucide-react";
import { useState } from "react";

export const Route = createFileRoute("/_auth/batches/$id")({ component: BatchDetail });

const transitions: Record<BatchStatusValue, BatchStatusValue[]> = {
  [BatchStatus.Germination]: [BatchStatus.Growth],
  [BatchStatus.Growth]: [BatchStatus.Harvested],
  [BatchStatus.Harvested]: [BatchStatus.Testing],
  [BatchStatus.Testing]: [BatchStatus.Released, BatchStatus.Rejected],
  [BatchStatus.Released]: [],
  [BatchStatus.Rejected]: [],
};

function BatchDetail() {
  const { id } = Route.useParams();
  const navigate = useNavigate();
  const { data: batch, isLoading } = useBatchSummary(id);
  const { data: analyses } = useBatchAnalyses(id);
  const { data: history } = useBatchStatusHistory(id);
  const update = useUpdateBatchStatus(id);
  const remove = useDeleteBatch();
  const [actionError, setActionError] = useState<string | null>(null);

  if (isLoading || !batch) {
    return <div className="p-8 text-muted-foreground">Carregando...</div>;
  }

  const onTransition = async (target: BatchStatusValue) => {
    setActionError(null);
    try {
      await update.mutateAsync({ status: target });
    } catch (e) {
      if (e instanceof ApiError) setActionError(e.detail ?? "Falha na transição");
    }
  };

  const onDelete = async () => {
    if (!confirm("Excluir este lote? (soft delete — fica preservado pra auditoria)")) return;
    setActionError(null);
    try {
      await remove.mutateAsync(id);
      navigate({ to: "/batches" });
    } catch (e) {
      if (e instanceof ApiError) setActionError(e.detail ?? "Falha ao excluir");
    }
  };

  const allowedNext = transitions[batch.status];

  return (
    <div className="space-y-6 p-8">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <Button variant="ghost" size="icon" onClick={() => navigate({ to: "/batches" })}>
            <ArrowLeft className="h-4 w-4" />
          </Button>
          <div>
            <h1 className="text-2xl font-bold tracking-tight">{batch.strain}</h1>
            <p className="text-xs text-muted-foreground font-mono">{batch.id}</p>
          </div>
        </div>
        <div className="flex items-center gap-2">
          <Link to="/batches/$id/coa" params={{ id }}>
            <Button variant="outline">
              <FileText className="h-4 w-4" /> Certificado de Análise
            </Button>
          </Link>
          <Button variant="destructive" size="icon" onClick={onDelete}>
            <Trash2 className="h-4 w-4" />
          </Button>
        </div>
      </div>

      {actionError && (
        <div className="rounded-md border border-destructive/50 bg-destructive/10 px-4 py-3 text-sm text-destructive">
          {actionError}
        </div>
      )}

      <div className="grid grid-cols-1 gap-6 lg:grid-cols-3">
        <Card>
          <CardHeader>
            <CardTitle>Status atual</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="mb-4"><BatchStatusBadge status={batch.status} className="text-base" /></div>
            {allowedNext.length > 0 ? (
              <div>
                <p className="mb-2 text-xs text-muted-foreground">Transições permitidas:</p>
                <div className="flex flex-wrap gap-2">
                  {allowedNext.map((s) => (
                    <Button
                      key={s}
                      variant="outline"
                      size="sm"
                      onClick={() => onTransition(s)}
                      disabled={update.isPending}
                    >
                      → {BatchStatusLabel[s]}
                    </Button>
                  ))}
                </div>
              </div>
            ) : (
              <p className="text-xs text-muted-foreground">Estado terminal — sem transições.</p>
            )}
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Resumo</CardTitle>
          </CardHeader>
          <CardContent className="space-y-2 text-sm">
            <Row label="THC" value={batch.thcPercentage != null ? `${batch.thcPercentage}%` : "—"} />
            <Row label="CBD" value={batch.cbdPercentage != null ? `${batch.cbdPercentage}%` : "—"} />
            <Row label="Temperatura atual" value={batch.currentTemperature != null ? `${batch.currentTemperature}°C` : "—"} />
            <Row label="Umidade atual" value={batch.currentMoisture != null ? `${batch.currentMoisture}%` : "—"} />
            <Row label="Contaminantes" value={batch.hasContaminants ? "Sim" : "Não"} />
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Análises laboratoriais</CardTitle>
            <CardDescription>{analyses?.length ?? 0} análise(s)</CardDescription>
          </CardHeader>
          <CardContent>
            {analyses && analyses.length > 0 ? (
              <ul className="space-y-2 text-sm">
                {analyses.map((a) => (
                  <li key={a.id} className="rounded-md border border-border p-2">
                    <div className="flex items-center justify-between">
                      <span className={a.isPassed ? "text-emerald-500" : "text-red-500"}>
                        {a.isPassed ? "✓ Aprovada" : "✗ Reprovada"}
                      </span>
                      <span className="text-xs text-muted-foreground">
                        {new Date(a.analysisDate).toLocaleDateString("pt-BR")}
                      </span>
                    </div>
                    <div className="mt-1 text-xs text-muted-foreground">
                      THC {a.thc}% · CBD {a.cbd}% · {a.terpenes || "—"}
                    </div>
                  </li>
                ))}
              </ul>
            ) : (
              <p className="text-xs text-muted-foreground">Sem análises registradas.</p>
            )}
          </CardContent>
        </Card>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Trilha de auditoria</CardTitle>
          <CardDescription>Histórico de transições de status</CardDescription>
        </CardHeader>
        <CardContent>
          {history && history.length > 0 ? (
            <ol className="relative space-y-4 border-l border-border pl-6">
              {history.map((h) => (
                <li key={h.id} className="relative">
                  <span className="absolute -left-[31px] flex h-3 w-3 -translate-y-0.5 items-center justify-center rounded-full bg-primary" />
                  <div className="flex items-center gap-2 text-sm">
                    {h.fromStatus !== null && (
                      <>
                        <BatchStatusBadge status={h.fromStatus} />
                        <span className="text-muted-foreground">→</span>
                      </>
                    )}
                    <BatchStatusBadge status={h.toStatus} />
                  </div>
                  <div className="mt-1 text-xs text-muted-foreground">
                    {new Date(h.changedAt).toLocaleString("pt-BR")} · {h.changedBy}
                  </div>
                  {h.reason && (
                    <div className="mt-1 text-xs italic text-muted-foreground">"{h.reason}"</div>
                  )}
                </li>
              ))}
            </ol>
          ) : (
            <p className="text-xs text-muted-foreground">Sem histórico.</p>
          )}
        </CardContent>
      </Card>
    </div>
  );
}

function Row({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex justify-between">
      <span className="text-muted-foreground">{label}</span>
      <span className="font-medium">{value}</span>
    </div>
  );
}
