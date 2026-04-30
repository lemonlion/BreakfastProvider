'use client';

import { createContext, useContext, useState, useCallback, type ReactNode } from 'react';
import { assignInlineVars } from '@vanilla-extract/dynamic';
import { toastTimerWidth, toastOffset } from '@/styles/vars.css';
import * as styles from './Toast.css';

type ToastVariant = 'success' | 'error' | 'warning' | 'info';

interface ToastItem {
  id: string;
  message: string;
  variant: ToastVariant;
  duration?: number;
}

interface ToastContextValue {
  addToast: (message: string, variant?: ToastVariant, duration?: number) => void;
  removeToast: (id: string) => void;
}

const ToastContext = createContext<ToastContextValue | undefined>(undefined);

/**
 * Toast provider — manages a stack of notification toasts.
 *
 * Learning points:
 * - assignInlineVars() from @vanilla-extract/dynamic sets CSS variables
 *   at runtime (the timer bar width and vertical offset)
 * - Each toast auto-dismisses after `duration` ms
 * - Multiple toasts stack vertically using dynamic toastOffset
 */
export function ToastProvider({ children }: { children: ReactNode }) {
  const [toasts, setToasts] = useState<ToastItem[]>([]);

  const removeToast = useCallback((id: string) => {
    setToasts((prev) => prev.filter((t) => t.id !== id));
  }, []);

  const addToast = useCallback(
    (message: string, variant: ToastVariant = 'info', duration = 5000) => {
      const id = Math.random().toString(36).slice(2);
      setToasts((prev) => [...prev, { id, message, variant, duration }]);

      // Auto-dismiss
      if (duration > 0) {
        setTimeout(() => removeToast(id), duration);
      }
    },
    [removeToast],
  );

  return (
    <ToastContext.Provider value={{ addToast, removeToast }}>
      {children}
      {/* Toast container — fixed position top-right */}
      <div className={styles.container} aria-live="polite">
        {toasts.map((toast, index) => (
          <div
            key={toast.id}
            className={styles.toastVariants[toast.variant]}
            style={assignInlineVars({
              [toastOffset]: `${index * 72}px`,
              [toastTimerWidth]: '100%',
            })}
          >
            <span>{toast.message}</span>
            <button
              className={styles.dismissButton}
              onClick={() => removeToast(toast.id)}
              aria-label="Dismiss notification"
            >
              ✕
            </button>
            {/* Timer bar — see Toast.css.ts for the animation */}
            <div className={styles.timerBar} style={{ animationDuration: `${toast.duration}ms` }} />
          </div>
        ))}
      </div>
    </ToastContext.Provider>
  );
}

/** Hook to show toast notifications */
export function useToast() {
  const context = useContext(ToastContext);
  if (!context) throw new Error('useToast must be used within ToastProvider');
  return context;
}
