import { recipe, type RecipeVariants } from '@vanilla-extract/recipes';
import { vars } from '@/styles/theme.css';
import { spin } from '@/styles/animations.css';

/**
 * recipe() creates a multi-variant style function.
 *
 * Learning point: Unlike styleVariants (which maps a key to a single style),
 * recipe() supports MULTIPLE variant axes that combine. For example:
 * button({ variant: 'primary', size: 'lg' }) generates a unique className
 * combining both the primary colour and lg size styles.
 */
export const buttonRecipe = recipe({
  base: {
    display: 'inline-flex',
    alignItems: 'center',
    justifyContent: 'center',
    gap: vars.space[2],
    fontFamily: vars.font.body,
    fontWeight: vars.fontWeight.medium,
    borderRadius: vars.radius.md,
    border: 'none',
    cursor: 'pointer',
    transition: `all ${vars.transition.fast}`,
    lineHeight: vars.lineHeight.tight,
    ':disabled': {
      opacity: 0.5,
      cursor: 'not-allowed',
    },
    ':focus-visible': {
      outline: `2px solid ${vars.color.primary500}`,
      outlineOffset: '2px',
    },
  },
  variants: {
    variant: {
      primary: {
        backgroundColor: vars.color.primary500,
        color: vars.color.textInverse,
        ':hover': { backgroundColor: vars.color.primary600 },
        ':active': { backgroundColor: vars.color.primary700 },
      },
      secondary: {
        backgroundColor: vars.color.surface,
        color: vars.color.text,
        border: `1px solid ${vars.color.border}`,
        ':hover': { backgroundColor: vars.color.surfaceHover, borderColor: vars.color.borderHover },
      },
      danger: {
        backgroundColor: vars.color.error,
        color: vars.color.textInverse,
        ':hover': { opacity: 0.9 },
      },
      ghost: {
        backgroundColor: 'transparent',
        color: vars.color.text,
        ':hover': { backgroundColor: vars.color.surface },
      },
    },
    size: {
      sm: { padding: `${vars.space[1]} ${vars.space[3]}`, fontSize: vars.fontSize.sm },
      md: { padding: `${vars.space[2]} ${vars.space[4]}`, fontSize: vars.fontSize.md },
      lg: { padding: `${vars.space[3]} ${vars.space[6]}`, fontSize: vars.fontSize.lg },
    },
  },
  defaultVariants: {
    variant: 'primary',
    size: 'md',
  },
});

export type ButtonVariants = RecipeVariants<typeof buttonRecipe>;

/** Spinner icon animation for loading state */
export const spinnerStyle = {
  animation: `${spin} 1s linear infinite`,
  width: '1em',
  height: '1em',
};
