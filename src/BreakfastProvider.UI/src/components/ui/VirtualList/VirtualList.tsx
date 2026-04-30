'use client';

import { useRef, type ReactNode } from 'react';
import { useVirtualizer } from '@tanstack/react-virtual';
import * as styles from './VirtualList.css';

/**
 * Virtual scrolling list for large datasets.
 *
 * Learning point: TanStack Virtual only renders items visible in the
 * viewport (plus overscan). A list of 10,000 items renders ~20 DOM nodes.
 * This is essential for the infinite-scroll audit log view.
 */
interface VirtualListProps<T> {
  items: T[];
  /** Estimated height of each item (px) */
  estimateSize: number;
  /** Visible viewport height (px) */
  height: number;
  /** Render function for each item */
  renderItem: (item: T, index: number) => ReactNode;
  /** Number of extra items to render outside viewport (default: 5) */
  overscan?: number;
}

export function VirtualList<T>({
  items,
  estimateSize,
  height,
  renderItem,
  overscan = 5,
}: VirtualListProps<T>) {
  const parentRef = useRef<HTMLDivElement>(null);

  const virtualizer = useVirtualizer({
    count: items.length,
    getScrollElement: () => parentRef.current,
    estimateSize: () => estimateSize,
    overscan,
  });

  return (
    <div ref={parentRef} className={styles.container} style={{ height, overflow: 'auto' }}>
      <div style={{ height: `${virtualizer.getTotalSize()}px`, position: 'relative' }}>
        {virtualizer.getVirtualItems().map((virtualItem) => (
          <div
            key={virtualItem.key}
            className={styles.item}
            style={{
              position: 'absolute',
              top: 0,
              left: 0,
              width: '100%',
              transform: `translateY(${virtualItem.start}px)`,
            }}
          >
            {renderItem(items[virtualItem.index], virtualItem.index)}
          </div>
        ))}
      </div>
    </div>
  );
}
