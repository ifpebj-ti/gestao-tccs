'use client';

import { useState } from 'react';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { IconDefinition } from '@fortawesome/fontawesome-svg-core';
import { faAngleRight } from '@fortawesome/free-solid-svg-icons';
import React from 'react';
import { cn } from '@/lib/utils';

interface CollapseCardProps {
  title: string;
  icon: IconDefinition | React.ReactNode;
  indicatorNumber?: number;
  indicatorColor?: string;
  profileText?: string;
  statusText?: string;
  statusClass?: string;
  children?: React.ReactNode;
  onClick?: () => void;
  isOpen?: boolean;
  onToggle?: () => void;
  variant?: 'default' | 'flat';
}

export function CollapseCard({
  title,
  icon,
  indicatorNumber,
  indicatorColor = 'bg-[#002B18]',
  profileText,
  statusText,
  statusClass,
  children,
  onClick,
  isOpen: isOpenProp,
  onToggle: onToggleProp,
  variant = 'default'
}: CollapseCardProps) {
  const [internalIsOpen, setInternalIsOpen] = useState(false);

  const isControlled = isOpenProp !== undefined && onToggleProp !== undefined;
  const isOpen = isControlled ? isOpenProp : internalIsOpen;
  const hasChildren = !!children;

  const handleClick = () => {
    if (hasChildren) {
      if (isControlled) {
        onToggleProp();
      } else {
        setInternalIsOpen((prev) => !prev);
      }
    } else if (onClick) {
      onClick();
    }
  };

  const shouldRenderProfileBadge = !!profileText;
  const shouldRenderStatusBadge = !!statusText && !!statusClass;
  const isFontAwesome = (icon as IconDefinition)?.iconName !== undefined;

  return (
    <div
      className={cn(
        variant === 'flat'
          ? 'border-b border-[#ECEAE2] bg-transparent'
          : 'border border-slate-200/90 rounded-lg bg-white mb-3 transition-colors',
        isOpen && variant === 'default' && 'border-[#002B18]'
      )}
    >
      <button
        className={cn(
          'group flex items-center justify-between gap-3 w-full p-4 text-left text-gray-800 hover:cursor-pointer focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[#006633]',
          variant === 'flat' && 'px-0 py-5'
        )}
        onClick={handleClick}
        type="button"
        aria-expanded={hasChildren ? isOpen : undefined}
      >
        {/* --- LADO ESQUERDO (TÍTULO, PERFIL, STATUS) --- */}
        <div className="flex min-w-0 items-center gap-3">
          <div
            className={cn(
              'w-9 h-9 flex items-center justify-center text-[#002B18] flex-shrink-0',
              variant === 'flat'
                ? 'group-hover:text-[#006633]'
                : 'rounded-md bg-[#002B18]/5'
            )}
          >
            {isFontAwesome ? (
              <FontAwesomeIcon
                icon={icon as IconDefinition}
                className="text-base"
              />
            ) : (
              (icon as React.ReactNode)
            )}
          </div>
          <div className="flex flex-col text-left">
            <span
              className={cn(
                'font-semibold text-sm text-[#002B18]',
                variant === 'flat' &&
                  'font-[family-name:var(--font-poppins)] group-hover:text-[#006633]'
              )}
            >
              {title}
            </span>

            <div className="flex items-center gap-2 mt-0.5">
              {shouldRenderProfileBadge && (
                <span className="px-2 py-0.5 bg-gray-100 text-gray-600 rounded text-xs font-medium">
                  {profileText}
                </span>
              )}

              {shouldRenderStatusBadge && (
                <span
                  className={`px-2 py-0.5 rounded text-xs font-medium ${statusClass}`}
                >
                  {statusText}
                </span>
              )}
            </div>
          </div>
        </div>

        {/* --- LADO DIREITO (INDICADOR E SETA) --- */}
        <div className="flex items-center gap-2">
          {indicatorNumber !== undefined && indicatorNumber > 0 && (
            <span
              className={`text-white text-xs px-2 py-0.5 rounded-full font-bold ${indicatorColor}`}
            >
              {indicatorNumber}
            </span>
          )}
          {hasChildren && (
            <FontAwesomeIcon
              icon={faAngleRight}
              className={`w-4 h-4 transition-transform text-gray-400 ${
                isOpen ? 'rotate-270' : 'rotate-90'
              }`}
            />
          )}
        </div>
      </button>

      {hasChildren && isOpen && <div className="px-4 pb-4">{children}</div>}
    </div>
  );
}
