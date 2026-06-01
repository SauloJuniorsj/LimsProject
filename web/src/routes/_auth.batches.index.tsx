import { createFileRoute, Link } from "@tanstack/react-router";
import { useState } from "react";
import { toast } from "sonner";
import { useBatchesList, useCreateBatch } from "@/hooks/useBatches";
import {
  BatchStatus,
  BatchStatusLabel,
  type BatchListParams,
  type BatchStatusValue,
} from "@/types/api";
import { BatchStatusBadge } from "@/components/BatchStatusBadge";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Card, CardContent } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import { ApiError } from "@/lib/api";
import { fmtDate } from "@/lib/format";
import { ArrowDown, ArrowUp, ChevronLeft, ChevronRight, Plus, Sprout } from "lucide-react";

export const Route = createFileRoute("/_auth/batches/")({ component: BatchesList });

type SortBy = "createdAt" | "strain" | "status";
type SortDir = "asc" | "desc";

function BatchesList() {
  const [strain, setStrain] = useState("");
  const [status, setStatus] = useState<BatchStatusValue | "">("");
  const [sortBy, setSortBy] = useState<SortBy>("createdAt");
  const [sortDir, setSortDir] = useState<SortDir>("desc");
  const [page, setPage] = useState(1);
  const [newStrain, setNewStrain] = useState("");

  const params: BatchListParams = {
    page,
    pageSize: 20,
    strain: strain.trim() || undefined,
    status: status === "" ? undefined : status,
    sortBy,
    sortDir,
  };

  const { data, isLoading, isError, refetch } = useBatchesList(params);
  const create = useCreateBatch();

  const onCreate = async (e: React.FormEvent) => {
    e.preventDefault();
    const trimmed = newStrain.trim();
    if (!trimmed) return;
    try {
      await create.mutateAsync({ strain: trimmed });
      toast.success(`Lote "${trimmed}" criado`);
      setNewStrain("");
    } catch (err) {
      const msg = err instanceof ApiError ? (err.detail ?? "Falha ao criar lote") : "Erro inesperado";
      toast.error(msg);
    }
  };

  const onHeaderSort = (column: SortBy) => {
    if (sortBy === column) {
      setSortDir((d) => (d === "asc" ? "desc" : "asc"));
    } else {
      setSortBy(column);
      setSortDir("asc");
    }
    setPage(1);
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
                <option key={s} value={s}>
                  {BatchStatusLabel[s]}
                </option>
              ))}
            </select>
          </div>

          <div className="overflow-x-auto rounded-md border border-border">
            <table className="w-full text-sm">
              <thead className="border-b border-border bg-muted/40">
                <tr className="text-left">
                  <SortableTh column="strain" current={sortBy} dir={sortDir} onClick={onHeaderSort}>
                    Strain
                  </SortableTh>
                  <SortableTh column="status" current={sortBy} dir={sortDir} onClick={onHeaderSort}>
                    Status
                  </SortableTh>
                  <SortableTh column="createdAt" current={sortBy} dir={sortDir} onClick={onHeaderSort}>
                    Criado em
                  </SortableTh>
                  <th className="px-4 py-2 font-medium">Criado por</th>
                  <th className="px-4 py-2" />
                </tr>
              </thead>
              <tbody>
                {isLoading &&
                  Array.from({ length: 5 }).map((_, i) => (
                    <tr key={i} className="border-b border-border/50">
                      <td className="px-4 py-3"><Skeleton className="h-4 w-32" /></td>
                      <td className="px-4 py-3"><Skeleton className="h-5 w-20" /></td>
                      <td className="px-4 py-3"><Skeleton className="h-4 w-20" /></td>
                      <td className="px-4 py-3"><Skeleton className="h-4 w-32" /></td>
                      <td className="px-4 py-3"><Skeleton className="h-4 w-16" /></td>
                    </tr>
                  ))}
                {!isLoading && isError && (
                  <tr>
                    <td colSpan={5} className="px-4 py-8 text-center">
                      <p className="text-destructive">Falha ao carregar lotes.</p>
                      <Button variant="link" size="sm" onClick={() => refetch()}>
                        Tentar novamente
                      </Button>
                    </td>
                  </tr>
                )}
                {!isLoading && !isError && data?.items.length === 0 && (
                  <tr>
                    <td colSpan={5} className="px-4 py-12 text-center">
                      <Sprout className="mx-auto mb-2 h-8 w-8 text-muted-foreground/40" />
                      <p className="text-sm text-muted-foreground">
                        {strain || status !== ""
                          ? "Nenhum lote bate com os filtros."
                          : "Sem lotes ainda. Crie o primeiro acima."}
                      </p>
                    </td>
                  </tr>
                )}
                {data?.items.map((b) => (
                  <tr
                    key={b.id}
                    className="border-b border-border/50 last:border-0 hover:bg-muted/30"
                  >
                    <td className="px-4 py-3 font-medium">{b.strain}</td>
                    <td className="px-4 py-3">
                      <BatchStatusBadge status={b.status} />
                    </td>
                    <td className="px-4 py-3 text-muted-foreground">{fmtDate(b.createdAt)}</td>
                    <td
                      className="max-w-[200px] truncate px-4 py-3 text-xs text-muted-foreground"
                      title={b.createdBy ?? ""}
                    >
                      {b.createdBy ?? "—"}
                    </td>
                    <td className="px-4 py-3 text-right">
                      <Link
                        to="/batches/$id"
                        params={{ id: b.id }}
                        className="text-xs text-primary hover:underline"
                      >
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
                Página {data.page} de {data.totalPages} ({data.totalCount}{" "}
                {data.totalCount === 1 ? "lote" : "lotes"})
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

interface SortableThProps {
  column: SortBy;
  current: SortBy;
  dir: SortDir;
  onClick: (column: SortBy) => void;
  children: React.ReactNode;
}

function SortableTh({ column, current, dir, onClick, children }: SortableThProps) {
  const active = current === column;
  return (
    <th className="px-4 py-2 font-medium">
      <button
        type="button"
        onClick={() => onClick(column)}
        className="flex items-center gap-1 hover:text-foreground"
      >
        {children}
        {active && (dir === "asc" ? <ArrowUp className="h-3 w-3" /> : <ArrowDown className="h-3 w-3" />)}
      </button>
    </th>
  );
}
