import { createFileRoute, Link } from "@tanstack/react-router";
import { useBatchesList } from "@/hooks/useBatches";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import { BatchStatus, BatchStatusLabel, type Batch, type BatchStatusValue } from "@/types/api";
import { BatchStatusBadge } from "@/components/BatchStatusBadge";
import { fmtDate } from "@/lib/format";
import {
  Bar,
  BarChart,
  Cell,
  Pie,
  PieChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from "recharts";
import { CheckCircle2, FlaskConical, Sprout, XCircle, ArrowRight } from "lucide-react";

export const Route = createFileRoute("/_auth/")({ component: Dashboard });

// Cores OKLCH alinhadas com os badges
const statusColors: Record<BatchStatusValue, string> = {
  [BatchStatus.Germination]: "oklch(0.7 0.15 145)",
  [BatchStatus.Growth]: "oklch(0.65 0.18 145)",
  [BatchStatus.Harvested]: "oklch(0.72 0.15 80)",
  [BatchStatus.Testing]: "oklch(0.7 0.18 250)",
  [BatchStatus.Released]: "oklch(0.62 0.2 145)",
  [BatchStatus.Rejected]: "oklch(0.6 0.22 27)",
};

function Dashboard() {
  // Pega uma página grande pra computar KPIs e charts no client (suficiente p/ portfolio)
  const { data, isLoading } = useBatchesList({ page: 1, pageSize: 100, sortBy: "createdAt", sortDir: "desc" });

  const items = data?.items ?? [];

  const byStatus = (status: BatchStatusValue) => items.filter((b) => b.status === status).length;

  const total = data?.totalCount ?? 0;
  const released = byStatus(BatchStatus.Released);
  const rejected = byStatus(BatchStatus.Rejected);
  const inProgress = total - released - rejected;
  const approvalRate =
    released + rejected > 0
      ? `${((released / (released + rejected)) * 100).toFixed(1)}%`
      : "—";

  const chartData = Object.values(BatchStatus).map((s) => ({
    status: BatchStatusLabel[s],
    statusValue: s,
    count: byStatus(s),
    fill: statusColors[s],
  }));

  return (
    <div className="space-y-6 p-8">
      <div>
        <h1 className="text-3xl font-bold tracking-tight">Dashboard</h1>
        <p className="text-sm text-muted-foreground">Visão geral dos lotes e análises</p>
      </div>

      <div className="grid grid-cols-1 gap-4 md:grid-cols-2 lg:grid-cols-4">
        <KpiCard
          title="Total de lotes"
          value={isLoading ? null : String(total)}
          icon={<Sprout className="h-4 w-4 text-emerald-500" />}
        />
        <KpiCard
          title="Em andamento"
          value={isLoading ? null : String(inProgress)}
          icon={<FlaskConical className="h-4 w-4 text-sky-500" />}
        />
        <KpiCard
          title="Aprovados"
          value={isLoading ? null : String(released)}
          icon={<CheckCircle2 className="h-4 w-4 text-teal-500" />}
        />
        <KpiCard
          title="Taxa de aprovação"
          value={isLoading ? null : approvalRate}
          icon={<XCircle className="h-4 w-4 text-red-500" />}
        />
      </div>

      <div className="grid grid-cols-1 gap-6 lg:grid-cols-2">
        <Card>
          <CardHeader>
            <CardTitle>Distribuição por status</CardTitle>
            <CardDescription>Pipeline atual dos lotes</CardDescription>
          </CardHeader>
          <CardContent>
            {isLoading ? (
              <Skeleton className="h-64 w-full" />
            ) : total === 0 ? (
              <EmptyChart message="Sem lotes ainda — crie o primeiro em /batches" />
            ) : (
              <ResponsiveContainer width="100%" height={260}>
                <PieChart>
                  <Pie
                    data={chartData.filter((d) => d.count > 0)}
                    dataKey="count"
                    nameKey="status"
                    innerRadius={60}
                    outerRadius={90}
                    paddingAngle={2}
                    label={({ value }) => String(value ?? "")}
                  >
                    {chartData.map((d) => (
                      <Cell key={d.status} fill={d.fill} />
                    ))}
                  </Pie>
                  <Tooltip
                    contentStyle={{
                      backgroundColor: "var(--card)",
                      border: "1px solid var(--border)",
                      borderRadius: "var(--radius)",
                      fontSize: 13,
                    }}
                  />
                </PieChart>
              </ResponsiveContainer>
            )}
            <div className="mt-4 flex flex-wrap gap-3">
              {chartData.map((d) => (
                <div key={d.status} className="flex items-center gap-1.5 text-xs">
                  <span
                    className="h-2.5 w-2.5 rounded-full"
                    style={{ backgroundColor: d.fill }}
                  />
                  <span className="text-muted-foreground">{d.status}</span>
                  <span className="font-medium">{d.count}</span>
                </div>
              ))}
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Volume por status</CardTitle>
            <CardDescription>Quantidade absoluta de lotes em cada estado</CardDescription>
          </CardHeader>
          <CardContent>
            {isLoading ? (
              <Skeleton className="h-64 w-full" />
            ) : total === 0 ? (
              <EmptyChart message="Sem dados" />
            ) : (
              <ResponsiveContainer width="100%" height={260}>
                <BarChart data={chartData} margin={{ top: 8, right: 8, left: -16, bottom: 0 }}>
                  <XAxis
                    dataKey="status"
                    tick={{ fill: "currentColor", fontSize: 11 }}
                    stroke="currentColor"
                    opacity={0.5}
                    interval={0}
                    angle={-15}
                    textAnchor="end"
                    height={60}
                  />
                  <YAxis
                    tick={{ fill: "currentColor", fontSize: 12 }}
                    stroke="currentColor"
                    opacity={0.5}
                    allowDecimals={false}
                  />
                  <Tooltip
                    contentStyle={{
                      backgroundColor: "var(--card)",
                      border: "1px solid var(--border)",
                      borderRadius: "var(--radius)",
                      fontSize: 13,
                    }}
                    cursor={{ fill: "var(--muted)", opacity: 0.3 }}
                  />
                  <Bar dataKey="count" radius={[4, 4, 0, 0]}>
                    {chartData.map((d) => (
                      <Cell key={d.status} fill={d.fill} />
                    ))}
                  </Bar>
                </BarChart>
              </ResponsiveContainer>
            )}
          </CardContent>
        </Card>
      </div>

      <Card>
        <CardHeader className="flex flex-row items-center justify-between space-y-0">
          <div>
            <CardTitle>Lotes recentes</CardTitle>
            <CardDescription>Últimas {Math.min(5, items.length)} entradas</CardDescription>
          </div>
          <Link to="/batches" className="text-sm text-primary hover:underline inline-flex items-center gap-1">
            Ver todos <ArrowRight className="h-3 w-3" />
          </Link>
        </CardHeader>
        <CardContent>
          {isLoading ? (
            <div className="space-y-2">
              {Array.from({ length: 5 }).map((_, i) => <Skeleton key={i} className="h-10 w-full" />)}
            </div>
          ) : items.length === 0 ? (
            <p className="text-sm text-muted-foreground">Sem lotes ainda.</p>
          ) : (
            <ul className="divide-y divide-border/50">
              {items.slice(0, 5).map((b: Batch) => (
                <li key={b.id}>
                  <Link
                    to="/batches/$id"
                    params={{ id: b.id }}
                    className="flex items-center justify-between py-2.5 hover:bg-muted/30"
                  >
                    <div className="flex items-center gap-3">
                      <BatchStatusBadge status={b.status} />
                      <span className="font-medium">{b.strain}</span>
                    </div>
                    <span className="text-xs text-muted-foreground">{fmtDate(b.createdAt)}</span>
                  </Link>
                </li>
              ))}
            </ul>
          )}
        </CardContent>
      </Card>
    </div>
  );
}

function KpiCard({ title, value, icon }: { title: string; value: string | null; icon: React.ReactNode }) {
  return (
    <Card>
      <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
        <CardTitle className="text-sm font-medium text-muted-foreground">{title}</CardTitle>
        {icon}
      </CardHeader>
      <CardContent>
        {value === null ? <Skeleton className="h-8 w-16" /> : <div className="text-2xl font-bold">{value}</div>}
      </CardContent>
    </Card>
  );
}

function EmptyChart({ message }: { message: string }) {
  return (
    <div className="flex h-64 items-center justify-center text-sm text-muted-foreground">
      {message}
    </div>
  );
}
