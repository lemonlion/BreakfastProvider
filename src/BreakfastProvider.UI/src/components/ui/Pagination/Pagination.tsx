'use client';

import * as styles from './Pagination.css';

interface PaginationProps {
  currentPage: number;
  totalPages: number;
  onPageChange: (page: number) => void;
  /** Max page buttons to show before adding ellipsis (default: 7) */
  siblingCount?: number;
}

/**
 * Page navigation with previous/next controls and ellipsis.
 *
 * Learning point: The pagination range is computed to always show the first,
 * last, and pages around the current page, with ellipsis filling gaps.
 */
export function Pagination({
  currentPage,
  totalPages,
  onPageChange,
  siblingCount = 1,
}: PaginationProps) {
  const range = getPageRange(currentPage, totalPages, siblingCount);

  if (totalPages <= 1) return null;

  return (
    <nav className={styles.wrapper} aria-label="Pagination">
      <button
        className={styles.pageButton}
        onClick={() => onPageChange(currentPage - 1)}
        disabled={currentPage <= 1}
        aria-label="Previous page"
      >
        ←
      </button>

      {range.map((item, index) =>
        item === 'ellipsis' ? (
          <span key={`ellipsis-${index}`} className={styles.ellipsis}>
            …
          </span>
        ) : (
          <button
            key={item}
            className={item === currentPage ? styles.pageButtonActive : styles.pageButton}
            onClick={() => onPageChange(item)}
            aria-current={item === currentPage ? 'page' : undefined}
            aria-label={`Page ${item}`}
          >
            {item}
          </button>
        ),
      )}

      <button
        className={styles.pageButton}
        onClick={() => onPageChange(currentPage + 1)}
        disabled={currentPage >= totalPages}
        aria-label="Next page"
      >
        →
      </button>
    </nav>
  );
}

function getPageRange(
  current: number,
  total: number,
  siblingCount: number,
): (number | 'ellipsis')[] {
  const totalNumbers = siblingCount * 2 + 5; // siblings + first + last + current + 2 ellipsis slots

  if (total <= totalNumbers) {
    return Array.from({ length: total }, (_, i) => i + 1);
  }

  const leftSibling = Math.max(current - siblingCount, 2);
  const rightSibling = Math.min(current + siblingCount, total - 1);

  const showLeftEllipsis = leftSibling > 2;
  const showRightEllipsis = rightSibling < total - 1;

  const result: (number | 'ellipsis')[] = [1];

  if (showLeftEllipsis) {
    result.push('ellipsis');
  } else {
    for (let i = 2; i < leftSibling; i++) {
      result.push(i);
    }
  }

  for (let i = leftSibling; i <= rightSibling; i++) {
    result.push(i);
  }

  if (showRightEllipsis) {
    result.push('ellipsis');
  } else {
    for (let i = rightSibling + 1; i < total; i++) {
      result.push(i);
    }
  }

  result.push(total);
  return result;
}
