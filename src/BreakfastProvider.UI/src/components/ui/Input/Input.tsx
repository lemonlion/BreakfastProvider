import { forwardRef, type InputHTMLAttributes } from 'react';
import * as styles from './Input.css';

interface InputProps extends InputHTMLAttributes<HTMLInputElement> {
  label?: string;
  error?: string;
  helperText?: string;
}

export const Input = forwardRef<HTMLInputElement, InputProps>(
  ({ label: labelText, error, helperText: helper, id, ...props }, ref) => {
    const inputId = id ?? labelText?.toLowerCase().replace(/\s/g, '-');
    return (
      <div>
        {labelText && (
          <label htmlFor={inputId} className={styles.label}>
            {labelText}
          </label>
        )}
        <input
          ref={ref}
          id={inputId}
          className={styles.inputVariants[error ? 'error' : 'default']}
          aria-invalid={!!error}
          aria-describedby={error ? `${inputId}-error` : undefined}
          {...props}
        />
        {error && (
          <p id={`${inputId}-error`} className={styles.errorText} role="alert">
            {error}
          </p>
        )}
        {helper && !error && <p className={styles.helperText}>{helper}</p>}
      </div>
    );
  },
);

Input.displayName = 'Input';
