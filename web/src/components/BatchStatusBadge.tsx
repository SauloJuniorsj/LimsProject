import { cn } from "@/lib/utils";
import { BatchStatus, BatchStatusLabel, type BatchStatusValue } from "@/types/api";

const styles: Record<BatchStatusValue, string> = {
  [BatchStatus.Germination]: "bg-emerald-100 text-emerald-800 dark:bg-emerald-950 dark:text-emerald-300",
  [BatchStatus.Growth]:      "bg-green-100 text-green-800 dark:bg-green-950 dark:text-green-300",
  [BatchStatus.Harvested]:   "bg-amber-100 text-amber-800 dark:bg-amber-950 dark:text-amber-300",
  [BatchStatus.Testing]:     "bg-sky-100 text-sky-800 dark:bg-sky-950 dark:text-sky-300",
  [BatchStatus.Released]:    "bg-teal-100 text-teal-800 dark:bg-teal-950 dark:text-teal-300",
  [BatchStatus.Rejected]:    "bg-red-100 text-red-800 dark:bg-red-950 dark:text-red-300",
};

export function BatchStatusBadge({ status, className }: { status: BatchStatusValue; className?: string }) {
  return (
    <span
      className={cn(
        "inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium",
        styles[status],
        className,
      )}
    >
      {BatchStatusLabel[status]}
    </span>
  );
}
