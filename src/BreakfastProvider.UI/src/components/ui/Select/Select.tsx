import { forwardRef, type SelectHTMLAttributes, type ReactNode } from 'react';
import * as styles from './Select.css';

interface SelectProps extends SelectHTMLAttributes<HTMLSelectElement> {
  label?: string;
  error?: string;
  helperText?: string;
  children: ReactNode;
}

export const Select = forwardRef<HTMLSelectElement, SelectProps>(
  ({ label: labelText, error, helperText: helper, id, children, ...props }, ref) => {
    const selectId = id ?? labelText?.toLowerCase().replace(/\s/g, '-');
    return (
      <div>
        {labelText && (
          <label htmlFor={selectId} className={styles.label}>
            {labelText}
          </label>
        )}
        <select
          ref={ref}
          id={selectId}
          className={styles.selectVariants[error ? 'error' : 'default']}
          aria-invalid={!!error}
          aria-describedby={error ? `${selectId}-error` : undefined}
          {...props}
        >
          {children}
        </select>
        {error && (
          <p id={`${selectId}-error`} className={styles.errorText} role="alert">
            {error}
          </p>
        )}
        {helper && !error && <p className={styles.helperText}>{helper}</p>}
      </div>
    );
  },
);

Select.displayName = 'Select';
