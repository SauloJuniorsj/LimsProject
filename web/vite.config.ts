import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import tailwindcss from "@tailwindcss/vite";
import { TanStackRouterVite } from "@tanstack/router-plugin/vite";
import path from "node:path";

export default defineConfig({
  plugins: [
    TanStackRouterVite({ target: "react", autoCodeSplitting: true }),
    react(),
    tailwindcss(),
  ],
  resolve: {
    alias: {
      "@": path.resolve(__dirname, "./src"),
    },
  },
  server: {
    port: 5173,
    proxy: {
      // API .NET roda em http://localhost:8080 (docker-compose ou dotnet run)
      "/auth":     { target: "http://localhost:8080", changeOrigin: true },
      "/batches":  { target: "http://localhost:8080", changeOrigin: true },
      "/health":   { target: "http://localhost:8080", changeOrigin: true },
      "/swagger":  { target: "http://localhost:8080", changeOrigin: true },
      "/debug":    { target: "http://localhost:8080", changeOrigin: true },
    },
  },
});
