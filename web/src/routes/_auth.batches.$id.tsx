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
import { Skeleton } from "@/components/ui/skeleton";
import { ConfirmDialog } from "@/components/ui/confirm-dialog";
import { BatchStatusBadge } from "@/components/BatchStatusBadge";
import { NewAnalysisForm } from "@/components/NewAnalysisForm";
import { SensorDataChart } from "@/components/SensorDataChart";
import { SensorReadingInput } from "@/components/SensorReadingInput";
import { SensorReadingsTable } from "@/components/SensorReadingsTable";
import { SensorSimulator } from "@/components/SensorSimulator";
import { fmtDateTime, fmtPercent, fmtTemperature } from "@/lib/format";
import { ApiError } from "@/lib/api";
import { BatchStatus, BatchStatusLabel, type BatchStatusValue } from "@/types/api";
import { ArrowLeft, FileText, Trash2 } from "lucide-react";
import { useState } from "react";
import { toast } from "sonner";

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
  const [isSimulating, setIsSimulating] = useState(false);
  const { data: batch, isLoading } = useBatchSummary(id, { refetchInterval: isSimulating ? 4000 : false });
  const { data: analyses } = useBatchAnalyses(id);
  const { data: history } = useBatchStatusHistory(id);
  const update = useUpdateBatchStatus(id);
  const remove = useDeleteBatch();
  const [confirmOpen, setConfirmOpen] = useState(false);

  if (isLoading || !batch) {
    return (
      <div className="space-y-6 p-8">
        <Skeleton className="h-8 w-64" />
        <div className="grid grid-cols-1 gap-6 lg:grid-cols-3">
          <Skeleton className="h-48" />
          <Skeleton className="h-48" />
          <Skeleton className="h-48" />
        </div>
        <Skeleton className="h-64" />
      </div>
    );
  }

  const onTransition = async (target: BatchStatusValue) => {
    try {
      await update.mutateAsync({ status: target });
      toast.success(`Status atualizado para ${BatchStatusLabel[target]}`);
    } catch (e) {
      const msg = e instanceof ApiError ? (e.detail ?? "Falha na transição") : "Erro inesperado";
      toast.error(msg);
    }
  };

  const onDelete = async () => {
    setConfirmOpen(false);
    try {
      await remove.mutateAsync(id);
      toast.success("Lote excluído (soft delete preservado pra auditoria)");
      navigate({ to: "/batches" });
    } catch (e) {
      const msg = e instanceof ApiError ? (e.detail ?? "Falha ao excluir") : "Erro inesperado";
      toast.error(msg);
    }
  };

  const allowedNext = transitions[batch.status];
  const isTerminal = allowedNext.length === 0;

  return (
    <>
      <div className="space-y-6 p-8">
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-3">
            <Button variant="ghost" size="icon" onClick={() => navigate({ to: "/batches" })}>
              <ArrowLeft className="h-4 w-4" />
            </Button>
            <div>
              <h1 className="text-2xl font-bold tracking-tight">{batch.strain}</h1>
              <p className="font-mono text-xs text-muted-foreground">{batch.id}</p>
            </div>
          </div>
          <div className="flex items-center gap-2">
            <Link to="/batches/$id/coa" params={{ id }}>
              <Button variant="outline">
                <FileText className="h-4 w-4" /> Certificado de Análise
              </Button>
            </Link>
            <Button variant="destructive" size="icon" onClick={() => setConfirmOpen(true)}>
              <Trash2 className="h-4 w-4" />
            </Button>
          </div>
        </div>

        <div className="grid grid-cols-1 gap-6 lg:grid-cols-3">
          <Card>
            <CardHeader>
              <CardTitle>Status atual</CardTitle>
            </CardHeader>
            <CardContent>
              <div className="mb-4">
                <BatchStatusBadge status={batch.status} className="text-base" />
              </div>
              {!isTerminal ? (
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
              <Row label="THC" value={fmtPercent(batch.thcPercentage)} />
              <Row label="CBD" value={fmtPercent(batch.cbdPercentage)} />
              <Row label="Temperatura atual" value={fmtTemperature(batch.currentTemperature)} />
              <Row label="Umidade atual" value={fmtPercent(batch.currentMoisture)} />
              <Row label="Contaminantes" value={batch.hasContaminants ? "Sim" : "Não"} />
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>Análises ({analyses?.length ?? 0})</CardTitle>
            </CardHeader>
            <CardContent>
              {analyses && analyses.length > 0 ? (
                <ul className="space-y-2 text-sm">
                  {analyses.slice(0, 3).map((a) => (
                    <li key={a.id} className="rounded-md border border-border p-2">
                      <div className="flex items-center justify-between">
                        <span className={a.isPassed ? "text-emerald-500" : "text-red-500"}>
                          {a.isPassed ? "✓ Aprovada" : "✗ Reprovada"}
                        </span>
                        <span className="text-xs text-muted-foreground">{fmtDateTime(a.analysisDate)}</span>
                      </div>
                      <div className="mt-1 text-xs text-muted-foreground">
                        THC {a.thc}% · CBD {a.cbd}% · {a.terpenes || "—"}
                      </div>
                    </li>
                  ))}
                  {analyses.length > 3 && (
                    <li className="text-xs text-muted-foreground">+ {analyses.length - 3} análise(s) anteriores</li>
                  )}
                </ul>
              ) : (
                <p className="text-xs text-muted-foreground">Sem análises registradas.</p>
              )}
            </CardContent>
          </Card>
        </div>

        {/* Telemetria — só faz sentido nos estados ativos */}
        {!isTerminal && (
          <>
            <div className="flex justify-end">
              <SensorSimulator batchId={id} onActiveChange={setIsSimulating} />
            </div>
            <div className="grid grid-cols-1 gap-6 lg:grid-cols-3">
              <div className="lg:col-span-2">
                <SensorDataChart batchId={id} live={isSimulating} />
              </div>
              <SensorReadingInput batchId={id} />
            </div>
            <SensorReadingsTable batchId={id} live={isSimulating} />
          </>
        )}

        {/* Form de análise — só quando lote está em pipeline (não terminal) */}
        {!isTerminal && <NewAnalysisForm batchId={id} />}

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
                      {fmtDateTime(h.changedAt)} · {h.changedBy}
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

      <ConfirmDialog
        open={confirmOpen}
        title="Excluir lote?"
        description={`O lote "${batch.strain}" sofrerá soft delete — fica preservado fisicamente no banco pra auditoria regulatória, mas some das listagens.`}
        confirmLabel="Excluir"
        cancelLabel="Cancelar"
        destructive
        onConfirm={onDelete}
        onCancel={() => setConfirmOpen(false)}
      />
    </>
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
