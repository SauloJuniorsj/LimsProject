import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { api } from "@/lib/api";
import type { PagedResult } from "@/types/api";

export interface UserListItem {
  id: string;
  email: string;
  userName: string;
  roles: string[];
}

export function useUsersList(params: { page?: number; pageSize?: number; email?: string }) {
  const sp = new URLSearchParams();
  if (params.page) sp.set("page", String(params.page));
  if (params.pageSize) sp.set("pageSize", String(params.pageSize));
  if (params.email) sp.set("email", params.email);
  const qs = sp.toString();
  return useQuery({
    queryKey: ["users", params],
    queryFn: () => api<PagedResult<UserListItem>>(`/users${qs ? `?${qs}` : ""}`),
  });
}

export function useDeleteUser() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => api<void>(`/users/${id}`, { method: "DELETE" }),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["users"] }),
  });
}

export function useUpdateUserRole() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, role }: { id: string; role: "Lab" | "Admin" }) =>
      api<void>(`/users/${id}/role`, { method: "PUT", body: { role } }),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["users"] }),
  });
}

export function useRegisterUser() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (input: { email: string; password: string; role: "Lab" | "Admin" }) =>
      api("/auth/register", { method: "POST", body: input }),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["users"] }),
  });
}
