'use client';

import * as React from 'react';
import * as Menu from '@radix-ui/react-dropdown-menu';
import { Check, ChevronDown } from 'lucide-react';
import {
  useController,
  type Control,
  type FieldPath,
  type FieldValues
} from 'react-hook-form';
import { cn } from '@/lib/utils';

type Option = { value: string; label: React.ReactNode; disabled?: boolean };

export function selectOptions(children: React.ReactNode): Option[] {
  return React.Children.toArray(children).flatMap((child) => {
    if (
      !React.isValidElement<{
        value?: string | number;
        children?: React.ReactNode;
        disabled?: boolean;
      }>(child)
    )
      return [];
    if (child.type === React.Fragment)
      return selectOptions(child.props.children);
    if (child.type !== 'option') return [];
    return [
      {
        value: String(child.props.value ?? ''),
        label: child.props.children,
        disabled: child.props.disabled
      }
    ];
  });
}

type SelectProps = Omit<
  React.ButtonHTMLAttributes<HTMLButtonElement>,
  'value' | 'defaultValue' | 'onChange' | 'type'
> & {
  value?: string | number | null;
  onValueChange: (value: string) => void;
};

export const PatternSelect = React.forwardRef<HTMLButtonElement, SelectProps>(
  function PatternSelect(
    { value, onValueChange, children, className, disabled, ...props },
    ref
  ) {
    const options = selectOptions(children);
    const selectedValue = value == null ? '' : String(value);
    const selected = options.find((option) => option.value === selectedValue);
    return (
      <Menu.Root modal={false}>
        <Menu.Trigger asChild>
          <button
            {...props}
            ref={ref}
            type="button"
            disabled={disabled}
            className={cn(
              'w-full min-w-0 border-0 border-b-2 border-[#183E2D] bg-transparent py-2.5 text-left text-sm text-[#183E2D] outline-none focus-visible:ring-2 focus-visible:ring-[#183E2D]/40 disabled:cursor-not-allowed disabled:opacity-50',
              className
            )}
          >
            <span className="flex w-full items-center justify-between gap-3">
              <span className="min-w-0 truncate">
                {selected?.label ?? options[0]?.label ?? 'Selecione uma opção'}
              </span>
              <ChevronDown
                aria-hidden="true"
                className="size-4 shrink-0 text-[#183E2D]"
              />
            </span>
          </button>
        </Menu.Trigger>
        <Menu.Portal>
          <Menu.Content
            align="start"
            sideOffset={6}
            collisionPadding={12}
            className="z-[100] max-h-[min(20rem,var(--radix-dropdown-menu-content-available-height))] w-[var(--radix-dropdown-menu-trigger-width)] overflow-y-auto rounded-xl border border-[#DDD9CD] bg-[#FAF9F5] p-1.5 shadow-xl"
          >
            <Menu.RadioGroup
              value={selectedValue}
              onValueChange={onValueChange}
            >
              {options.map((option) => (
                <Menu.RadioItem
                  key={option.value}
                  value={option.value}
                  disabled={option.disabled}
                  className="relative cursor-pointer rounded-lg py-3 pl-3 pr-9 text-sm text-[#183E2D] outline-none data-[highlighted]:bg-[#E8F3EC] data-[state=checked]:bg-[#183E2D] data-[state=checked]:text-[#EFF2E8] data-[state=checked]:data-[highlighted]:bg-[#183E2D] data-[disabled]:pointer-events-none data-[disabled]:opacity-50"
                >
                  {option.label}
                  <Menu.ItemIndicator className="absolute right-3 top-1/2 -translate-y-1/2">
                    <Check aria-hidden="true" className="size-4" />
                  </Menu.ItemIndicator>
                </Menu.RadioItem>
              ))}
            </Menu.RadioGroup>
          </Menu.Content>
        </Menu.Portal>
      </Menu.Root>
    );
  }
);

type FormSelectProps<T extends FieldValues> = Omit<
  SelectProps,
  'value' | 'onValueChange'
> & {
  control: Control<T>;
  name: FieldPath<T>;
  parseValue?: (value: string) => unknown;
};

export function FormSelect<T extends FieldValues>({
  control,
  name,
  parseValue,
  ...props
}: FormSelectProps<T>) {
  const { field, fieldState } = useController({ control, name });
  const value = Array.isArray(field.value) ? field.value[0] : field.value;
  return (
    <PatternSelect
      {...props}
      name={name}
      ref={field.ref}
      value={value}
      onBlur={field.onBlur}
      aria-invalid={props['aria-invalid'] ?? !!fieldState.error}
      onValueChange={(value) =>
        field.onChange(parseValue ? parseValue(value) : value)
      }
    />
  );
}
