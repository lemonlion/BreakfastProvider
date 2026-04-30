import { badgeVariants, type BadgeVariant } from './Badge.css';

interface BadgeProps {
  variant: BadgeVariant;
  children: React.ReactNode;
  /** Optional dot indicator before text */
  dot?: boolean;
}

export function Badge({ variant, children, dot }: BadgeProps) {
  return (
    <span className={badgeVariants[variant]}>
      {dot && (
        <span
          style={{
            width: 6,
            height: 6,
            borderRadius: '50%',
            backgroundColor: 'currentColor',
          }}
          aria-hidden="true"
        />
      )}
      {children}
    </span>
  );
}
