import { styleVariants, style } from '@vanilla-extract/css';
import { vars } from '@/styles/theme.css';

/**
 * styleVariants() creates a map of className → styles.
 *
 * Learning point: Unlike recipe() (which handles multiple axes),
 * styleVariants is for a SINGLE dimension of variation. Perfect for
 * color-coded status badges.
 */
const badgeBase = style({
  display: 'inline-flex',
  alignItems: 'center',
  gap: vars.space[1],
  padding: `${vars.space[1]} ${vars.space[2]}`,
  fontSize: vars.fontSize.xs,
  fontWeight: vars.fontWeight.medium,
  borderRadius: vars.radius.full,
  lineHeight: '1',
  whiteSpace: 'nowrap',
});

export const badgeVariants = styleVariants({
  success: [badgeBase, { backgroundColor: vars.color.successLight, color: vars.color.success }],
  warning: [badgeBase, { backgroundColor: vars.color.warningLight, color: vars.color.warning }],
  error: [badgeBase, { backgroundColor: vars.color.errorLight, color: vars.color.error }],
  info: [badgeBase, { backgroundColor: vars.color.infoLight, color: vars.color.info }],
  neutral: [badgeBase, { backgroundColor: vars.color.neutral100, color: vars.color.neutral600 }],
});

export type BadgeVariant = keyof typeof badgeVariants;
