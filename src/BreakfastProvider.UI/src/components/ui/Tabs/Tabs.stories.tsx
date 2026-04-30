import type { Meta, StoryObj } from '@storybook/react';
import { useState } from 'react';
import { Tabs } from './Tabs';

const meta: Meta<typeof Tabs> = {
  title: 'UI/Tabs',
  component: Tabs,
  tags: ['autodocs'],
  decorators: [(Story) => <div style={{ maxWidth: 500 }}><Story /></div>],
};

export default meta;
type Story = StoryObj<typeof meta>;

const tabs = [
  { id: 'pancakes', label: 'Pancakes', content: <p>Fluffy buttermilk pancakes with maple syrup.</p> },
  { id: 'waffles', label: 'Waffles', content: <p>Crispy Belgian waffles with fresh berries.</p> },
  { id: 'french-toast', label: 'French Toast', content: <p>Thick-cut brioche French toast.</p> },
];

export const Default: Story = {
  args: { tabs, activeTab: 'pancakes', onTabChange: () => {} },
};

export const SecondTabActive: Story = {
  args: { tabs, activeTab: 'waffles', onTabChange: () => {} },
};