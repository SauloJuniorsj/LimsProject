import { createFileRoute } from "@tanstack/react-router";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { toast } from "sonner";
import { Shield, ShieldCheck, Trash2, UserPlus } from "lucide-react";
import { useState } from "react";
import {
  useDeleteUser,
  useRegisterUser,
  useUpdateUserRole,
  useUsersList,
  type UserListItem,
} from "@/hooks/useUsers";
import { useAuth } from "@/hooks/useAuth";
import { ApiError } from "@/lib/api";
import { cn } from "@/lib/utils";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import { ConfirmDialog } from "@/components/ui/confirm-dialog";

export const Route = createFileRoute("/_auth/users")({ component: UsersAdmin });

const schema = z.object({
  email: z.string().email("Email inválido"),
  password: z
    .string()
    .min(8, "Mínimo 8 caracteres")
    .regex(/[0-9]/, "Precisa de pelo menos um número"),
  role: z.enum(["Lab", "Admin"]),
});
type FormData = z.infer<typeof schema>;

function UsersAdmin() {
  const { email: currentEmail } = useAuth();
  const [emailFilter, setEmailFilter] = useState("");
  const [page, setPage] = useState(1);
  const [confirmDelete, setConfirmDelete] = useState<UserListItem | null>(null);

  const list = useUsersList({ page, pageSize: 20, email: emailFilter.trim() || undefined });
  const register = useRegisterUser();
  const remove = useDeleteUser();
  const updateRole = useUpdateUserRole();

  const {
    register: rhf,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<FormData>({
    resolver: zodResolver(schema),
    defaultValues: { role: "Lab" },
  });

  const onCreate = async (data: FormData) => {
    try {
      await register.mutateAsync(data);
      toast.success(`Usuário ${data.email} criado como ${data.role}`);
      reset({ email: "", password: "", role: data.role });
    } catch (e) {
      const msg = e instanceof ApiError ? (e.detail ?? "Falha ao criar") : "Erro inesperado";
      toast.error(msg);
    }
  };

  const onChangeRole = async (user: UserListItem, role: "Lab" | "Admin") => {
    if (user.roles.includes(role)) return;
    try {
      await updateRole.mutateAsync({ id: user.id, role });
      toast.success(`Role de ${user.email} alterada pra ${role}`);
    } catch (e) {
      const msg = e instanceof ApiError ? (e.detail ?? "Falha ao alterar role") : "Erro inesperado";
      toast.error(msg);
    }
  };

  const onDelete = async () => {
    if (!confirmDelete) return;
    const target = confirmDelete;
    setConfirmDelete(null);
    try {
      await remove.mutateAsync(target.id);
      toast.success(`Usuário ${target.email} excluído`);
    } catch (e) {
      const msg = e instanceof ApiError ? (e.detail ?? "Falha ao excluir") : "Erro inesperado";
      toast.error(msg);
    }
  };

  return (
    <>
      <div className="space-y-6 p-8">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Usuários</h1>
          <p className="text-sm text-muted-foreground">
            Gerencie operadores do laboratório e administradores
          </p>
        </div>

        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <UserPlus className="h-4 w-4" /> Novo usuário
            </CardTitle>
            <CardDescription>
              <strong>Lab</strong>: registra leituras e análises. <strong>Admin</strong>: gerencia
              lotes e usuários.
            </CardDescription>
          </CardHeader>
          <CardContent>
            <form onSubmit={handleSubmit(onCreate)} className="grid grid-cols-1 gap-3 md:grid-cols-4">
              <div className="space-y-1">
                <Label htmlFor="email">Email</Label>
                <Input id="email" type="email" {...rhf("email")} aria-invalid={!!errors.email} />
                {errors.email && <p className="text-xs text-destructive">{errors.email.message}</p>}
              </div>
              <div className="space-y-1">
                <Label htmlFor="password">Senha</Label>
                <Input id="password" type="password" {...rhf("password")} aria-invalid={!!errors.password} />
                {errors.password && (
                  <p className="text-xs text-destructive">{errors.password.message}</p>
                )}
              </div>
              <div className="space-y-1">
                <Label htmlFor="role">Role</Label>
                <select
                  id="role"
                  {...rhf("role")}
                  className="flex h-9 w-full rounded-md border border-input bg-background px-3 text-sm"
                >
                  <option value="Lab">Lab</option>
                  <option value="Admin">Admin</option>
                </select>
              </div>
              <div className="flex items-end">
                <Button type="submit" disabled={register.isPending} className="w-full">
                  {register.isPending ? "Criando..." : "Criar"}
                </Button>
              </div>
            </form>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Usuários cadastrados</CardTitle>
            <CardDescription>{list.data?.totalCount ?? 0} no total</CardDescription>
          </CardHeader>
          <CardContent>
            <div className="mb-4">
              <Input
                placeholder="Filtrar por email..."
                value={emailFilter}
                onChange={(e) => {
                  setEmailFilter(e.target.value);
                  setPage(1);
                }}
                className="max-w-sm"
              />
            </div>

            <div className="overflow-x-auto rounded-md border border-border">
              <table className="w-full text-sm">
                <thead className="border-b border-border bg-muted/40">
                  <tr className="text-left">
                    <th className="px-4 py-2 font-medium">Email</th>
                    <th className="px-4 py-2 font-medium">Role</th>
                    <th className="px-4 py-2 text-right font-medium">Ações</th>
                  </tr>
                </thead>
                <tbody>
                  {list.isLoading &&
                    Array.from({ length: 5 }).map((_, i) => (
                      <tr key={i} className="border-b border-border/50">
                        <td className="px-4 py-3"><Skeleton className="h-4 w-48" /></td>
                        <td className="px-4 py-3"><Skeleton className="h-5 w-20" /></td>
                        <td className="px-4 py-3"><Skeleton className="ml-auto h-7 w-24" /></td>
                      </tr>
                    ))}
                  {list.data?.items.length === 0 && (
                    <tr>
                      <td colSpan={3} className="px-4 py-8 text-center text-sm text-muted-foreground">
                        Nenhum usuário encontrado.
                      </td>
                    </tr>
                  )}
                  {list.data?.items.map((u) => {
                    const isSelf = u.email === currentEmail;
                    const role = u.roles[0] ?? "—";
                    const isAdmin = u.roles.includes("Admin");
                    return (
                      <tr key={u.id} className="border-b border-border/50 last:border-0 hover:bg-muted/30">
                        <td className="px-4 py-3 font-medium">
                          {u.email}
                          {isSelf && (
                            <span className="ml-2 rounded bg-primary/15 px-1.5 py-0.5 text-[10px] uppercase tracking-wide text-primary">
                              você
                            </span>
                          )}
                        </td>
                        <td className="px-4 py-3">
                          <span
                            className={cn(
                              "inline-flex items-center gap-1 rounded-full px-2.5 py-0.5 text-xs font-medium",
                              isAdmin
                                ? "bg-purple-100 text-purple-800 dark:bg-purple-950 dark:text-purple-300"
                                : "bg-sky-100 text-sky-800 dark:bg-sky-950 dark:text-sky-300",
                            )}
                          >
                            {isAdmin ? <ShieldCheck className="h-3 w-3" /> : <Shield className="h-3 w-3" />}
                            {role}
                          </span>
                        </td>
                        <td className="px-4 py-3">
                          <div className="flex items-center justify-end gap-1">
                            <select
                              value={role === "Admin" ? "Admin" : "Lab"}
                              onChange={(e) => onChangeRole(u, e.target.value as "Lab" | "Admin")}
                              disabled={updateRole.isPending}
                              className="h-7 rounded-md border border-input bg-background px-2 text-xs"
                            >
                              <option value="Lab">Lab</option>
                              <option value="Admin">Admin</option>
                            </select>
                            <Button
                              variant="ghost"
                              size="icon"
                              className="h-7 w-7"
                              disabled={isSelf}
                              title={isSelf ? "Não pode excluir a si mesmo" : "Excluir"}
                              onClick={() => setConfirmDelete(u)}
                            >
                              <Trash2 className="h-3.5 w-3.5" />
                            </Button>
                          </div>
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>

            {list.data && list.data.totalPages > 1 && (
              <div className="mt-4 flex items-center justify-between text-sm text-muted-foreground">
                <div>
                  Página {list.data.page} de {list.data.totalPages}
                </div>
                <div className="flex gap-1">
                  <Button variant="outline" size="sm" disabled={page <= 1} onClick={() => setPage((p) => p - 1)}>
                    Anterior
                  </Button>
                  <Button
                    variant="outline"
                    size="sm"
                    disabled={page >= list.data.totalPages}
                    onClick={() => setPage((p) => p + 1)}
                  >
                    Próxima
                  </Button>
                </div>
              </div>
            )}
          </CardContent>
        </Card>
      </div>

      <ConfirmDialog
        open={!!confirmDelete}
        title="Excluir usuário?"
        description={
          confirmDelete
            ? `O usuário "${confirmDelete.email}" perderá o acesso ao sistema. Esta ação é irreversível.`
            : ""
        }
        confirmLabel="Excluir"
        destructive
        onConfirm={onDelete}
        onCancel={() => setConfirmDelete(null)}
      />
    </>
  );
}
