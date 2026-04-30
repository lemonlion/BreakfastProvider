import { forwardRef, type TextareaHTMLAttributes } from 'react';
import * as styles from './TextArea.css';

interface TextAreaProps extends TextareaHTMLAttributes<HTMLTextAreaElement> {
  label?: string;
  error?: string;
  helperText?: string;
}

export const TextArea = forwardRef<HTMLTextAreaElement, TextAreaProps>(
  ({ label: labelText, error, helperText: helper, id, rows = 4, ...props }, ref) => {
    const textareaId = id ?? labelText?.toLowerCase().replace(/\s/g, '-');
    return (
      <div>
        {labelText && (
          <label htmlFor={textareaId} className={styles.label}>
            {labelText}
          </label>
        )}
        <textarea
          ref={ref}
          id={textareaId}
          rows={rows}
          className={styles.textareaVariants[error ? 'error' : 'default']}
          aria-invalid={!!error}
          aria-describedby={error ? `${textareaId}-error` : undefined}
          {...props}
        />
        {error && (
          <p id={`${textareaId}-error`} className={styles.errorText} role="alert">
            {error}
          </p>
        )}
        {helper && !error && <p className={styles.helperText}>{helper}</p>}
      </div>
    );
  },
);

TextArea.displayName = 'TextArea';
