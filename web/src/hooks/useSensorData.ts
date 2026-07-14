import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { api } from "@/lib/api";
import type { BatchDailySummary, PagedResult, SensorReading } from "@/types/api";

interface LiveOptions {
  /** Intervalo de refetch em ms enquanto uma simulação está ativa; false desliga o polling. */
  refetchInterval?: number | false;
}

export function useSensorData(batchId: string, page = 1, pageSize = 50, opts?: LiveOptions) {
  return useQuery({
    queryKey: ["batch", batchId, "sensor-data", page, pageSize],
    queryFn: () =>
      api<PagedResult<SensorReading>>(
        `/batches/${batchId}/sensor-data?page=${page}&pageSize=${pageSize}`,
      ),
    enabled: !!batchId,
    refetchInterval: opts?.refetchInterval,
  });
}

export function useDailySummaries(batchId: string, opts?: LiveOptions) {
  return useQuery({
    queryKey: ["batch", batchId, "daily-summaries"],
    queryFn: () => api<BatchDailySummary[]>(`/batches/${batchId}/daily-summaries`),
    enabled: !!batchId,
    refetchInterval: opts?.refetchInterval,
  });
}

export function useRecordSensorReading(batchId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: { temperature: number }) =>
      api<SensorReading>(`/batches/${batchId}/sensor-data`, { method: "POST", body: input }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["batch", batchId] });
    },
  });
}

interface SimulationResponse {
  batchId: string;
  durationSeconds: number;
  message: string;
}

/** Dispara o burst de sensor simulado no backend — mesmo caminho do RollupWorker, só que sob demanda. */
export function useSimulateSensor(batchId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: () =>
      api<SimulationResponse>(`/batches/${batchId}/sensor-data/simulate`, { method: "POST" }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["batch", batchId] });
    },
  });
}
