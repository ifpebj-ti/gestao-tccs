import * as React from 'react';

import { cn } from '@/lib/utils';

function Textarea({ className, ...props }: React.ComponentProps<'textarea'>) {
  return (
    <textarea
      data-slot="textarea"
      className={cn(
        'placeholder:text-muted-foreground aria-invalid:border-destructive flex field-sizing-content min-h-24 w-full border-0 border-b-2 border-[#002B18] bg-transparent py-2.5 text-base text-gray-800 transition-colors duration-150 outline-none focus-visible:border-[#006633] disabled:cursor-not-allowed disabled:opacity-50 md:text-sm',
        className
      )}
      {...props}
    />
  );
}

export { Textarea };
