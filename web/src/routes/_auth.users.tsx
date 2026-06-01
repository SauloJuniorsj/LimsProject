import { createFileRoute } from "@tanstack/react-router";
import { useMutation } from "@tanstack/react-query";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { toast } from "sonner";
import { UserPlus } from "lucide-react";
import { api, ApiError } from "@/lib/api";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Button } from "@/components/ui/button";

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
  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<FormData>({
    resolver: zodResolver(schema),
    defaultValues: { role: "Lab" },
  });

  const create = useMutation({
    mutationFn: (input: FormData) =>
      api("/auth/register", { method: "POST", body: input }),
  });

  const onSubmit = async (data: FormData) => {
    try {
      await create.mutateAsync(data);
      toast.success(`Usuário ${data.email} criado como ${data.role}`);
      reset({ email: "", password: "", role: data.role });
    } catch (e) {
      const msg = e instanceof ApiError ? (e.detail ?? "Falha ao criar usuário") : "Erro inesperado";
      toast.error(msg);
    }
  };

  return (
    <div className="space-y-6 p-8">
      <div>
        <h1 className="text-3xl font-bold tracking-tight">Usuários</h1>
        <p className="text-sm text-muted-foreground">
          Cadastre operadores do laboratório e administradores
        </p>
      </div>

      <Card className="max-w-2xl">
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <UserPlus className="h-4 w-4" /> Novo usuário
          </CardTitle>
          <CardDescription>
            Roles disponíveis: <strong>Lab</strong> (registra leituras e análises) ou{" "}
            <strong>Admin</strong> (gerencia lotes e usuários)
          </CardDescription>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
            <div className="space-y-2">
              <Label htmlFor="email">Email</Label>
              <Input
                id="email"
                type="email"
                placeholder="operador@empresa.com"
                {...register("email")}
                aria-invalid={!!errors.email}
              />
              {errors.email && <p className="text-xs text-destructive">{errors.email.message}</p>}
            </div>

            <div className="space-y-2">
              <Label htmlFor="password">Senha temporária</Label>
              <Input
                id="password"
                type="password"
                placeholder="Mínimo 8 caracteres + 1 número"
                {...register("password")}
                aria-invalid={!!errors.password}
              />
              {errors.password && (
                <p className="text-xs text-destructive">{errors.password.message}</p>
              )}
            </div>

            <div className="space-y-2">
              <Label htmlFor="role">Role</Label>
              <select
                id="role"
                {...register("role")}
                className="flex h-9 w-full rounded-md border border-input bg-background px-3 text-sm"
              >
                <option value="Lab">Lab — operadores de laboratório</option>
                <option value="Admin">Admin — gestão de lotes e usuários</option>
              </select>
            </div>

            <Button type="submit" disabled={create.isPending}>
              {create.isPending ? "Criando..." : "Criar usuário"}
            </Button>
          </form>
        </CardContent>
      </Card>

      <Card className="max-w-2xl border-amber-500/30 bg-amber-500/5">
        <CardContent className="pt-6 text-sm text-muted-foreground">
          <p>
            <strong>Nota:</strong> Listar/editar/excluir usuários requer endpoints adicionais no
            backend (não implementados na v1.0.0). Esta tela hoje só cadastra novos via{" "}
            <code className="rounded bg-muted px-1 py-0.5 text-xs">/auth/register</code>.
          </p>
        </CardContent>
      </Card>
    </div>
  );
}
