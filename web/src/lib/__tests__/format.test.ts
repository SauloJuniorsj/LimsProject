import { describe, expect, it } from "vitest";
import { fmtDate, fmtDateTime, fmtNumber, fmtPercent, fmtTemperature } from "../format";

describe("format helpers", () => {
  it("fmtDate retorna '—' pra null/undefined", () => {
    expect(fmtDate(null)).toBe("—");
    expect(fmtDate(undefined)).toBe("—");
  });

  it("fmtDate formata ISO em pt-BR", () => {
    expect(fmtDate("2026-06-01T12:00:00Z")).toMatch(/01\/06\/2026/);
  });

  it("fmtDateTime inclui horário", () => {
    const out = fmtDateTime("2026-06-01T12:30:00Z");
    expect(out).toContain("01/06/2026");
    expect(out).toMatch(/\d{2}:\d{2}/);
  });

  it("fmtNumber respeita locale pt-BR (vírgula decimal)", () => {
    expect(fmtNumber(1234.56)).toMatch(/1\.234,56|1234,56/);
  });

  it("fmtTemperature sufixa °C", () => {
    expect(fmtTemperature(22.5)).toContain("°C");
    expect(fmtTemperature(null)).toBe("—");
  });

  it("fmtPercent sufixa %", () => {
    expect(fmtPercent(85)).toContain("%");
    expect(fmtPercent(null)).toBe("—");
  });
});
