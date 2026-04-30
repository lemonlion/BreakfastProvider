import { type Page, expect } from '@playwright/test';
import { BasePage } from './BasePage';

export class ReservationsPage extends BasePage {
  constructor(page: Page) {
    super(page);
  }

  async goto() {
    await this.page.goto('/reservations');
  }

  /** Fill the reservation form with provided data */
  async fillReservationForm(data: { name: string; date: string; partySize: number }) {
    await this.page.getByLabel(/name/i).fill(data.name);
    await this.page.getByLabel(/date/i).fill(data.date);
    await this.page.getByLabel(/party size/i).fill(String(data.partySize));
  }

  /** Cancel a reservation by its ID */
  async cancelReservation(id: string) {
    const row = this.page.locator('tr').filter({ hasText: id });
    await row.getByRole('button', { name: /cancel/i }).click();
  }
}
