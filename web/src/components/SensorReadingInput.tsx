import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { toast } from "sonner";
import { Thermometer } from "lucide-react";
import { useRecordSensorReading } from "@/hooks/useSensorData";
import { ApiError } from "@/lib/api";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";

const schema = z.object({
  temperature: z
    .number({ message: "Informe um número" })
    .min(-10, "Temperatura abaixo de -10°C")
    .max(60, "Temperatura acima de 60°C"),
});
type FormData = z.infer<typeof schema>;

export function SensorReadingInput({ batchId }: { batchId: string }) {
  const record = useRecordSensorReading(batchId);
  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<FormData>({
    resolver: zodResolver(schema),
    defaultValues: { temperature: 22 },
  });

  const onSubmit = async (data: FormData) => {
    try {
      await record.mutateAsync(data);
      toast.success(`Leitura registrada: ${data.temperature}°C`);
      reset({ temperature: 22 });
    } catch (e) {
      const msg = e instanceof ApiError ? (e.detail ?? "Falha ao registrar") : "Erro inesperado";
      toast.error(msg);
    }
  };

  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center gap-2">
          <Thermometer className="h-4 w-4" /> Registrar leitura
        </CardTitle>
        <CardDescription>
          Atualiza temperatura atual do lote — entra no consolidado diário pelo RollupWorker
        </CardDescription>
      </CardHeader>
      <CardContent>
        <form onSubmit={handleSubmit(onSubmit)} className="flex items-end gap-2">
          <div className="flex-1 space-y-1">
            <Label htmlFor="temperature">Temperatura (°C)</Label>
            <Input
              id="temperature"
              type="number"
              step="0.1"
              min="-10"
              max="60"
              {...register("temperature", { valueAsNumber: true })}
              aria-invalid={!!errors.temperature}
            />
            {errors.temperature && (
              <p className="text-xs text-destructive">{errors.temperature.message}</p>
            )}
          </div>
          <Button type="submit" disabled={record.isPending}>
            {record.isPending ? "..." : "Registrar"}
          </Button>
        </form>
      </CardContent>
    </Card>
  );
}
