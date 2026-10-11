import React from 'react';

interface AuthEditorialPanelProps {
  title: string;
  description: string;
  stat1Value?: string;
  stat1Label?: string;
  stat2Value?: string;
  stat2Label?: string;
}

export function AuthEditorialPanel({
  title,
  description,
  stat1Value = '100%',
  stat1Label = 'Digital & Sem papel',
  stat2Value = 'Fluxo',
  stat2Label = 'Integrado ao Campus'
}: AuthEditorialPanelProps) {
  return (
    <div>
      <h1
        style={{
          fontSize: 'clamp(2.3rem, 3.8vw, 3.4rem)',
          fontWeight: 800,
          lineHeight: 1.1,
          letterSpacing: '-0.035em',
          color: '#002B18',
          margin: 0,
          marginBottom: '1.25rem'
        }}
      >
        {title}
      </h1>

      <p
        style={{
          fontSize: '0.95rem',
          lineHeight: 1.65,
          color: '#4B5563',
          maxWidth: '460px',
          margin: 0,
          marginBottom: '2.5rem'
        }}
      >
        {description}
      </p>

      <div style={{ display: 'flex', alignItems: 'center', gap: '2.5rem' }}>
        <div>
          <div
            style={{
              fontSize: '1.75rem',
              fontWeight: 800,
              color: '#002B18',
              letterSpacing: '-0.03em',
              lineHeight: 1
            }}
          >
            {stat1Value}
          </div>
          <div
            style={{
              fontSize: '0.8rem',
              color: '#6B7280',
              fontWeight: 500,
              marginTop: '0.35rem'
            }}
          >
            {stat1Label}
          </div>
        </div>

        <div style={{ width: '1px', height: '38px', background: '#DDD9CD' }} />

        <div>
          <div
            style={{
              fontSize: '1.75rem',
              fontWeight: 800,
              color: '#002B18',
              letterSpacing: '-0.03em',
              lineHeight: 1
            }}
          >
            {stat2Value}
          </div>
          <div
            style={{
              fontSize: '0.8rem',
              color: '#6B7280',
              fontWeight: 500,
              marginTop: '0.35rem'
            }}
          >
            {stat2Label}
          </div>
        </div>
      </div>
    </div>
  );
}