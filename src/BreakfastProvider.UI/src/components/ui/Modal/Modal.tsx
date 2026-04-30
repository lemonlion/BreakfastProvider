'use client';

import { useEffect, useRef, type ReactNode } from 'react';
import { createPortal } from 'react-dom';
import * as styles from './Modal.css';

interface ModalProps {
  isOpen: boolean;
  onClose: () => void;
  title: string;
  children: ReactNode;
  size?: 'sm' | 'md' | 'lg';
}

/**
 * Modal dialog with portal, focus trap, and animations.
 *
 * Learning points:
 * - createPortal() renders outside the parent DOM tree (at document.body)
 * - useRef + useEffect for focus management (trap focus inside modal)
 * - Escape key handler for accessibility
 * - Backdrop click to close
 * - Keyframe animations for enter/exit (fadeIn, scaleIn from animations.css.ts)
 */
export function Modal({ isOpen, onClose, title, children, size = 'md' }: ModalProps) {
  const modalRef = useRef<HTMLDivElement>(null);

  // Focus trap: focus first focusable element on open
  useEffect(() => {
    if (isOpen && modalRef.current) {
      const focusable = modalRef.current.querySelector<HTMLElement>(
        'button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])',
      );
      focusable?.focus();
    }
  }, [isOpen]);

  // Escape key handler
  useEffect(() => {
    const handleEscape = (e: KeyboardEvent) => {
      if (e.key === 'Escape') onClose();
    };
    if (isOpen) {
      document.addEventListener('keydown', handleEscape);
      // Prevent background scrolling
      document.body.style.overflow = 'hidden';
    }
    return () => {
      document.removeEventListener('keydown', handleEscape);
      document.body.style.overflow = '';
    };
  }, [isOpen, onClose]);

  if (!isOpen) return null;

  return createPortal(
    <div className={styles.backdrop} onClick={onClose} role="presentation">
      <div
        ref={modalRef}
        className={styles.modalVariants[size]}
        role="dialog"
        aria-modal="true"
        aria-label={title}
        onClick={(e) => e.stopPropagation()} // Prevent close on modal body click
      >
        <div className={styles.header}>
          <h2 className={styles.title}>{title}</h2>
          <button className={styles.closeButton} onClick={onClose} aria-label="Close">
            ✕
          </button>
        </div>
        <div className={styles.body}>{children}</div>
      </div>
    </div>,
    document.body,
  );
}
