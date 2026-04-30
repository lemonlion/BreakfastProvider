import { sprinkles, type Sprinkles } from '@/styles/sprinkles.css';
import { type ReactNode } from 'react';

interface ContainerProps {
  children: ReactNode;
  /** Responsive padding — demonstrates sprinkles responsive conditions */
  padding?: Sprinkles['padding'];
  maxWidth?: Sprinkles['maxWidth'];
}

/**
 * Max-width content wrapper using sprinkles for responsive padding.
 *
 * Learning point: sprinkles({ padding: { mobile: '4', desktop: '8' } })
 * generates atomic classes with media queries. The padding is 16px on
 * mobile and 32px on desktop, with no JS resize listener.
 */
export function Container({
  children,
  padding = { mobile: '4', desktop: '8' },
  maxWidth = '1280px',
}: ContainerProps) {
  return (
    <div className={sprinkles({ padding, maxWidth, marginX: 'auto' as never })}>
      {children}
    </div>
  );
}
