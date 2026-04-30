import { type HTMLAttributes, type ReactNode } from 'react';
import { cardRecipe, type CardVariants } from './Card.css';

interface CardProps extends HTMLAttributes<HTMLDivElement>, NonNullable<CardVariants> {
  children: ReactNode;
}

export function Card({ variant, children, className, ...props }: CardProps) {
  return (
    <div className={`${cardRecipe({ variant })} ${className ?? ''}`} {...props}>
      {children}
    </div>
  );
}
