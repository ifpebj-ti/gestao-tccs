'use client';

import { useState } from 'react';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { IconDefinition } from '@fortawesome/free-solid-svg-icons';
import { faAngleRight } from '@fortawesome/free-solid-svg-icons';
import React from 'react';

interface CollapseCardProps {
  title: string;
  icon: IconDefinition;
  indicatorNumber?: number;
  indicatorColor?: string;
  profile?: string;
  status?: string;
  children?: React.ReactNode;
  onClick?: () => void;
  isOpen?: boolean;
  onToggle?: () => void;
}

// --- Função-auxiliar para formatar o perfil ---
const formatProfile = (profile?: string): string => {
  if (!profile) return '';

  switch (profile.toUpperCase()) {
    case 'ADMIN':
      return 'Admin';
    case 'COORDINATOR':
      return 'Coordenador';
    case 'SUPERVISOR':
      return 'Supervisor';
    case 'ADVISOR':
      return 'Orientador';
    case 'STUDENT':
      return 'Estudante';
    case 'BANKING':
      return 'Banca';
    case 'LIBRARY':
      return 'Biblioteca';
    default:
      // Caso não esteja mapeado, apenas formata (ex: "OTHER" -> "Other")
      return profile.charAt(0).toUpperCase() + profile.slice(1).toLowerCase();
  }
};

export function CollapseCard({
  title,
  icon,
  indicatorNumber,
  indicatorColor = 'bg-blue-500',
  profile,
  status,
  children,
  onClick,
  isOpen: isOpenProp,
  onToggle: onToggleProp
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

  // Formata o Status (Ativo/Inativo)
  const formattedStatus =
    status?.toUpperCase() === 'ACTIVE'
      ? 'Ativo'
      : status?.toUpperCase() === 'INACTIVE'
        ? 'Inativo'
        : '';

  // --- Formata o Perfil ---
  const formattedProfile = formatProfile(profile);

  return (
    <div
      className={`border rounded-xl shadow-sm mb-3 transition-shadow relative ${
        isOpen ? 'z-10 shadow-lg' : 'z-0'
      }`}
    >
      <button
        className="flex items-center justify-between w-full p-4 text-left text-gray-800 focus:outline-none hover:cursor-pointer"
        onClick={handleClick}
        type="button"
      >
        {/* --- LADO ESQUERDO (TÍTULO, PERFIL, STATUS) --- */}
        <div className="flex items-start gap-3">
          <FontAwesomeIcon
            icon={icon}
            className="text-xl text-[#1351B4] pt-0.5"
          />
          <div className="flex flex-col text-left">
            <span className="font-medium">{title}</span>

            <div className="flex items-center gap-2 mt-1">
              {/* --- Badge de Perfil --- */}
              {formattedProfile && (
                <span className="px-2 py-0.5 bg-gray-200 text-gray-700 rounded-full text-xs font-medium">
                  {formattedProfile}
                </span>
              )}

              {/* Badge de Status */}
              {formattedStatus && (
                <span
                  className={`px-2 py-0.5 rounded-full text-xs font-medium ${
                    status?.toUpperCase() === 'ACTIVE'
                      ? 'bg-green-100 text-green-800'
                      : 'bg-red-100 text-red-800'
                  }`}
                >
                  {formattedStatus}
                </span>
              )}
            </div>
          </div>
        </div>

        {/* --- LADO DIREITO (INDICADOR E SETA) --- */}
        <div className="flex items-center gap-2">
          {indicatorNumber !== undefined && (
            <span
              className={`text-white text-xs px-2 py-1 rounded-full ${indicatorColor}`}
            >
              {indicatorNumber}
            </span>
          )}
          {hasChildren && (
            <FontAwesomeIcon
              icon={faAngleRight}
              className={`w-4 h-4 transition-transform text-[#1351B4] ${
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
