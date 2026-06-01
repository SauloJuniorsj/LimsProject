import { describe, expect, it } from "vitest";
import { cn } from "../utils";

describe("cn", () => {
  it("concatena classes", () => {
    expect(cn("a", "b")).toBe("a b");
  });
  it("respeita condicionais (clsx)", () => {
    expect(cn("a", false && "b", "c")).toBe("a c");
  });
  it("mescla classes Tailwind conflitantes (tailwind-merge)", () => {
    // p-2 deve ser substituído por p-4 (mesma propriedade padding)
    expect(cn("p-2", "p-4")).toBe("p-4");
  });
});
