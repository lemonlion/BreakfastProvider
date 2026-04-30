import type { Metadata } from 'next';
import { Inter, JetBrains_Mono } from 'next/font/google';
import { QueryProvider } from '@/providers/query-provider';
import { ThemeProvider } from '@/providers/theme-provider';
import { ToastProvider } from '@/components/ui/Toast/Toast';
import { AppShell } from '@/components/layout/AppShell/AppShell';
import '@/styles/global.css';

/**
 * next/font optimisation — fonts are self-hosted at build time.
 *
 * Learning point: By defining fonts here and passing them via CSS variables,
 * all pages share the same font instances with zero layout shift (FOUT).
 */
const inter = Inter({
  subsets: ['latin'],
  variable: '--font-body',
  display: 'swap',
});

const jetbrainsMono = JetBrains_Mono({
  subsets: ['latin'],
  variable: '--font-mono',
  display: 'swap',
});

/**
 * Static metadata for the entire application.
 *
 * Learning point: Next.js merges metadata from parent → child layouts.
 * Each page can override or extend these values.
 */
export const metadata: Metadata = {
  title: {
    default: 'BreakfastProvider Dashboard',
    template: '%s | BreakfastProvider',
  },
  description: 'Breakfast preparation management system',
};

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="en" className={`${inter.variable} ${jetbrainsMono.variable}`}>
      <body>
        <QueryProvider>
          <ThemeProvider>
            <ToastProvider>
              <AppShell>{children}</AppShell>
            </ToastProvider>
          </ThemeProvider>
        </QueryProvider>
      </body>
    </html>
  );
}
