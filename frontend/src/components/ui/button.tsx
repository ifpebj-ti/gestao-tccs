import * as React from 'react';
import { Slot } from '@radix-ui/react-slot';
import { cva, type VariantProps } from 'class-variance-authority';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { IconDefinition } from '@fortawesome/fontawesome-svg-core';

import { cn } from '@/lib/utils';

const negativeButtonStyles =
  'border border-red-600 bg-red-600 text-white hover:border-red-700 hover:bg-red-700 hover:text-white active:border-red-800 active:bg-red-800 focus-visible:ring-red-500';

const buttonVariants = cva(
  'inline-flex cursor-pointer items-center justify-center gap-2 whitespace-nowrap rounded-full text-sm font-bold transition-[color,background-color,transform] duration-150 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[#006633] focus-visible:ring-offset-2 disabled:pointer-events-none disabled:opacity-50 motion-reduce:transform-none',
  {
    variants: {
      variant: {
        // Ações principais seguem a inversão de cores do fluxo de autenticação.
        default:
          'bg-[#C8FF47] text-[#002B18] hover:bg-[#002B18] hover:text-[#C8FF47] hover:-translate-y-0.5',
        brand:
          'bg-[#002B18] text-[#C8FF47] hover:bg-[#006633] hover:-translate-y-0.5',
        // Padrão Verde Claro / Accent: quando claro, hover/active escurece para verde escuro (#002B18)
        accent:
          'bg-[#C8FF47] text-[#002B18] hover:bg-[#002B18] hover:text-[#C8FF47] hover:-translate-y-0.5',
        outline:
          'border border-[#006633] bg-transparent text-[#002B18] hover:bg-[#002B18] hover:text-[#C8FF47] hover:border-[#002B18]',
        ghost:
          'bg-transparent text-[#002B18] hover:bg-[#002B18] hover:text-[#C8FF47]',
        destructive: negativeButtonStyles,
        cancel: negativeButtonStyles,
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
