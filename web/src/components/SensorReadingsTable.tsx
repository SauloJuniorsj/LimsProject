import { useState } from "react";
import { ChevronLeft, ChevronRight, Activity } from "lucide-react";
import { useSensorData } from "@/hooks/useSensorData";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import { Button } from "@/components/ui/button";
import { fmtDateTime, fmtTemperature } from "@/lib/format";

export function SensorReadingsTable({ batchId }: { batchId: string }) {
  const [page, setPage] = useState(1);
  const pageSize = 10;
  const { data, isLoading } = useSensorData(batchId, page, pageSize);

  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center gap-2">
          <Activity className="h-4 w-4" /> Leituras de sensor (raw)
        </CardTitle>
        <CardDescription>
          {data
            ? `${data.totalCount} leitura(s) registradas — exibindo ${pageSize} por página`
            : "Carregando..."}
        </CardDescription>
      </CardHeader>
      <CardContent>
        <div className="overflow-x-auto rounded-md border border-border">
          <table className="w-full text-sm">
            <thead className="border-b border-border bg-muted/40">
              <tr className="text-left">
                <th className="px-4 py-2 font-medium">Timestamp</th>
                <th className="px-4 py-2 font-medium">Temperatura</th>
              </tr>
            </thead>
            <tbody>
              {isLoading &&
                Array.from({ length: 5 }).map((_, i) => (
                  <tr key={i} className="border-b border-border/50">
                    <td className="px-4 py-3"><Skeleton className="h-4 w-40" /></td>
                    <td className="px-4 py-3"><Skeleton className="h-4 w-16" /></td>
                  </tr>
                ))}
              {!isLoading && data?.items.length === 0 && (
                <tr>
                  <td colSpan={2} className="px-4 py-8 text-center text-sm text-muted-foreground">
                    Sem leituras registradas. Use o painel de registro acima.
                  </td>
                </tr>
              )}
              {data?.items.map((r) => (
                <tr key={r.id} className="border-b border-border/50 last:border-0">
                  <td className="px-4 py-2 font-mono text-xs text-muted-foreground">
                    {fmtDateTime(r.readingTime)}
                  </td>
                  <td className="px-4 py-2 font-medium">{fmtTemperature(r.temperature)}</td>
                </tr>
              ))}
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
