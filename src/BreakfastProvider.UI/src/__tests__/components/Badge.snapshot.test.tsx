import { render } from '@/test-utils/render';
import { Badge } from '@/components/ui/Badge/Badge';

describe('Badge snapshots', () => {
  it.each(['success', 'warning', 'error', 'info', 'neutral'] as const)(
    'should match snapshot for %s variant',
    (variant) => {
      const { container } = render(
        <Badge variant={variant}>Status Text</Badge>,
      );
      expect(container.firstChild).toMatchSnapshot();
    },
  );

  it('should match snapshot with dot indicator', () => {
    const { container } = render(
      <Badge variant="success" dot>Online</Badge>,
    );
    expect(container.firstChild).toMatchSnapshot();
  });
});
