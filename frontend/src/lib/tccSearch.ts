export interface SearchableTcc {
  tccId: number;
  studanteNames: string[];
  title?: string | null;
  advisorUserId?: number | null;
}

export function filterTccs<T extends SearchableTcc>(
  tccs: T[],
  query: string,
  advisorNames: Record<number, string>
): T[] {
  const normalize = (value: string) =>
    value
      .normalize('NFD')
      .replace(/[\u0300-\u036f]/g, '')
      .toLocaleLowerCase('pt-BR');
  const terms = normalize(query.trim()).split(/\s+/).filter(Boolean);
  if (!terms.length) return tccs;

  return tccs.filter((tcc) => {
    const text = normalize(
      [
        ...tcc.studanteNames,
        tcc.title ?? '',
        tcc.advisorUserId ? (advisorNames[tcc.advisorUserId] ?? '') : ''
      ].join(' ')
    );
    return terms.every((term) => text.includes(term));
  });
}
