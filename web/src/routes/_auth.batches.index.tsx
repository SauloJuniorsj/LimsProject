import { createFileRoute, Link } from "@tanstack/react-router";
import { useState } from "react";
import { useBatchesList, useCreateBatch } from "@/hooks/useBatches";
import { BatchStatus, BatchStatusLabel, type BatchListParams, type BatchStatusValue } from "@/types/api";
import { BatchStatusBadge } from "@/components/BatchStatusBadge";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Card, CardContent } from "@/components/ui/card";
import { Plus, ChevronLeft, ChevronRight } from "lucide-react";

export const Route = createFileRoute("/_auth/batches/")({ component: BatchesList });

function BatchesList() {
  const [strain, setStrain] = useState("");
  const [status, setStatus] = useState<BatchStatusValue | "">("");
  const [page, setPage] = useState(1);
  const [newStrain, setNewStrain] = useState("");

  const params: BatchListParams = {
    page,
    pageSize: 20,
    strain: strain.trim() || undefined,
    status: status === "" ? undefined : status,
    sortBy: "createdAt",
    sortDir: "desc",
  };

  const { data, isLoading } = useBatchesList(params);
  const create = useCreateBatch();

  const onCreate = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!newStrain.trim()) return;
    await create.mutateAsync({ strain: newStrain.trim() });
    setNewStrain("");
  };

  return (
    <div className="space-y-6 p-8">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Lotes</h1>
          <p className="text-sm text-muted-foreground">Gerencie todos os lotes de cultivo</p>
        </div>
      </div>

      <Card>
        <CardContent className="pt-6">
          <form onSubmit={onCreate} className="flex gap-2">
            <Input
              placeholder="Nome da strain (ex: White Widow)"
              value={newStrain}
              onChange={(e) => setNewStrain(e.target.value)}
              className="max-w-sm"
            />
            <Button type="submit" disabled={create.isPending || !newStrain.trim()}>
              <Plus className="h-4 w-4" /> Criar lote
            </Button>
          </form>
          {create.error && (
            <p className="mt-2 text-xs text-destructive">
              {create.error.message ?? "Erro ao criar"}
            </p>
          )}
        </CardContent>
      </Card>

      <Card>
        <CardContent className="pt-6">
          <div className="mb-4 flex flex-wrap gap-3">
            <Input
              placeholder="Filtrar por strain..."
              value={strain}
              onChange={(e) => {
                setStrain(e.target.value);
                setPage(1);
              }}
              className="max-w-xs"
            />
            <select
              value={status === "" ? "" : String(status)}
              onChange={(e) => {
                setStatus(e.target.value === "" ? "" : (Number(e.target.value) as BatchStatusValue));
                setPage(1);
              }}
              className="h-9 rounded-md border border-input bg-background px-3 text-sm"
            >
              <option value="">Todos os status</option>
              {Object.values(BatchStatus).map((s) => (
                <option key={s} value={s}>{BatchStatusLabel[s]}</option>
              ))}
            </select>
          </div>

          <div className="overflow-x-auto rounded-md border border-border">
            <table className="w-full text-sm">
              <thead className="border-b border-border bg-muted/40">
                <tr className="text-left">
                  <th className="px-4 py-2 font-medium">Strain</th>
                  <th className="px-4 py-2 font-medium">Status</th>
                  <th className="px-4 py-2 font-medium">Criado em</th>
                  <th className="px-4 py-2 font-medium">Criado por</th>
                  <th className="px-4 py-2"></th>
                </tr>
              </thead>
              <tbody>
                {isLoading && (
                  <tr><td colSpan={5} className="px-4 py-8 text-center text-muted-foreground">Carregando...</td></tr>
                )}
                {data?.items.length === 0 && (
                  <tr><td colSpan={5} className="px-4 py-8 text-center text-muted-foreground">Nenhum lote encontrado.</td></tr>
                )}
                {data?.items.map((b) => (
                  <tr key={b.id} className="border-b border-border/50 last:border-0 hover:bg-muted/30">
                    <td className="px-4 py-3 font-medium">{b.strain}</td>
                    <td className="px-4 py-3"><BatchStatusBadge status={b.status} /></td>
                    <td className="px-4 py-3 text-muted-foreground">
                      {new Date(b.createdAt).toLocaleDateString("pt-BR")}
                    </td>
                    <td className="px-4 py-3 text-xs text-muted-foreground truncate max-w-[200px]" title={b.createdBy ?? ""}>
                      {b.createdBy ?? "—"}
                    </td>
                    <td className="px-4 py-3 text-right">
                      <Link to="/batches/$id" params={{ id: b.id }} className="text-xs text-primary hover:underline">
                        Ver detalhes →
                      </Link>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          {data && (
            <div className="mt-4 flex items-center justify-between text-sm text-muted-foreground">
              <div>
                Página {data.page} de {data.totalPages} ({data.totalCount} {data.totalCount === 1 ? "lote" : "lotes"})
              </div>
              <div className="flex gap-1">
                <Button
                  variant="outline"
                  size="icon"
                  disabled={page <= 1}
                  onClick={() => setPage((p) => p - 1)}
                >
                  <ChevronLeft className="h-4 w-4" />
                </Button>
                <Button
                  variant="outline"
                  size="icon"
                  disabled={page >= data.totalPages}
                  onClick={() => setPage((p) => p + 1)}
                >
                  <ChevronRight className="h-4 w-4" />
                </Button>
              </div>
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
