import * as React from 'react';
import { Slot } from '@radix-ui/react-slot';
import { cva, type VariantProps } from 'class-variance-authority';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { IconDefinition } from '@fortawesome/fontawesome-svg-core';

import { cn } from '@/lib/utils';

const buttonVariants = cva(
  'inline-flex cursor-pointer items-center justify-center gap-2 whitespace-nowrap rounded-full text-sm font-semibold transition-all duration-300 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[#002B18] disabled:pointer-events-none disabled:opacity-50 active:scale-[0.97]',
  {
    variants: {
      variant: {
        // Padrão Verde Escuro: quando escuro, hover/active clareia para verde mais claro (#00A355 / #008C45) com destaque
        default:
          'bg-[#002B18] text-[#C8FF47] shadow-sm hover:bg-[#00A355] hover:text-white hover:shadow-md hover:scale-[1.01] active:scale-[0.98]',
        brand:
          'bg-[#002B18] text-[#C8FF47] shadow-sm hover:bg-[#00A355] hover:text-white hover:shadow-md hover:scale-[1.01] active:scale-[0.98]',
        // Padrão Verde Claro / Accent: quando claro, hover/active escurece para verde escuro (#002B18)
        accent:
          'bg-[#C8FF47] text-[#002B18] shadow-sm hover:bg-[#002B18] hover:text-[#C8FF47] hover:shadow-md hover:scale-[1.01] active:scale-[0.98]',
        outline:
          'border border-slate-300 bg-white text-gray-700 hover:bg-slate-50 hover:text-[#002B18] hover:border-slate-400',
        ghost: 'bg-transparent text-gray-700 hover:bg-slate-100 hover:text-[#002B18]',
        destructive:
          'bg-transparent border border-red-600 text-red-600 hover:bg-red-700 hover:text-white hover:border-red-700 focus-visible:ring-red-500',
        cancel:
          'bg-white border border-gray-300 text-gray-700 hover:bg-red-700 hover:text-white hover:border-red-700 transition-colors',
        link: 'text-[#002B18] underline-offset-4 hover:underline'
      },
      size: {
        default: 'h-10 px-5 py-2',
        sm: 'h-8 px-3.5 text-xs',
        lg: 'h-12 px-7 text-base'
      }
    },
    defaultVariants: {
      variant: 'default',
      size: 'default'
    }
  }
);


export interface ButtonProps
  extends React.ButtonHTMLAttributes<HTMLButtonElement>,
    VariantProps<typeof buttonVariants> {
  asChild?: boolean;
  icon?: IconDefinition;
}

const Button = React.forwardRef<HTMLButtonElement, ButtonProps>(
  (
    { className, variant, size, icon, asChild = false, children, ...props },
    ref
  ) => {
    const Comp = asChild ? Slot : 'button';
    return (
      <Comp
        className={cn(buttonVariants({ variant, size, className }))}
        ref={ref}
        {...props}
      >
        {icon && (
          <FontAwesomeIcon
            icon={icon}
            className={children ? 'mr-1' : undefined}
          />
        )}
        {children}
      </Comp>
    );
  }
);
Button.displayName = 'Button';

export { Button, buttonVariants };
