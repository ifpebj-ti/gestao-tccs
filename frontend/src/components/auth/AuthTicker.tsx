import React from 'react';

export function AuthTicker() {
  return (
    <div
      style={{
        background: '#002B18',
        color: '#C8FF47',
        padding: '0.75rem 0',
        overflow: 'hidden',
        whiteSpace: 'nowrap',
        borderTop: '2px solid #C8FF47',
        display: 'flex',
        alignItems: 'center'
      }}
    >
      <div
        style={{
          display: 'inline-flex',
          alignItems: 'center',
          gap: '2.5rem',
          fontSize: '0.8rem',
          fontWeight: 700,
          letterSpacing: '0.12em',
          textTransform: 'uppercase',
          width: '100%',
          justifyContent: 'space-around'
        }}
      >
        <span>GESTÃO DE TCCs</span>
        <span style={{ color: '#C8FF47' }}>✶</span>
        <span>PROPOSTAS & PROJETOS</span>
        <span style={{ color: '#C8FF47' }}>✶</span>
        <span>ORIENTAÇÃO ACADÊMICA</span>
        <span style={{ color: '#C8FF47' }}>✶</span>
        <span>BANCAS EXAMINADORAS</span>
        <span style={{ color: '#C8FF47' }}>✶</span>
        <span>DEFESA PÚBLICA</span>
        <span style={{ color: '#C8FF47' }}>✶</span>
        <span>FLUXO 100% DIGITAL</span>
      </div>
    </div>
  );
}