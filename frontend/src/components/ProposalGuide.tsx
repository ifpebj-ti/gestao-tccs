import { GraduationCap } from 'lucide-react';

export function ProposalGuide({ isStudent }: { isStudent: boolean }) {
  const tips = [
    'Use um título direto e específico.',
    'Resuma o problema, o objetivo e a abordagem.',
    isStudent
      ? 'Revise as informações antes de enviar.'
      : 'Confira os e-mails e cursos dos estudantes.'
  ];

  return (
    <aside
      aria-label="Dicas para a proposta"
      className="h-fit rounded-xl bg-[#183E2D] p-7 text-[#EFF2E8] lg:sticky lg:top-6"
    >
      <div className="mb-6">
        <GraduationCap
          className="size-7 text-[#C8FF47]"
          strokeWidth={1.3}
          aria-hidden="true"
        />
      </div>
      <h2 className="text-lg font-semibold">Escreva com clareza.</h2>
      <ul className="mt-4 divide-y divide-white/15">
        {tips.map((tip, index) => (
          <li
            key={tip}
            className="flex items-start gap-3 py-4 text-sm leading-relaxed"
          >
            <span className="text-[10px] text-[#C8FF47]" aria-hidden="true">
              0{index + 1}
            </span>
            {tip}
          </li>
        ))}
      </ul>
    </aside>
  );
}
