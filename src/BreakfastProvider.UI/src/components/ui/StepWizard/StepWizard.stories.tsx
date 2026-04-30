import type { Meta, StoryObj } from '@storybook/react';
import { userEvent, within, expect } from '@storybook/test';
import { StepWizard } from './StepWizard';

const steps = [
  { label: 'Select Items', content: <p>Choose your breakfast items from the menu.</p> },
  { label: 'Customise', content: <p>Add toppings and special instructions.</p> },
  { label: 'Review', content: <p>Review your order before placing it.</p> },
  { label: 'Confirm', content: <p>Your order has been placed!</p> },
];

const meta: Meta<typeof StepWizard> = {
  title: 'UI/StepWizard',
  component: StepWizard,
  tags: ['autodocs'],
  decorators: [(Story) => <div style={{ maxWidth: 600 }}><Story /></div>],
};

export default meta;
type Story = StoryObj<typeof meta>;

/** Default at first step */
export const Default: Story = {
  args: { steps },
};

/** At second step */
export const SecondStep: Story = {
  args: { steps },
};

/** Step navigation interaction test */
export const NavigationInteraction: Story = {
  args: { steps },
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    // Click Next to advance
    const nextButton = canvas.getByRole('button', { name: /next/i });
    await userEvent.click(nextButton);

    // Should now show step 2 content
    await expect(canvas.getByText('Add toppings and special instructions.')).toBeInTheDocument();

    // Click Next again
    await userEvent.click(nextButton);

    // Should now show step 3 content
    await expect(canvas.getByText('Review your order before placing it.')).toBeInTheDocument();
  },
};
