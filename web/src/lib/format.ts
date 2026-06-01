/** Helpers de formatação consistentes em PT-BR. */

const DATE_FMT = new Intl.DateTimeFormat("pt-BR", { dateStyle: "short" });
const DATETIME_FMT = new Intl.DateTimeFormat("pt-BR", { dateStyle: "short", timeStyle: "short" });
const NUMBER_FMT = new Intl.NumberFormat("pt-BR", { maximumFractionDigits: 2 });

export const fmtDate = (iso: string | null | undefined): string =>
  iso ? DATE_FMT.format(new Date(iso)) : "—";

export const fmtDateTime = (iso: string | null | undefined): string =>
  iso ? DATETIME_FMT.format(new Date(iso)) : "—";

export const fmtNumber = (n: number | null | undefined): string =>
  n == null ? "—" : NUMBER_FMT.format(n);

export const fmtTemperature = (n: number | null | undefined): string =>
  n == null ? "—" : `${NUMBER_FMT.format(n)}°C`;

export const fmtPercent = (n: number | null | undefined): string =>
  n == null ? "—" : `${NUMBER_FMT.format(n)}%`;
