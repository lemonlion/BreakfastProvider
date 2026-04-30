import type { Meta, StoryObj } from '@storybook/react';
import { VirtualList } from './VirtualList';

/** Generate a large dataset */
function generateItems(count: number) {
  return Array.from({ length: count }, (_, i) => ({
    id: `item-${i}`,
    label: `Item #${i + 1} — Breakfast topping variant`,
  }));
}

const meta: Meta<typeof VirtualList> = {
  title: 'UI/VirtualList',
  component: VirtualList,
  tags: ['autodocs'],
  argTypes: {
    height: {
      control: { type: 'range', min: 200, max: 600, step: 50 },
      description: 'Height of the scrollable container in pixels',
    },
  },
  decorators: [(Story) => <div style={{ maxWidth: 500 }}><Story /></div>],
};

export default meta;
type Story = StoryObj<typeof meta>;

/** 10,000 items — demonstrates virtualisation performance */
export const TenThousandItems: Story = {
  args: {
    items: generateItems(10_000),
    height: 400,
    renderItem: (item: unknown) => (
      <div style={{ padding: '8px 12px', borderBottom: '1px solid #eee' }}>
        {(item as any).label}
      </div>
    ),
  },
};

/** Small list */
export const SmallList: Story = {
  args: {
    items: generateItems(50),
    height: 300,
    renderItem: (item: unknown) => (
      <div style={{ padding: '8px 12px', borderBottom: '1px solid #eee' }}>
        {(item as any).label}
      </div>
    ),
  },
};

/** Custom height */
export const CustomHeight: Story = {
  args: {
    items: generateItems(1_000),
    height: 200,
    renderItem: (item: unknown) => (
      <div style={{ padding: '8px 12px', borderBottom: '1px solid #eee' }}>
        {(item as any).label}
      </div>
    ),
  },
};
