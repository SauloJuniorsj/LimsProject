import {
  Area,
  AreaChart,
  CartesianGrid,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from "recharts";
import { TrendingUp } from "lucide-react";
import { useBatchesList } from "@/hooks/useBatches";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";

/** Quantidade de dias na janela. */
const WINDOW_DAYS = 30;

export function ActivityChart() {
  const { data, isLoading } = useBatchesList({
    page: 1,
    pageSize: 100,
    sortBy: "createdAt",
    sortDir: "desc",
  });

  // Bucketiza por dia (00:00 UTC) os batches dos últimos WINDOW_DAYS
  const now = new Date();
  const start = new Date(now.getTime() - WINDOW_DAYS * 24 * 3600 * 1000);
  start.setHours(0, 0, 0, 0);

  const buckets = new Map<string, number>();
  for (let i = 0; i < WINDOW_DAYS; i++) {
    const d = new Date(start);
    d.setDate(d.getDate() + i);
    buckets.set(d.toISOString().slice(0, 10), 0);
  }

  (data?.items ?? []).forEach((b) => {
    const day = new Date(b.createdAt);
    if (day >= start) {
      const key = day.toISOString().slice(0, 10);
      buckets.set(key, (buckets.get(key) ?? 0) + 1);
    }
  });

  const chartData = Array.from(buckets.entries()).map(([date, count]) => ({
    date,
    label: new Date(date).toLocaleDateString("pt-BR", { day: "2-digit", month: "2-digit" }),
    count,
  }));

  const hasData = chartData.some((d) => d.count > 0);

  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center gap-2">
          <TrendingUp className="h-4 w-4" /> Atividade dos últimos {WINDOW_DAYS} dias
        </CardTitle>
        <CardDescription>Lotes criados por dia</CardDescription>
      </CardHeader>
      <CardContent>
        {isLoading ? (
          <Skeleton className="h-64 w-full" />
        ) : !hasData ? (
          <div className="flex h-64 items-center justify-center text-sm text-muted-foreground">
            Nenhuma atividade na janela. Crie alguns lotes pra ver a tendência.
          </div>
        ) : (
          <ResponsiveContainer width="100%" height={260}>
            <AreaChart data={chartData} margin={{ top: 10, right: 16, left: -16, bottom: 0 }}>
              <defs>
                <linearGradient id="activityGradient" x1="0" y1="0" x2="0" y2="1">
                  <stop offset="0%" stopColor="oklch(0.7 0.15 145)" stopOpacity={0.45} />
                  <stop offset="100%" stopColor="oklch(0.7 0.15 145)" stopOpacity={0.02} />
                </linearGradient>
              </defs>
              <CartesianGrid strokeDasharray="3 3" stroke="currentColor" opacity={0.08} />
              <XAxis
                dataKey="label"
                tick={{ fill: "currentColor", fontSize: 11 }}
                stroke="currentColor"
                opacity={0.5}
                interval="preserveStartEnd"
                minTickGap={28}
              />
              <YAxis
                tick={{ fill: "currentColor", fontSize: 12 }}
                stroke="currentColor"
                opacity={0.5}
                allowDecimals={false}
                width={36}
              />
              <Tooltip
                contentStyle={{
                  backgroundColor: "var(--card)",
                  border: "1px solid var(--border)",
                  borderRadius: "var(--radius)",
                  fontSize: 13,
                }}
                labelFormatter={(label) => `Dia ${label}`}
                formatter={(value) => [`${value} lote(s)`, "Criados"]}
              />
              <Area
                type="monotone"
                dataKey="count"
                stroke="oklch(0.62 0.2 145)"
                strokeWidth={2}
                fill="url(#activityGradient)"
              />
            </AreaChart>
          </ResponsiveContainer>
        )}
      </CardContent>
    </Card>
  );
}
