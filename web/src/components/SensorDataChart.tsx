import {
  Area,
  CartesianGrid,
  ComposedChart,
  Legend,
  Line,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from "recharts";
import { useDailySummaries } from "@/hooks/useSensorData";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import { Thermometer } from "lucide-react";

export function SensorDataChart({ batchId, live }: { batchId: string; live?: boolean }) {
  const { data, isLoading } = useDailySummaries(batchId, { refetchInterval: live ? 4000 : false });

  // Transforma min/max em range pra plotar como Area + linha de média no centro
  const chartData = (data ?? []).slice().reverse().map((s) => ({
    date: new Date(s.date).toLocaleDateString("pt-BR", { day: "2-digit", month: "2-digit" }),
    avg: Number(s.avgTemperature.toFixed(2)),
    min: Number(s.minTemperature.toFixed(2)),
    max: Number(s.maxTemperature.toFixed(2)),
    range: [Number(s.minTemperature.toFixed(2)), Number(s.maxTemperature.toFixed(2))],
    count: s.readingCount,
  }));

  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center gap-2">
          <Thermometer className="h-4 w-4" /> Telemetria diária
          {live && (
            <span className="inline-flex items-center gap-1 rounded-full bg-emerald-500/15 px-2 py-0.5 text-xs font-medium text-emerald-600 dark:text-emerald-400">
              <span className="h-1.5 w-1.5 animate-pulse rounded-full bg-emerald-500" /> ao vivo
            </span>
          )}
        </CardTitle>
        <CardDescription>
          Consolidado por dia (min / média / máx) — atualizado pelo RollupWorker no backend
        </CardDescription>
      </CardHeader>
      <CardContent>
        {isLoading ? (
          <Skeleton className="h-64 w-full" />
        ) : chartData.length === 0 ? (
          <div className="flex h-64 items-center justify-center text-sm text-muted-foreground">
            Sem consolidações diárias ainda. O worker roda a cada 60s — registre leituras e aguarde.
          </div>
        ) : (
          <ResponsiveContainer width="100%" height={280}>
            <ComposedChart data={chartData} margin={{ top: 10, right: 16, left: 0, bottom: 0 }}>
              <CartesianGrid strokeDasharray="3 3" stroke="currentColor" opacity={0.1} />
              <XAxis dataKey="date" tick={{ fill: "currentColor", fontSize: 12 }} stroke="currentColor" opacity={0.5} />
              <YAxis
                tick={{ fill: "currentColor", fontSize: 12 }}
                stroke="currentColor"
                opacity={0.5}
                unit="°C"
              />
              <Tooltip
                contentStyle={{
                  backgroundColor: "var(--card)",
                  border: "1px solid var(--border)",
                  borderRadius: "var(--radius)",
                  fontSize: 13,
                }}
                labelStyle={{ color: "var(--foreground)" }}
                formatter={(value, name) => {
                  if (name === "range" && Array.isArray(value)) {
                    return [`${value[0]} – ${value[1]}°C`, "Min–Máx"];
                  }
                  return [`${value}°C`, name === "avg" ? "Média" : String(name)];
                }}
              />
              <Legend
                wrapperStyle={{ fontSize: 12 }}
                formatter={(v) => (v === "range" ? "Min–Máx" : v === "avg" ? "Média" : v)}
              />
              <Area
                type="monotone"
                dataKey="range"
                fill="oklch(0.7 0.18 145)"
                fillOpacity={0.15}
                stroke="oklch(0.7 0.18 145)"
                strokeOpacity={0.3}
                name="range"
              />
              <Line
                type="monotone"
                dataKey="avg"
                stroke="oklch(0.62 0.2 145)"
                strokeWidth={2}
                dot={{ r: 3 }}
                name="avg"
              />
            </ComposedChart>
          </ResponsiveContainer>
        )}
      </CardContent>
    </Card>
  );
}
