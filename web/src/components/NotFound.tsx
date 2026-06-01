import { Link } from "@tanstack/react-router";
import { FileQuestion } from "lucide-react";
import { Button } from "@/components/ui/button";

export function NotFound() {
  return (
    <div className="flex h-screen w-screen flex-col items-center justify-center bg-background p-6 text-center">
      <FileQuestion className="mb-4 h-12 w-12 text-muted-foreground/40" />
      <h1 className="text-2xl font-bold">Página não encontrada</h1>
      <p className="mt-2 max-w-md text-sm text-muted-foreground">
        A rota que você acessou não existe ou foi removida.
      </p>
      <Link to="/" className="mt-6">
        <Button>Ir pro Dashboard</Button>
      </Link>
    </div>
  );
}
