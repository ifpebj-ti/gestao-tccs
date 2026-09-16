'use client';

import { Label } from '@/components/ui/label';

interface BankingInfo {
  members: { name: string; email: string; role: string }[];
}

interface BankingInfoSectionProps {
  bankingData: BankingInfo | null;
}

export function BankingInfoSection({
  bankingData,
}: BankingInfoSectionProps) {
  const hasBanking = bankingData && bankingData.members && bankingData.members.length > 0;

  return (
    <section>
      <h2 className="text-lg font-extrabold uppercase">Informações da Banca</h2>

      {hasBanking ? (
        <div className="grid md:grid-cols-2 gap-4 mt-4">
          {bankingData.members.map((member, index) => (
            <div key={index} className="border p-4 rounded-md bg-gray-50 flex flex-col gap-2">
              <div className="grid items-center gap-1.5">
                <Label className="font-semibold text-xs text-gray-500 uppercase">Membro da Banca</Label>
                <span className="font-medium">{member.name}</span>
              </div>
              <div className="grid items-center gap-1.5">
                <Label className="font-semibold text-xs text-gray-500 uppercase">E-mail</Label>
                <span>{member.email}</span>
              </div>
              <div className="grid items-center gap-1.5">
                <Label className="font-semibold text-xs text-gray-500 uppercase">Instituição/Papel</Label>
                <span>{member.role}</span>
              </div>
            </div>
          ))}
        </div>
      ) : (
        <p className="text-gray-600 italic mt-4">
          Aguardando cadastro da banca.
        </p>
      )}
    </section>
  );
}
