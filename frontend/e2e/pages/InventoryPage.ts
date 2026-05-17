import type { Page, Locator } from "@playwright/test";

export class InventoryPage {
  readonly heading: Locator;
  readonly itemsTab: Locator;
  readonly warehousesTab: Locator;
  readonly table: Locator;
  readonly newWarehouseButton: Locator;
  readonly warehouseCodeInput: Locator;
  readonly warehouseNameInput: Locator;
  readonly warehouseAddressInput: Locator;
  readonly createButton: Locator;

  constructor(private readonly page: Page) {
    const pageMain = page.locator("main");
    const dialog = page.locator('[role="dialog"]');

    this.heading = pageMain.getByRole("heading", { name: /inventario/i });
    this.itemsTab = pageMain.getByRole("tab", { name: /existencias/i });
    this.warehousesTab = pageMain.getByRole("tab", { name: /almacenes/i });
    this.table = pageMain.getByRole("table");
    this.newWarehouseButton = pageMain.getByRole("button", {
      name: /nuevo almacén/i,
    });

    this.warehouseCodeInput = dialog.getByPlaceholder("CDMX-01");
    this.warehouseNameInput = dialog.getByPlaceholder(
      "Almacén Ciudad de México",
    );
    this.warehouseAddressInput = dialog.getByPlaceholder("Opcional");
    this.createButton = dialog.getByRole("button", { name: /crear/i });
  }

  async goto() {
    await this.page.goto("/inventory");
    await this.heading.waitFor({ state: "visible" });
  }

  async createWarehouse(code: string, name: string, address?: string) {
    await this.warehousesTab.click();
    await this.newWarehouseButton.click();
    await this.warehouseCodeInput.fill(code);
    await this.warehouseNameInput.fill(name);
    if (address) await this.warehouseAddressInput.fill(address);
    await this.createButton.click();
  }

  warehouseRow(code: string) {
    return this.page.locator("table tr", { hasText: code });
  }
}
