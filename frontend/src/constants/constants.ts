// Opções para dropdowns (o que o usuário vê)
export const PROFILE_OPTIONS = [
  { label: 'Admin', value: 'ADMIN' },
  { label: 'Coordenador', value: 'COORDINATOR' },
  { label: 'Supervisor', value: 'SUPERVISOR' },
  { label: 'Advisor', value: 'ADVISOR' },
  { label: 'Estudante', value: 'STUDENT' },
  { label: 'Banking', value: 'BANKING' },
  { label: 'Biblioteca', value: 'LIBRARY' }
];

export const STATUS_OPTIONS = [
  { label: 'Ativo', value: 'ACTIVE' },
  { label: 'Inativo', value: 'INACTIVE' }
];

// O grande problema: mapear string (GET) para número (PUT)
export const SHIFT_MAP = {
  // Chave (string do GET) -> Valor (número para o PUT)
  MORNING: 1,
  AFTERNOON: 2,
  DAYTIME: 3
};

// Mapeamento reverso para exibir no dropdown
export const SHIFT_OPTIONS = [
  { label: 'Manhã', value: SHIFT_MAP.MORNING }, // value: 1
  { label: 'Tarde', value: SHIFT_MAP.AFTERNOON }, // value: 2
  { label: 'Integral', value: SHIFT_MAP.DAYTIME } // value: 3
];
