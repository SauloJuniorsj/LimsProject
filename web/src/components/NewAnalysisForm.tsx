import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { AlertTriangle, FlaskConical } from "lucide-react";
import { toast } from "sonner";
import { useCreateAnalysis } from "@/hooks/useBatches";
import { ApiError } from "@/lib/api";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";

const HEMP_THC_LIMIT = 0.3;

const schema = z.object({
  thc: z
    .number({ message: "Informe um número" })
    .min(0, "THC não pode ser negativo")
    .max(35, "THC acima de 35% não é fisiologicamente plausível"),
  cbd: z.number({ message: "Informe um número" }).min(0, "CBD não pode ser negativo"),
  terpenes: z.string().min(1, "Descreva os terpenes"),
  isPassed: z.boolean(),
}).refine(
  (data) => !(data.thc > HEMP_THC_LIMIT && data.isPassed),
  {
    message: `Lote com THC acima de ${HEMP_THC_LIMIT}% não pode ser aprovado como cânhamo.`,
    path: ["isPassed"],
  },
);

type FormData = z.infer<typeof schema>;

export function NewAnalysisForm({ batchId }: { batchId: string }) {
  const create = useCreateAnalysis(batchId);

  const {
    register,
    handleSubmit,
    watch,
    setValue,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<FormData>({
    resolver: zodResolver(schema),
    defaultValues: { thc: 0, cbd: 0, terpenes: "", isPassed: false },
  });

  const thc = watch("thc");
  const isPassed = watch("isPassed");
  const overHempLimit = thc > HEMP_THC_LIMIT;

  // Se passar do limite enquanto isPassed=true, força pra rejeitado
  if (overHempLimit && isPassed) {
    setValue("isPassed", false, { shouldValidate: true });
  }

  const onSubmit = async (data: FormData) => {
    try {
      await create.mutateAsync(data);
      toast.success(
        data.isPassed ? "Análise aprovada — lote liberado" : "Análise registrada — lote rejeitado",
      );
      reset({ thc: 0, cbd: 0, terpenes: "", isPassed: false });
    } catch (e) {
      const msg = e instanceof ApiError ? (e.detail ?? "Falha ao registrar análise") : "Erro inesperado";
      toast.error(msg);
    }
  };

  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center gap-2">
          <FlaskConical className="h-4 w-4" /> Nova análise laboratorial
        </CardTitle>
        <CardDescription>
          Resultado da análise muda o status do lote pra <strong>Liberado</strong> ou <strong>Rejeitado</strong>.
        </CardDescription>
      </CardHeader>
      <CardContent>
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
          <div className="grid grid-cols-1 gap-4 md:grid-cols-3">
            <div className="space-y-2">
              <Label htmlFor="thc">THC (%)</Label>
              <Input
                id="thc"
                type="number"
                step="0.01"
                min="0"
                max="35"
                {...register("thc", { valueAsNumber: true })}
                aria-invalid={!!errors.thc}
              />
              {errors.thc && <p className="text-xs text-destructive">{errors.thc.message}</p>}
            </div>
            <div className="space-y-2">
              <Label htmlFor="cbd">CBD (%)</Label>
              <Input
                id="cbd"
                type="number"
                step="0.01"
                min="0"
                {...register("cbd", { valueAsNumber: true })}
                aria-invalid={!!errors.cbd}
              />
              {errors.cbd && <p className="text-xs text-destructive">{errors.cbd.message}</p>}
            </div>
            <div className="space-y-2">
              <Label htmlFor="terpenes">Terpenes</Label>
              <Input
                id="terpenes"
                placeholder="ex: myrcene, limonene"
                {...register("terpenes")}
                aria-invalid={!!errors.terpenes}
              />
              {errors.terpenes && (
                <p className="text-xs text-destructive">{errors.terpenes.message}</p>
              )}
            </div>
          </div>

          {/* Visualização da regra de compliance — feedback em tempo real */}
          {overHempLimit && (
            <div className="flex items-start gap-2 rounded-md border border-amber-500/50 bg-amber-500/10 p-3 text-sm">
              <AlertTriangle className="mt-0.5 h-4 w-4 flex-shrink-0 text-amber-500" />
              <div>
                <p className="font-medium text-amber-700 dark:text-amber-300">
                  Compliance: THC acima de {HEMP_THC_LIMIT}%
                </p>
                <p className="mt-1 text-xs text-muted-foreground">
                  Para classificação como cânhamo, THC deve ser ≤ {HEMP_THC_LIMIT}%. Esta análise
                  só pode ser registrada como <strong>Rejeitada</strong>.
                </p>
              </div>
            </div>
          )}

          <div className="flex items-center justify-between border-t border-border pt-4">
            <label className="flex items-center gap-2 text-sm">
              <input
                type="checkbox"
                {...register("isPassed")}
                disabled={overHempLimit}
                className="h-4 w-4 rounded border-input"
              />
              <span className={overHempLimit ? "text-muted-foreground" : ""}>
                Marcar como <strong>Aprovada</strong> (lote será liberado)
              </span>
            </label>

            <Button type="submit" disabled={isSubmitting || create.isPending}>
              {create.isPending ? "Registrando..." : "Registrar análise"}
            </Button>
          </div>
          {errors.isPassed && (
            <p className="text-xs text-destructive">{errors.isPassed.message}</p>
          )}
        </form>
      </CardContent>
    </Card>
  );
}
