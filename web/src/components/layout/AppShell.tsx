import { Link, Outlet, useRouter } from "@tanstack/react-router";
import { LayoutDashboard, LogOut, Menu, Moon, Sprout, Sun, Users, X } from "lucide-react";
import { useState } from "react";
import { useAuth } from "@/hooks/useAuth";
import { useTheme } from "@/hooks/useTheme";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";

const navItems = [
  { to: "/", label: "Dashboard", icon: LayoutDashboard },
  { to: "/batches", label: "Lotes", icon: Sprout },
  { to: "/users", label: "Usuários", icon: Users },
];

export function AppShell() {
  const { email, logout } = useAuth();
  const { theme, toggle } = useTheme();
  const router = useRouter();
  const [mobileOpen, setMobileOpen] = useState(false);
  const close = () => setMobileOpen(false);

  return (
    <div className="flex h-screen w-screen overflow-hidden bg-background text-foreground">
      {mobileOpen && (
        <div
          className="fixed inset-0 z-30 bg-black/50 md:hidden"
          onClick={close}
          aria-hidden="true"
        />
      )}

      <aside
        className={cn(
          "fixed inset-y-0 left-0 z-40 flex w-64 flex-col border-r border-border bg-card transition-transform md:relative md:translate-x-0",
          mobileOpen ? "translate-x-0" : "-translate-x-full md:translate-x-0",
        )}
      >
        <div className="flex h-14 items-center justify-between border-b border-border px-6">
          <div className="flex items-center gap-2">
            <Sprout className="h-5 w-5 text-emerald-500" />
            <span className="font-semibold">LIMS</span>
          </div>
          <Button variant="ghost" size="icon" className="md:hidden" onClick={close}>
            <X className="h-4 w-4" />
          </Button>
        </div>
        <nav className="flex-1 space-y-1 p-3">
          {navItems.map(({ to, label, icon: Icon }) => (
            <Link
              key={to}
              to={to}
              onClick={close}
              activeOptions={{ exact: to === "/" }}
              className={cn(
                "flex items-center gap-3 rounded-md px-3 py-2 text-sm text-muted-foreground transition-colors hover:bg-accent hover:text-accent-foreground",
              )}
              activeProps={{ className: "bg-accent text-accent-foreground" }}
            >
              <Icon className="h-4 w-4" />
              {label}
            </Link>
          ))}
        </nav>
        <div className="border-t border-border p-3 text-xs text-muted-foreground">
          <div className="mb-2 truncate" title={email ?? ""}>
            {email ?? "—"}
          </div>
          <div className="flex gap-1">
            <Button variant="ghost" size="icon" onClick={toggle} title="Alternar tema">
              {theme === "dark" ? <Sun className="h-4 w-4" /> : <Moon className="h-4 w-4" />}
            </Button>
            <Button
              variant="ghost"
              size="icon"
              title="Sair"
              onClick={async () => {
                await logout();
                router.navigate({ to: "/login" });
              }}
            >
              <LogOut className="h-4 w-4" />
            </Button>
          </div>
        </div>
      </aside>

      <main className="flex flex-1 flex-col overflow-y-auto">
        <div className="flex h-14 items-center border-b border-border px-4 md:hidden">
          <Button variant="ghost" size="icon" onClick={() => setMobileOpen(true)}>
            <Menu className="h-5 w-5" />
          </Button>
          <div className="ml-3 flex items-center gap-2">
            <Sprout className="h-5 w-5 text-emerald-500" />
            <span className="font-semibold">LIMS</span>
          </div>
        </div>
        <div className="flex-1">
          <Outlet />
        </div>
      </main>
    </div>
  );
}
