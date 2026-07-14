import { useEffect, useRef, useState } from "react";
import { Radio } from "lucide-react";
import { toast } from "sonner";
import { useSimulateSensor } from "@/hooks/useSensorData";
import { Button } from "@/components/ui/button";
import { ApiError } from "@/lib/api";

/**
 * Dispara um burst de leituras sintéticas no backend e mantém uma contagem regressiva
 * local pra sinalizar "ao vivo" pros componentes irmãos (chart, tabela, resumo) via
 * onActiveChange — eles reagem ligando refetchInterval enquanto isso.
 */
export function SensorSimulator({
  batchId,
  onActiveChange,
}: {
  batchId: string;
  onActiveChange: (active: boolean) => void;
}) {
  const simulate = useSimulateSensor(batchId);
  const [remaining, setRemaining] = useState(0);
  const timerRef = useRef<ReturnType<typeof setInterval> | null>(null);

  useEffect(() => {
    return () => {
      if (timerRef.current) clearInterval(timerRef.current);
    };
  }, []);

  const onClick = async () => {
    try {
      const res = await simulate.mutateAsync();
      onActiveChange(true);
      setRemaining(res.durationSeconds);
      toast.success("Simulação iniciada — observe o gráfico e a tabela abaixo");

      if (timerRef.current) clearInterval(timerRef.current);
      timerRef.current = setInterval(() => {
        setRemaining((s) => {
          if (s <= 1) {
            if (timerRef.current) clearInterval(timerRef.current);
            onActiveChange(false);
            return 0;
          }
          return s - 1;
        });
      }, 1000);
    } catch (e) {
      const msg = e instanceof ApiError ? (e.detail ?? "Falha ao iniciar simulação") : "Erro inesperado";
      toast.error(msg);
    }
  };

  const active = remaining > 0;

  return (
    <Button
      type="button"
      variant={active ? "default" : "outline"}
      onClick={onClick}
      disabled={active || simulate.isPending}
    >
      <Radio className={active ? "h-4 w-4 animate-pulse" : "h-4 w-4"} />
      {active ? `Simulando sensor... ${remaining}s` : "▶ Simular sensor (60s)"}
    </Button>
  );
}
