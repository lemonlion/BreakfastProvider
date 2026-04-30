import type { Meta, StoryObj } from '@storybook/react';
import { ErrorBoundary } from './ErrorBoundary';

/** Component that throws on render */
function BuggyComponent(): React.ReactNode {
  throw new Error('Test error: Something broke!');
}

/** Component that renders normally */
function GoodComponent() {
  return <p>This component renders fine.</p>;
}

const meta: Meta<typeof ErrorBoundary> = {
  title: 'UI/ErrorBoundary',
  component: ErrorBoundary,
  tags: ['autodocs'],
  parameters: { layout: 'centered' },
};

export default meta;
type Story = StoryObj<typeof meta>;

/** Error boundary catching a thrown error */
export const WithError: Story = {
  args: {
    children: <BuggyComponent />,
  },
};

/** Error boundary with custom fallback */
export const WithCustomFallback: Story = {
  args: {
    children: <BuggyComponent />,
    fallback: (
      <div style={{ padding: 24, textAlign: 'center', color: '#DC2626' }}>
        <h3>Custom Fallback UI</h3>
        <p>Something went wrong. Please contact support.</p>
      </div>
    ),
  },
};

/** No error — children render normally */
export const NoError: Story = {
  args: {
    children: <GoodComponent />,
  },
};
