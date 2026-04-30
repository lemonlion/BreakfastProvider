import { forwardRef, type ButtonHTMLAttributes, type ReactNode } from 'react';
import { buttonRecipe, spinnerStyle, type ButtonVariants } from './Button.css';

interface ButtonProps
  extends ButtonHTMLAttributes<HTMLButtonElement>,
    NonNullable<ButtonVariants> {
  loading?: boolean;
  children: ReactNode;
}

/**
 * Button with variant/size props powered by vanilla-extract recipe().
 *
 * Learning point: forwardRef lets parent components access the underlying
 * <button> DOM element (needed for focus management, tooltips, etc.).
 */
export const Button = forwardRef<HTMLButtonElement, ButtonProps>(
  ({ variant, size, loading, disabled, children, className, ...props }, ref) => (
    <button
      ref={ref}
      className={`${buttonRecipe({ variant, size })} ${className ?? ''}`}
      disabled={disabled || loading}
      aria-busy={loading}
      {...props}
    >
      {loading && (
        <svg style={spinnerStyle} viewBox="0 0 24 24" fill="none" aria-hidden="true">
          <circle cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="3" opacity={0.25} />
          <path
            d="M12 2a10 10 0 0 1 10 10"
            stroke="currentColor"
            strokeWidth="3"
            strokeLinecap="round"
          />
        </svg>
      )}
      {children}
    </button>
  ),
);

Button.displayName = 'Button';
