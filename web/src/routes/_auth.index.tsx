import { createFileRoute } from "@tanstack/react-router";
import { useBatchesList } from "@/hooks/useBatches";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { BatchStatus, BatchStatusLabel, type BatchStatusValue } from "@/types/api";
import { BatchStatusBadge } from "@/components/BatchStatusBadge";
import { Sprout, FlaskConical, CheckCircle2, XCircle } from "lucide-react";

export const Route = createFileRoute("/_auth/")({ component: Dashboard });

function Dashboard() {
  // Estratégia simples pra portfolio: busca uma página grande e agrega no client.
  // Para produção, mover pra endpoint específico de agregação.
  const { data, isLoading } = useBatchesList({ page: 1, pageSize: 100 });

  const byStatus = (status: BatchStatusValue) =>
    data?.items.filter((b) => b.status === status).length ?? 0;

  const total = data?.totalCount ?? 0;
  const released = byStatus(BatchStatus.Released);
  const rejected = byStatus(BatchStatus.Rejected);
  const inProgress = total - released - rejected;
  const approvalRate = released + rejected > 0
    ? ((released / (released + rejected)) * 100).toFixed(1)
    : "—";

  return (
    <div className="space-y-6 p-8">
      <div>
        <h1 className="text-3xl font-bold tracking-tight">Dashboard</h1>
        <p className="text-sm text-muted-foreground">Visão geral dos lotes e análises</p>
      </div>

      <div className="grid grid-cols-1 gap-4 md:grid-cols-2 lg:grid-cols-4">
        <KpiCard
          title="Total de lotes"
          value={isLoading ? "…" : String(total)}
          icon={<Sprout className="h-4 w-4 text-emerald-500" />}
        />
        <KpiCard
          title="Em andamento"
          value={isLoading ? "…" : String(inProgress)}
          icon={<FlaskConical className="h-4 w-4 text-sky-500" />}
        />
        <KpiCard
          title="Aprovados"
          value={isLoading ? "…" : String(released)}
          icon={<CheckCircle2 className="h-4 w-4 text-teal-500" />}
        />
        <KpiCard
          title="Taxa de aprovação"
          value={isLoading ? "…" : `${approvalRate}${approvalRate !== "—" ? "%" : ""}`}
          icon={<XCircle className="h-4 w-4 text-red-500" />}
        />
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Lotes por status</CardTitle>
          <CardDescription>Distribuição atual da pipeline</CardDescription>
        </CardHeader>
        <CardContent>
          <div className="grid grid-cols-2 gap-3 md:grid-cols-3 lg:grid-cols-6">
            {Object.values(BatchStatus).map((s) => (
              <div key={s} className="rounded-md border border-border p-4">
                <BatchStatusBadge status={s} />
                <div className="mt-2 text-2xl font-semibold">{byStatus(s)}</div>
                <div className="text-xs text-muted-foreground">{BatchStatusLabel[s]}</div>
              </div>
            ))}
          </div>
        </CardContent>
      </Card>
    </div>
  );
}

function KpiCard({ title, value, icon }: { title: string; value: string; icon: React.ReactNode }) {
  return (
    <Card>
      <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
        <CardTitle className="text-sm font-medium text-muted-foreground">{title}</CardTitle>
        {icon}
      </CardHeader>
      <CardContent>
        <div className="text-2xl font-bold">{value}</div>
      </CardContent>
    </Card>
  );
}
