import type { Meta, StoryObj } from '@storybook/react';
import { fn, userEvent, within, expect } from '@storybook/test';
import { Pagination } from './Pagination';

const meta: Meta<typeof Pagination> = {
  title: 'UI/Pagination',
  component: Pagination,
  tags: ['autodocs'],
  argTypes: {
    totalPages: {
      control: { type: 'range', min: 1, max: 50, step: 1 },
      description: 'Total number of pages',
    },
    currentPage: {
      control: { type: 'number' },
      description: 'Currently active page (1-indexed)',
    },
  },
  args: {
    onPageChange: fn(),
  },
};

export default meta;
type Story = StoryObj<typeof meta>;

/** Default pagination */
export const Default: Story = {
  args: {
    totalPages: 10,
    currentPage: 1,
  },
};

/** Middle page active */
export const MiddlePage: Story = {
  args: {
    totalPages: 20,
    currentPage: 10,
  },
};

/** Last page */
export const LastPage: Story = {
  args: {
    totalPages: 5,
    currentPage: 5,
  },
};

/** Single page (no navigation needed) */
export const SinglePage: Story = {
  args: {
    totalPages: 1,
    currentPage: 1,
  },
};

/** Page click interaction test */
export const PageClickInteraction: Story = {
  args: {
    totalPages: 10,
    currentPage: 1,
    onPageChange: fn(),
  },
  play: async ({ canvasElement, args }) => {
    const canvas = within(canvasElement);

    // Click page 3
    const page3 = canvas.getByRole('button', { name: /3/ });
    await userEvent.click(page3);

    await expect(args.onPageChange).toHaveBeenCalledWith(3);
  },
};
