import React from 'react';

interface AcademicCapWatermarkProps {
  className?: string;
}

export const AcademicCapWatermark: React.FC<AcademicCapWatermarkProps> = ({
  className = 'w-96 h-96 text-white opacity-[0.06] pointer-events-none select-none',
}) => {
  return (
    <svg
      viewBox="0 0 200 200"
      fill="none"
      xmlns="http://www.w3.org/2000/svg"
      className={className}
      aria-hidden="true"
    >
      {/* Top Cap Diamond Rhombus */}
      <path
        d="M100 35L175 75L100 115L25 75L100 35Z"
        stroke="currentColor"
        strokeWidth="3.5"
        strokeLinejoin="round"
      />
      {/* Lower Skull Cap Arc */}
      <path
        d="M55 93V135C55 152 75 165 100 165C125 165 145 152 145 135V93"
        stroke="currentColor"
        strokeWidth="3.5"
        strokeLinecap="round"
      />
      {/* Tassel String and Pendant */}
      <path
        d="M100 115C100 115 110 130 112 145C113.5 156 114 165 114 175"
        stroke="currentColor"
        strokeWidth="2.5"
        strokeLinecap="round"
      />
      <rect
        x="110.5"
        y="168"
        width="7"
        height="14"
        rx="2"
        stroke="currentColor"
        strokeWidth="2"
      />
      {/* Fine Geometric Accent Ring */}
      <circle
        cx="100"
        cy="75"
        r="4.5"
        fill="currentColor"
      />
    </svg>
  );
};
