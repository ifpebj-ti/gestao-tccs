'use client';

import { useState } from 'react';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { IconDefinition } from '@fortawesome/free-solid-svg-icons';
import { faAngleRight } from '@fortawesome/free-solid-svg-icons'; // importa o ícone

interface CollapseCardMobileProps {
  title: string;
  icon: IconDefinition;
  indicatorNumber?: number;
  indicatorColor?: string;
  children?: React.ReactNode;
  onClick?: () => void; // ação quando não tiver filhos, tipo direcionar
}

export function CollapseCardMobile({
  title,
  icon,
  indicatorNumber,
  indicatorColor = 'bg-blue-500',
  children,
  onClick
}: CollapseCardMobileProps) {
  const [isOpen, setIsOpen] = useState(false);

  const hasChildren = !!children;

  const handleClick = () => {
    if (hasChildren) {
      setIsOpen(!isOpen);
    } else if (onClick) {
      onClick();
    }
  };

  return (
    <div className="border rounded-xl shadow-sm mb-3 md:hidden">
      <button
        className="flex items-center justify-between w-full p-4 text-left text-gray-800 font-medium focus:outline-none hover:cursor-pointer hover:shadow-xl transition-shadow duration-300 ease-in-out"
        onClick={handleClick}
        type="button"
      >
        <div className="flex items-center gap-3">
          <FontAwesomeIcon icon={icon} className="text-xl text-[#1351B4]" />
          <span>{title}</span>
        </div>
        <div className="flex items-center gap-2">
          {indicatorNumber !== undefined && (
            <span
              className={`text-white text-xs px-2 py-1 rounded-full ${indicatorColor}`}
            >
              {indicatorNumber}
            </span>
          )}

          {/* Ícone da seta com FontAwesome */}
          <FontAwesomeIcon
            icon={faAngleRight}
            className={`w-4 h-4 transition-transform text-[#1351B4] ${
              hasChildren && isOpen ? 'rotate-90' : 'rotate-0'
            }`}
          />
        </div>
      </button>

      {hasChildren && isOpen && <div className="px-4 pb-4">{children}</div>}
    </div>
  );
}
