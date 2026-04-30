'use client';

import { Component, type ErrorInfo, type ReactNode } from 'react';

interface ErrorBoundaryProps {
  children: ReactNode;
  fallback?: ReactNode;
}

interface ErrorBoundaryState {
  hasError: boolean;
  error: Error | null;
}

/**
 * React Error Boundary — catches render errors in child components.
 *
 * Learning point: Error Boundaries must be class components because
 * componentDidCatch and getDerivedStateFromError have no hook equivalents.
 */
export class ErrorBoundary extends Component<ErrorBoundaryProps, ErrorBoundaryState> {
  constructor(props: ErrorBoundaryProps) {
    super(props);
    this.state = { hasError: false, error: null };
  }

  static getDerivedStateFromError(error: Error): ErrorBoundaryState {
    return { hasError: true, error };
  }

  componentDidCatch(error: Error, errorInfo: ErrorInfo) {
    if (process.env.NODE_ENV === 'development') {
      console.error('ErrorBoundary caught:', error, errorInfo);
    }
  }

  render() {
    if (this.state.hasError) {
      if (this.props.fallback) {
        return this.props.fallback;
      }

      return (
        <div
          style={{
            padding: '24px',
            textAlign: 'center',
            color: '#DC2626',
          }}
        >
          <h2>Something went wrong</h2>
          <p>{this.state.error?.message}</p>
          <button
            onClick={() => this.setState({ hasError: false, error: null })}
            style={{
              marginTop: '16px',
              padding: '8px 16px',
              cursor: 'pointer',
              borderRadius: '6px',
              border: '1px solid #DC2626',
              backgroundColor: 'transparent',
              color: '#DC2626',
            }}
          >
            Try again
          </button>
        </div>
      );
    }

    return this.props.children;
  }
}
