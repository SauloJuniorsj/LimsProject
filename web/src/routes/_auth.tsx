import { createFileRoute, redirect } from "@tanstack/react-router";
import { authStore } from "@/lib/auth";
import { AppShell } from "@/components/layout/AppShell";

export const Route = createFileRoute("/_auth")({
  beforeLoad: () => {
    if (!authStore.isAuthenticated()) {
      throw redirect({ to: "/login" });
    }
  },
  component: AppShell,
});
