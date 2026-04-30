/**
 * Route group layout for kitchen items (pancakes, waffles).
 *
 * Learning point: (kitchen) is a route group — parentheses mean this
 * folder does NOT create a URL segment. /pancakes and /waffles both
 * share this layout, but the URL is /pancakes, NOT /kitchen/pancakes.
 */
export default function KitchenLayout({ children }: { children: React.ReactNode }) {
  return (
    <div>
      {/* Shared kitchen-area UI (e.g., a sub-navigation bar) */}
      <nav aria-label="Kitchen navigation" style={{ display: 'flex', gap: 16, marginBottom: 16 }}>
        {/* Links to /pancakes and /waffles */}
      </nav>
      {children}
    </div>
  );
}
