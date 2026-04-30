'use client';

import Link from 'next/link';
import { usePathname } from 'next/navigation';
import * as styles from './Sidebar.css';

/**
 * Navigation sidebar with route-aware active highlighting.
 *
 * Learning point: usePathname() from next/navigation returns the current
 * URL path. Comparing it to each nav item's href highlights the active page.
 */

interface NavItem {
  href: string;
  label: string;
  icon: string; // Emoji for simplicity — production apps use icon libraries
}

const navItems: NavItem[] = [
  { href: '/', label: 'Dashboard', icon: '🏠' },
  { href: '/pancakes', label: 'Pancakes', icon: '🥞' },
  { href: '/waffles', label: 'Waffles', icon: '🧇' },
  { href: '/orders', label: 'Orders', icon: '📋' },
  { href: '/menu', label: 'Menu', icon: '📖' },
  { href: '/daily-specials', label: 'Daily Specials', icon: '⭐' },
  { href: '/ingredients', label: 'Ingredients', icon: '🥛' },
  { href: '/toppings', label: 'Toppings', icon: '🍫' },
  { href: '/inventory', label: 'Inventory', icon: '📦' },
  { href: '/reservations', label: 'Reservations', icon: '🪑' },
  { href: '/staff', label: 'Staff', icon: '👨‍🍳' },
  { href: '/audit-logs', label: 'Audit Logs', icon: '📝' },
  { href: '/reporting', label: 'Reporting', icon: '📊' },
  { href: '/health', label: 'Health', icon: '💚' },
];

interface SidebarProps {
  collapsed: boolean;
  onToggle: () => void;
}

export function Sidebar({ collapsed, onToggle }: SidebarProps) {
  const pathname = usePathname();

  return (
    <nav className={styles.sidebar} aria-label="Main navigation">
      <div className={styles.logo}>
        <span className={styles.logoIcon}>🍳</span>
        {!collapsed && <span className={styles.logoText}>Breakfast Provider</span>}
      </div>

      <button className={styles.collapseButton} onClick={onToggle} aria-label="Toggle sidebar">
        {collapsed ? '→' : '←'}
      </button>

      <ul className={styles.navList}>
        {navItems.map((item) => {
          const isActive = pathname === item.href ||
            (item.href !== '/' && pathname.startsWith(item.href));
          return (
            <li key={item.href}>
              <Link
                href={item.href as any}
                className={isActive ? styles.navItemActive : styles.navItem}
                prefetch={true} // Next.js prefetch — loads page JS on hover
              >
                <span className={styles.navIcon}>{item.icon}</span>
                {!collapsed && <span className={styles.navLabel}>{item.label}</span>}
              </Link>
            </li>
          );
        })}
      </ul>
    </nav>
  );
}
