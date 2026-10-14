import { Check } from 'lucide-react';
const steps = [
  'Cadastro de proposta',
  'Início e organização',
  'Desenvolvimento e acompanhamento',
  'Preparação para apresentação',
  'Apresentação e avaliação',
  'Finalização e publicação'
];
export default function Step({ currentStep }: { currentStep: number }) {
  return (
    <section aria-label="Etapas do TCC" className="my-6">
      <p className="mb-4 text-sm text-[#002B18]">
        Etapa {currentStep} de 6{' '}
        <span className="font-semibold">· {steps[currentStep - 1]}</span>
      </p>
      <ol className="grid grid-cols-6 gap-2">
        {steps.map((label, index) => {
          const done = index + 1 < currentStep;
          const active = index + 1 === currentStep;
          return (
            <li
              key={label}
              aria-current={active ? 'step' : undefined}
              className="min-w-0"
            >
              <div
                className={`mb-3 h-1.5 rounded-full ${done ? 'bg-[#006633]' : active ? 'bg-[#C8FF47]' : 'bg-[#DDD9CD]'}`}
              />
              <span
                className={`inline-flex size-7 items-center justify-center rounded-full text-xs font-bold ${active ? 'bg-[#002B18] text-[#C8FF47]' : done ? 'text-[#006633]' : 'text-gray-500'}`}
                aria-hidden="true"
              >
                {done ? <Check size={16} /> : index + 1}
              </span>
              <span
                className={`mt-2 hidden text-xs leading-relaxed md:block ${active ? 'font-semibold text-[#002B18]' : 'text-gray-500'}`}
              >
                {label}
              </span>
              <span className="sr-only">
                {label}: {done ? 'concluída' : active ? 'atual' : 'próxima'}
              </span>
            </li>
          );
        })}
      </ol>
    </section>
  );
}
