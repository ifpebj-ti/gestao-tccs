'use client';

import React from 'react';
import { Check } from 'lucide-react';

interface StepProps {
  currentStep: number;
}

const predefinedSteps = [
  { id: 1, label: 'Cadastro de Proposta', mobileLabel: 'Proposta' },
  { id: 2, label: 'Início e Organização', mobileLabel: 'Início' },
  { id: 3, label: 'Desenvolvimento e Acompanhamento', mobileLabel: 'Desenv.' },
  { id: 4, label: 'Preparação para Apresentação', mobileLabel: 'Preparação' },
  { id: 5, label: 'Apresentação e Avaliação', mobileLabel: 'Apresentação' },
  { id: 6, label: 'Finalização e Publicação', mobileLabel: 'Finalização' }
];

export default function Step({ currentStep }: StepProps) {
  return (
    <div className="flex items-center justify-between w-full overflow-x-auto py-5 md:pr-0 pb-12 md:pb-20 lg:pb-20 select-none">
      {predefinedSteps.map((step, index) => {
        const isCompleted = step.id < currentStep;
        const isCurrent = step.id === currentStep;

        return (
          <div
            key={step.id}
            className="flex-1 flex flex-col items-center relative"
          >
            {/* Linha conectando steps */}
            {index > 0 && (
              <div
                className={`absolute top-4 lg:top-5 left-0 w-full h-0.5 z-0 -translate-x-1/2 ${
                  isCompleted ? 'bg-[#1351B4]' : 'bg-gray-300'
                }`}
              />
            )}

            {/* Círculo do step */}
            <div
              className={`z-10 flex items-center justify-center lg:w-12 lg:h-12 w-10 h-10 lg:text-2xl text-regular rounded-full border-2 relative transition-all
                ${
                  isCompleted
                    ? 'bg-blue-100 border-[#1351B4] text-[#1351B4]'
                    : isCurrent
                      ? 'bg-[#1351B4] border-[#1351B4] text-white'
                      : 'border-gray-400 text-gray-600 bg-white'
                }
              `}
            >
              {step.id}

              {isCompleted && (
                <span className="absolute -top-1.5 -right-1.5 bg-green-600 text-white rounded-full p-0.5">
                  <Check className="w-4 h-4" strokeWidth={3} />
                </span>
              )}
            </div>

            {/* Label para telas grandes */}
            <span
              className={`absolute md:top-[38px] lg:top-[60px] text-center hidden lg:block text-sm lg:text-base ${
                isCurrent
                  ? 'text-[#1351B4] font-semibold'
                  : isCompleted
                    ? 'text-gray-500'
                    : 'text-black'
              }`}
            >
              {step.label}
            </span>

            {/* Label para tablet */}
            <span
              className={`absolute top-[38px] hidden md:block lg:hidden text-center ${
                isCurrent
                  ? 'text-[#1351B4] font-semibold'
                  : isCompleted
                    ? 'text-gray-500'
                    : 'text-black'
              }`}
            >
              {step.mobileLabel}
            </span>

            {/* Mobile label flutuante */}
            {isCurrent && (
              <span className="absolute top-[44px] text-regular md:hidden text-center text-[#1351B4] font-semibold">
                {step.mobileLabel}
              </span>
            )}
          </div>
        );
      })}
    </div>
  );
}
