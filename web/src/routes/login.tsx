import { createFileRoute, useNavigate } from "@tanstack/react-router";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { ShieldCheck, Sprout, FlaskConical } from "lucide-react";
import { useAuth } from "@/hooks/useAuth";
import { ApiError } from "@/lib/api";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { useState } from "react";

const schema = z.object({
  email: z.string().email("Email inválido"),
  password: z.string().min(8, "Senha precisa de no mínimo 8 caracteres"),
});
type FormData = z.infer<typeof schema>;

// Espelha StartupExtensions.DemoUsers no backend — mantenha sincronizado!
const DEMO_ACCOUNTS = [
  { label: "Admin", email: "admin@lims.demo", icon: ShieldCheck },
  { label: "Lab (técnico)", email: "lab@lims.demo", icon: FlaskConical },
] as const;
const DEMO_PASSWORD = "Demo1234";

export const Route = createFileRoute("/login")({ component: Login });

function Login() {
  const navigate = useNavigate();
  const { login } = useAuth();
  const [serverError, setServerError] = useState<string | null>(null);
  const [demoLoading, setDemoLoading] = useState<string | null>(null);
  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<FormData>({ resolver: zodResolver(schema) });

  const doLogin = async (email: string, password: string) => {
    setServerError(null);
    try {
      await login(email, password);
      navigate({ to: "/" });
    } catch (e) {
      setServerError(e instanceof ApiError ? (e.detail ?? "Falha no login") : "Falha no login");
    }
  };

  const onSubmit = (data: FormData) => doLogin(data.email, data.password);

  const onDemoClick = async (email: string) => {
    setDemoLoading(email);
    await doLogin(email, DEMO_PASSWORD);
    setDemoLoading(null);
  };

  return (
    <div className="flex min-h-screen items-center justify-center bg-background p-4">
      <Card className="w-full max-w-md">
        <CardHeader className="text-center">
          <div className="mb-2 flex items-center justify-center gap-2">
            <Sprout className="h-6 w-6 text-emerald-500" />
            <span className="text-xl font-semibold">LIMS</span>
          </div>
          <CardTitle>Entrar</CardTitle>
          <CardDescription>Acesse o sistema de gestão laboratorial</CardDescription>
        </CardHeader>
        <CardContent>
          <div className="mb-5 space-y-2 rounded-md border border-dashed border-border bg-muted/30 p-3">
            <p className="text-xs font-medium text-muted-foreground">
              Contas de demonstração — 1 clique, sem cadastro
            </p>
            <div className="grid grid-cols-2 gap-2">
              {DEMO_ACCOUNTS.map(({ label, email, icon: Icon }) => (
                <Button
                  key={email}
                  type="button"
                  variant="outline"
                  size="sm"
                  className="flex-col gap-1 h-auto py-2"
                  disabled={demoLoading !== null || isSubmitting}
                  onClick={() => onDemoClick(email)}
                >
                  <Icon className="h-4 w-4" />
                  {demoLoading === email ? "Entrando..." : label}
                </Button>
              ))}
            </div>
            <p className="font-mono text-[11px] text-muted-foreground">
              {DEMO_ACCOUNTS.map((a) => a.email).join(" · ")} — senha: {DEMO_PASSWORD}
            </p>
          </div>

          <div className="mb-4 flex items-center gap-2 text-xs text-muted-foreground">
            <div className="h-px flex-1 bg-border" />
            ou entre com email e senha
            <div className="h-px flex-1 bg-border" />
          </div>

          <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
            <div className="space-y-2">
              <Label htmlFor="email">Email</Label>
              <Input
                id="email"
                type="email"
                placeholder="voce@empresa.com"
                {...register("email")}
                aria-invalid={!!errors.email}
              />
              {errors.email && (
                <p className="text-xs text-destructive">{errors.email.message}</p>
              )}
            </div>
            <div className="space-y-2">
              <Label htmlFor="password">Senha</Label>
              <Input
                id="password"
                type="password"
                placeholder="••••••••"
                {...register("password")}
                aria-invalid={!!errors.password}
              />
              {errors.password && (
                <p className="text-xs text-destructive">{errors.password.message}</p>
              )}
            </div>
            {serverError && (
              <div className="rounded-md border border-destructive/50 bg-destructive/10 px-3 py-2 text-xs text-destructive">
                {serverError}
              </div>
            )}
            <Button type="submit" className="w-full" disabled={isSubmitting}>
              {isSubmitting ? "Entrando..." : "Entrar"}
            </Button>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}
