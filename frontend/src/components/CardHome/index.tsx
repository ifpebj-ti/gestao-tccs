import * as React from 'react';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { IconDefinition } from '@fortawesome/fontawesome-svg-core';
import { cn } from '@/lib/utils';
import { ArrowRight } from 'lucide-react';

interface CardHomeProps {
  title: string;
  icon: IconDefinition | React.ReactNode;
  description?: string;
  indicatorNumber?: number;
  indicatorColor?: string;
  className?: string;
  onClick?: () => void;
  variant?: 'default' | 'flat';
}

export function CardHome({
  title,
  icon,
  description,
  indicatorNumber,
  indicatorColor = 'bg-[#002B18] text-white',
  className = '',
  onClick,
  variant = 'default'
}: CardHomeProps) {
  const isFontAwesome = (icon as IconDefinition)?.iconName !== undefined;

  if (variant === 'flat') {
    return (
      <button
        type="button"
        onClick={onClick}
        className={cn(
          'group flex w-full cursor-pointer items-start gap-4 border-b border-[#ECEAE2] px-3 py-6 text-left text-[#002B18] transition-colors duration-150 hover:bg-[#EAF2E8] hover:text-[#006633] focus-visible:bg-[#EAF2E8] focus-visible:outline-2 focus-visible:outline-offset-4 focus-visible:outline-[#006633]',
          className
        )}
      >
        <span className="mt-0.5 shrink-0" aria-hidden="true">
          {isFontAwesome ? (
            <FontAwesomeIcon icon={icon as IconDefinition} fixedWidth />
          ) : (
            (icon as React.ReactNode)
          )}
        </span>
        <span className="min-w-0 flex-1">
          <span className="flex flex-wrap items-center gap-2 font-[family-name:var(--font-poppins)] text-sm font-semibold group-hover:underline underline-offset-4">
            {title}
            {indicatorNumber !== undefined && indicatorNumber > 0 && (
              <span
                className={cn(
                  'rounded-full px-2 py-0.5 text-xs font-bold',
                  indicatorColor
                )}
              >
                {indicatorNumber}
              </span>
            )}
          </span>
          {description && (
            <span className="mt-1.5 block text-sm leading-relaxed text-gray-500">
              {description}
            </span>
          )}
        </span>
        <ArrowRight
          aria-hidden="true"
          className="mt-0.5 size-4 shrink-0 transition-transform duration-150 group-hover:translate-x-1 motion-reduce:transform-none"
        />
      </button>
    );
  }

  return (
    <div
      role={onClick ? 'button' : undefined}
      tabIndex={onClick ? 0 : undefined}
      onClick={onClick}
      onKeyDown={(e) => {
        if (onClick && (e.key === 'Enter' || e.key === ' ')) {
          e.preventDefault();
          onClick();
        }
      }}
      className={cn(
        'group relative p-6 bg-white border border-slate-200/90 rounded-lg flex flex-col items-center text-center transition-all duration-200 hover:border-[#002B18] hover:bg-slate-50/40',
        onClick ? 'cursor-pointer' : '',
        className
      )}
    >
      {/* Indicador numérico */}
      {indicatorNumber !== undefined && indicatorNumber > 0 && (
        <div
          className={cn(
            'absolute top-3 right-3 flex items-center justify-center text-xs font-bold px-2 py-0.5 rounded-full min-w-5 h-5',
            indicatorColor
          )}
        >
          {indicatorNumber}
        </div>
      )}

      {/* Ícone vazado / minimalista */}
      <div className="w-12 h-12 rounded-md bg-[#002B18]/5 flex items-center justify-center text-[#002B18] mb-4 transition-colors group-hover:bg-[#002B18]/10">
        {isFontAwesome ? (
          <FontAwesomeIcon
            icon={icon as IconDefinition}
            className="text-xl text-[#002B18]"
            fixedWidth
          />
        ) : (
          (icon as React.ReactNode)
        )}
      </div>

      {/* Título */}
      <h3 className="text-base font-semibold text-[#002B18] tracking-tight group-hover:text-[#002B18]">
        {title}
      </h3>

      {/* Descrição opcional */}
      {description && (
        <p className="mt-1.5 text-xs text-gray-500 line-clamp-2">
          {description}
        </p>
      )}
    </div>
  );
}
