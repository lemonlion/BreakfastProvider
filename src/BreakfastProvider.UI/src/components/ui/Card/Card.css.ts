import { recipe, type RecipeVariants } from '@vanilla-extract/recipes';
import { vars } from '@/styles/theme.css';

export const cardRecipe = recipe({
  base: {
    borderRadius: vars.radius.lg,
    padding: vars.space[6],
    transition: `all ${vars.transition.normal}`,
  },
  variants: {
    variant: {
      elevated: {
        backgroundColor: vars.color.neutral0,
        boxShadow: vars.shadow.md,
        ':hover': { boxShadow: vars.shadow.lg },
      },
      outlined: {
        backgroundColor: vars.color.neutral0,
        border: `1px solid ${vars.color.border}`,
        ':hover': { borderColor: vars.color.borderHover },
      },
      flat: {
        backgroundColor: vars.color.surface,
      },
    },
  },
  defaultVariants: { variant: 'elevated' },
});

export type CardVariants = RecipeVariants<typeof cardRecipe>;
