import { test, expect } from '@playwright/test'
import { LoginPage } from './pages/LoginPage'
import { InventoryPage } from './pages/InventoryPage'

test.describe('Inventory flow', () => {
  test('admin can create a warehouse and switch tabs without freezing', async ({ page }) => {
    const loginPage = new LoginPage(page)

    await loginPage.goto()
    await loginPage.login('admin@demo.com', 'Admin123!')
    await expect(page).toHaveURL(/\/users/)

    const inventoryPage = new InventoryPage(page)
    await inventoryPage.goto()
    await expect(page).toHaveURL(/\/inventory/)

    await inventoryPage.warehousesTab.click()
    await inventoryPage.newWarehouseButton.click()

    const code = `E2E-WH-${Date.now()}`
    await inventoryPage.warehouseCodeInput.fill(code)
    await inventoryPage.warehouseNameInput.fill('Test Inventory Warehouse')
    await inventoryPage.warehouseAddressInput.fill('Calle Falsa 123')
    await inventoryPage.createButton.click()

    await expect(inventoryPage.warehouseRow(code)).toBeVisible()

    await inventoryPage.itemsTab.click()
    await expect(inventoryPage.itemsTab).toHaveAttribute('data-state', 'active')
    await expect(inventoryPage.warehousesTab).toHaveAttribute('data-state', 'inactive')
    await expect(inventoryPage.heading).toBeVisible()
  })
})
