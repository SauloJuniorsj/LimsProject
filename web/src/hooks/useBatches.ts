import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { api } from "@/lib/api";
import type {
  Batch,
  BatchListParams,
  BatchStatusHistory,
  BatchStatusValue,
  CertificateOfAnalysis,
  LabAnalysis,
  PagedResult,
} from "@/types/api";

function buildQuery(params: BatchListParams): string {
  const sp = new URLSearchParams();
  Object.entries(params).forEach(([k, v]) => {
    if (v !== undefined && v !== null && v !== "") sp.set(k, String(v));
  });
  const qs = sp.toString();
  return qs ? `?${qs}` : "";
}

export function useBatchesList(params: BatchListParams) {
  return useQuery({
    queryKey: ["batches", params],
    queryFn: () => api<PagedResult<Batch>>(`/batches${buildQuery(params)}`),
  });
}

export function useBatchSummary(id: string, opts?: { refetchInterval?: number | false }) {
  return useQuery({
    queryKey: ["batch", id, "summary"],
    queryFn: () => api<Batch>(`/batches/${id}/summary`),
    enabled: !!id,
    refetchInterval: opts?.refetchInterval,
  });
}

export function useBatchAnalyses(id: string) {
  return useQuery({
    queryKey: ["batch", id, "analyses"],
    queryFn: () => api<LabAnalysis[]>(`/batches/${id}/analyses`),
    enabled: !!id,
  });
}

export function useBatchStatusHistory(id: string) {
  return useQuery({
    queryKey: ["batch", id, "status-history"],
    queryFn: () => api<BatchStatusHistory[]>(`/batches/${id}/status-history`),
    enabled: !!id,
  });
}

export function useBatchCoA(id: string) {
  return useQuery({
    queryKey: ["batch", id, "coa"],
    queryFn: () => api<CertificateOfAnalysis>(`/batches/${id}/certificate-of-analysis`),
    enabled: !!id,
  });
}

export function useCreateBatch() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: { strain: string }) =>
      api<Batch>("/batches", { method: "POST", body: input }),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["batches"] }),
  });
}

export function useUpdateBatchStatus(id: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: { status: BatchStatusValue; reason?: string }) =>
      api<Batch>(`/batches/${id}/status`, { method: "PATCH", body: input }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["batches"] });
      qc.invalidateQueries({ queryKey: ["batch", id] });
    },
  });
}

export function useDeleteBatch() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => api<void>(`/batches/${id}`, { method: "DELETE" }),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["batches"] }),
  });
}

export function useCreateAnalysis(batchId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: { thc: number; cbd: number; terpenes: string; isPassed: boolean }) =>
      api<LabAnalysis>(`/batches/${batchId}/analysis`, { method: "POST", body: input }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["batch", batchId] });
      qc.invalidateQueries({ queryKey: ["batches"] });
    },
  });
}
