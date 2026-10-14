import { forwardRef, type ComponentProps } from 'react';
import { ArrowUpRight, type LucideIcon } from 'lucide-react';
import { cn } from '@/lib/utils';
type Props = ComponentProps<'button'> & {
  heading: string;
  description: string;
  actionLabel: string;
  icon: LucideIcon;
  count?: number;
};
export const FeatureActionCard = forwardRef<HTMLButtonElement, Props>(
  function FeatureActionCard(
    {
      heading,
      description,
      actionLabel,
      icon: Icon,
      count,
      className,
      ...props
    },
    ref
  ) {
    return (
      <button
        type="button"
        ref={ref}
        {...props}
        className={cn(
          'group h-fit w-full cursor-pointer rounded-xl bg-[#183E2D] p-7 text-left text-[#EFF2E8] transition-[background-color,transform] duration-150 hover:-translate-y-0.5 hover:bg-[#002B18] focus-visible:outline-2 focus-visible:outline-offset-4 focus-visible:outline-[#006633] motion-reduce:transform-none',
          className
        )}
      >
        <span className="mb-6 flex items-center justify-between">
          <Icon
            className="size-7 text-[#C8FF47]"
            strokeWidth={1.5}
            aria-hidden="true"
          />
          {count !== undefined && count > 0 && (
            <span
              aria-label={`${count} pendências`}
              className="rounded-full bg-[#C8FF47] px-3 py-1 text-sm font-bold text-[#002B18]"
            >
              {count}
            </span>
          )}
        </span>
        <span className="block text-xl font-semibold leading-tight tracking-tight">
          {heading}
        </span>
        <span className="mt-4 block text-xs leading-relaxed text-[#C9D5C9]">
          {description}
        </span>
        <span className="mt-6 inline-flex items-center gap-2 rounded-full border border-[#C8FF47]/40 px-4 py-2 text-sm font-semibold text-[#C8FF47] transition-colors duration-150 group-hover:bg-[#C8FF47] group-hover:text-[#002B18]">
          {actionLabel}
          <ArrowUpRight
            size={16}
            aria-hidden="true"
            className="transition-transform duration-150 group-hover:translate-x-1 group-hover:-translate-y-0.5 motion-reduce:transform-none"
          />
        </span>
      </button>
    );
  }
);
