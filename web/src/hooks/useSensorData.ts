import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { api } from "@/lib/api";
import type { BatchDailySummary, PagedResult, SensorReading } from "@/types/api";

export function useSensorData(batchId: string, page = 1, pageSize = 50) {
  return useQuery({
    queryKey: ["batch", batchId, "sensor-data", page, pageSize],
    queryFn: () =>
      api<PagedResult<SensorReading>>(
        `/batches/${batchId}/sensor-data?page=${page}&pageSize=${pageSize}`,
      ),
    enabled: !!batchId,
  });
}

export function useDailySummaries(batchId: string) {
  return useQuery({
    queryKey: ["batch", batchId, "daily-summaries"],
    queryFn: () => api<BatchDailySummary[]>(`/batches/${batchId}/daily-summaries`),
    enabled: !!batchId,
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
