import * as React from 'react';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { IconDefinition } from '@fortawesome/fontawesome-svg-core';

import { cn } from '@/lib/utils';

interface CardHomeProps {
  title: string;
  icon: IconDefinition;
  indicatorNumber?: number;
  indicatorColor?: string; // classes tailwind, ex: 'bg-red-500'
  className?: string;
}

export function CardHome({
  title,
  icon,
  indicatorNumber,
  indicatorColor = 'bg-red-500',
  className = ''
}: CardHomeProps) {
  return (
    <div
      className={cn(
        'relative p-10 bg-white border-1 border-gray-100 rounded-sm shadow-md flex flex-col items-center hover:shadow-xl transition-shadow duration-300 ease-in-out cursor-pointer',
        className
      )}
    >
      {/* Indicador */}
      {indicatorNumber !== undefined && (
        <div
          className={cn(
            'absolute top-2 right-2 flex items-center justify-center text-sm font-semibold text-white rounded-full w-7 h-7',
            indicatorColor
          )}
        >
          {indicatorNumber}
        </div>
      )}

      {/* Ícone */}
      <FontAwesomeIcon
        icon={icon}
        className="text-3xl text-[#1351B4] mb-3"
        fixedWidth
      />

      {/* Título */}
      <h3 className="text-lg font-semibold text-[#1351B4]">{title}</h3>
    </div>
  );
}
