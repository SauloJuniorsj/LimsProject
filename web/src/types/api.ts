// Tipos espelhando os DTOs do backend C# (LimsProject.Domain.* e LimsProject.Application.Models.*)

export const BatchStatus = {
  Germination: 0,
  Growth: 1,
  Harvested: 2,
  Testing: 3,
  Released: 4,
  Rejected: 5,
} as const;
export type BatchStatusValue = (typeof BatchStatus)[keyof typeof BatchStatus];

export const BatchStatusLabel: Record<BatchStatusValue, string> = {
  0: "Germinação",
  1: "Crescimento",
  2: "Colhido",
  3: "Em análise",
  4: "Liberado",
  5: "Rejeitado",
};

export interface Batch {
  id: string;
  strain: string;
  status: BatchStatusValue;
  thcPercentage: number | null;
  cbdPercentage: number | null;
  hasContaminants: boolean;
  currentMoisture: number | null;
  currentTemperature: number | null;
  averageTemperature: number;
  createdAt: string;
  createdBy: string | null;
  updatedAt: string | null;
  updatedBy: string | null;
  deletedAt: string | null;
  deletedBy: string | null;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface LabAnalysis {
  id: string;
  batchId: string;
  thc: number;
  cbd: number;
  terpenes: string;
  analysisDate: string;
  isPassed: boolean;
}

export interface SensorReading {
  id: string;
  batchId: string;
  temperature: number;
  readingTime: string;
}

export interface BatchDailySummary {
  id: string;
  batchId: string;
  avgTemperature: number;
  minTemperature: number;
  maxTemperature: number;
  readingCount: number;
  date: string;
}

export interface BatchStatusHistory {
  id: string;
  batchId: string;
  fromStatus: BatchStatusValue | null;
  toStatus: BatchStatusValue;
  changedAt: string;
  changedBy: string;
  reason: string | null;
}

export interface CertificateOfAnalysis {
  batchId: string;
  strain: string;
  status: BatchStatusValue;
  batchCreatedAt: string;
  analyses: LabAnalysis[];
  environmental: {
    daysMonitored: number;
    overallAvgTemperature: number | null;
    overallMinTemperature: number | null;
    overallMaxTemperature: number | null;
    totalReadings: number;
  };
  lifecycle: BatchStatusHistory[];
  compliance: {
    hasPassingAnalysis: boolean;
    hempCompliant: boolean;
    analysisCount: number;
    lastAnalysisDate: string | null;
  };
  issuedAt: string;
}

export interface AuthTokens {
  accessToken: string;
  refreshToken: string;
  accessTokenExpiresAt: string;
  refreshTokenExpiresAt: string;
}

export interface BatchListParams {
  page?: number;
  pageSize?: number;
  strain?: string;
  status?: BatchStatusValue;
  sortBy?: "createdAt" | "strain" | "status";
  sortDir?: "asc" | "desc";
  createdAfter?: string;
  createdBefore?: string;
}
