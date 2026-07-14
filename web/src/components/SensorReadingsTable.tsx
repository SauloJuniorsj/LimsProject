import { useState } from "react";
import { Activity, AlertTriangle, ChevronLeft, ChevronRight } from "lucide-react";
import { useSensorData } from "@/hooks/useSensorData";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import { Button } from "@/components/ui/button";
import { fmtDateTime, fmtTemperature } from "@/lib/format";
import { cn } from "@/lib/utils";

// Espelha SensorThresholds.cs no backend — manter sincronizado!
const MIN_OK = 18;
const MAX_OK = 30;
const isOutOfRange = (t: number) => t < MIN_OK || t > MAX_OK;

export function SensorReadingsTable({ batchId, live }: { batchId: string; live?: boolean }) {
  const [page, setPage] = useState(1);
  const pageSize = 10;
  const { data, isLoading } = useSensorData(batchId, page, pageSize, {
    refetchInterval: live ? 3000 : false,
  });

  const outOfRangeCount = data?.items.filter((r) => isOutOfRange(r.temperature)).length ?? 0;

  return (
    <Card>
      <CardHeader>
        <div className="flex items-start justify-between">
          <div>
            <CardTitle className="flex items-center gap-2">
              <Activity className="h-4 w-4" /> Leituras de sensor (raw)
            </CardTitle>
            <CardDescription>
              {data
                ? `${data.totalCount} leitura(s) — faixa ideal ${MIN_OK}°C a ${MAX_OK}°C`
                : "Carregando..."}
            </CardDescription>
          </div>
          {outOfRangeCount > 0 && (
            <span className="inline-flex items-center gap-1 rounded-full bg-amber-500/15 px-2.5 py-1 text-xs font-medium text-amber-700 dark:text-amber-300">
              <AlertTriangle className="h-3 w-3" />
              {outOfRangeCount} fora da faixa
            </span>
          )}
        </div>
      </CardHeader>
      <CardContent>
        <div className="overflow-x-auto rounded-md border border-border">
          <table className="w-full text-sm">
            <thead className="border-b border-border bg-muted/40">
              <tr className="text-left">
                <th className="px-4 py-2 font-medium">Timestamp</th>
                <th className="px-4 py-2 font-medium">Temperatura</th>
                <th className="px-4 py-2 font-medium">Status</th>
              </tr>
            </thead>
            <tbody>
              {isLoading &&
                Array.from({ length: 5 }).map((_, i) => (
                  <tr key={i} className="border-b border-border/50">
                    <td className="px-4 py-3"><Skeleton className="h-4 w-40" /></td>
                    <td className="px-4 py-3"><Skeleton className="h-4 w-16" /></td>
                    <td className="px-4 py-3"><Skeleton className="h-4 w-20" /></td>
                  </tr>
                ))}
              {!isLoading && data?.items.length === 0 && (
                <tr>
                  <td colSpan={3} className="px-4 py-8 text-center text-sm text-muted-foreground">
                    Sem leituras registradas. Use o painel de registro acima.
                  </td>
                </tr>
              )}
              {data?.items.map((r) => {
                const ofr = isOutOfRange(r.temperature);
                return (
                  <tr
                    key={r.id}
                    className={cn(
                      "border-b border-border/50 last:border-0",
                      ofr && "bg-amber-500/5",
                    )}
                  >
                    <td className="px-4 py-2 font-mono text-xs text-muted-foreground">
                      {fmtDateTime(r.readingTime)}
                    </td>
                    <td className={cn("px-4 py-2 font-medium", ofr && "text-amber-700 dark:text-amber-300")}>
                      {fmtTemperature(r.temperature)}
                    </td>
                    <td className="px-4 py-2">
                      {ofr ? (
                        <span className="inline-flex items-center gap-1 text-xs text-amber-700 dark:text-amber-300">
                          <AlertTriangle className="h-3 w-3" /> Fora da faixa
                        </span>
                      ) : (
                        <span className="text-xs text-emerald-600 dark:text-emerald-400">OK</span>
                      )}
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>

        {data && data.totalPages > 1 && (
          <div className="mt-3 flex items-center justify-between text-xs text-muted-foreground">
            <div>
              Página {data.page} de {data.totalPages}
            </div>
            <div className="flex gap-1">
              <Button
                variant="outline"
                size="icon"
                className="h-7 w-7"
                disabled={page <= 1}
                onClick={() => setPage((p) => p - 1)}
              >
                <ChevronLeft className="h-3 w-3" />
              </Button>
              <Button
                variant="outline"
                size="icon"
                className="h-7 w-7"
                disabled={page >= data.totalPages}
                onClick={() => setPage((p) => p + 1)}
              >
                <ChevronRight className="h-3 w-3" />
              </Button>
            </div>
          </div>
        )}
      </CardContent>
    </Card>
  );
}
