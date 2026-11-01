import { PageTitle } from '@/components/layout/PageTitle';
import { SectionTitle } from '@/components/layout/SectionTitle';
import { useState, useMemo, useEffect } from 'react';
import { Button } from '@/components/ui/button';
import { GradeField } from '@/components/evaluation/GradeField';
import { toast } from 'react-toastify';
import { env } from 'next-runtime-env';
import { CheckCircle2, FileText, Mic, BookOpen } from 'lucide-react';
import Image from 'next/image';
import { BankingSignatures, FindAllPendingSignatureDTO } from './BankingSignatures';
import { jwtDecode } from 'jwt-decode';
interface AvaliacaoFormProps {
  token?: string | null;
  onSuccessCallback?: () => void;
  hideLogoAndMinHeight?: boolean;
}

interface BackendDocumentDTO {
  documentId: number;
  documentName: string;
  userDetails?: { idDocumentOwner: number | null }[];
}

interface BackendSignatureDTO {
  tccId: number;
  studentNames: string[];
  pendingDetails?: BackendDocumentDTO[];
  documents?: BackendDocumentDTO[];
}

export function AvaliacaoForm({ token, onSuccessCallback, hideLogoAndMinHeight = false }: AvaliacaoFormProps) {
  const API_URL = env('NEXT_PUBLIC_API_URL');

  // Oral Grades
  const [oralGrades, setOralGrades] = useState({
    postura: '',
    usoTempo: '',
    usoAudiovisual: '',
    dominioAssunto: '',
    clarezaComunicacao: '',
    exposicaoIdeias: '',
    articulacao: ''
  });

  // Textual Grades
  const [textualGrades, setTextualGrades] = useState({
    relevanciaTema: '',
    clarezaObjetividade: '',
    coerencia: '',
    desenvolvimento: '',
    originalidade: '',
    conteudoCientifico: '',
    referencias: '',
    conclusoes: '',
    normatizacao: ''
  });

  const [isSubmitting, setIsSubmitting] = useState(false);
  const [success, setSuccess] = useState(false);
  const [pendingSignaturesCount, setPendingSignaturesCount] = useState(0);
  const [pendingSignaturesData, setPendingSignaturesData] = useState<FindAllPendingSignatureDTO[]>([]);
  const [bankingJwt, setBankingJwt] = useState('');
  const [evaluatedTccId, setEvaluatedTccId] = useState<number | null>(null);
  const [refetchTrigger, setRefetchTrigger] = useState(0);

  useEffect(() => {
    let intervalId: NodeJS.Timeout;

    const checkDocs = async () => {
      if (!success || !token) return;
      try {
         const authRes = await fetch(`${API_URL}/Auth/banking-login?token=${token}`, { method: 'POST' });
         if (authRes.ok) {
           const { accessToken } = await authRes.json();
           setBankingJwt(accessToken);
           const decoded = jwtDecode<{ userId: string }>(accessToken);
           const userId = decoded.userId;
           const sigRes = await fetch(`${API_URL}/Signature/pending?userId=${userId}`, {
             headers: { 'Authorization': `Bearer ${accessToken}` }
           });
            if (sigRes.ok) {
             const sigs = await sigRes.json();
             const mappedSigs = sigs.map((s: BackendSignatureDTO) => ({
               tccId: s.tccId,
               studentNames: s.studentNames,
               documents: (s.pendingDetails || s.documents || []).map((doc: BackendDocumentDTO) => ({
                 documentId: doc.documentId,
                 documentName: doc.documentName,
                 studentId: doc.userDetails && doc.userDetails.length > 0 ? doc.userDetails[0].idDocumentOwner : null
               }))
             }));
             const filteredSigs = evaluatedTccId !== null 
               ? mappedSigs.filter((s: BackendSignatureDTO) => s.tccId === evaluatedTccId)
               : mappedSigs;
             const count = filteredSigs.reduce((acc: number, curr: { documents: unknown[] }) => acc + curr.documents.length, 0);
             setPendingSignaturesData(filteredSigs);
             setPendingSignaturesCount(count);

             // Se encontrou assinaturas, não precisa continuar fazendo polling
             if (count > 0 && intervalId) {
                clearInterval(intervalId);
             }
           }
         }
      } catch (err) {
        console.error("Erro ao buscar documentos pendentes:", err);
      }
    };

    if (success && token) {
      checkDocs();
      intervalId = setInterval(checkDocs, 10000);
    }

    return () => {
      if (intervalId) clearInterval(intervalId);
    };
  }, [success, token, API_URL, refetchTrigger, evaluatedTccId]);

  // Helper to parse grades
  const parseGrade = (val: string) => {
    const num = parseFloat(val);
    return isNaN(num) ? 0 : num;
  };

  const totalOral = useMemo(() => {
    return Object.values(oralGrades).reduce((acc, curr) => acc + parseGrade(curr), 0);
  }, [oralGrades]);

  const totalTextual = useMemo(() => {
    return Object.values(textualGrades).reduce((acc, curr) => acc + parseGrade(curr), 0);
  }, [textualGrades]);

  const finalGrade = useMemo(() => {
    return (totalOral + totalTextual) / 2;
  }, [totalOral, totalTextual]);

  const validateGrades = () => {
    for (const [, val] of Object.entries(oralGrades)) {
      if (val === '') return false;
    }
    for (const [, val] of Object.entries(textualGrades)) {
      if (val === '') return false;
    }
    return true;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!token) {
      toast.error('Token não encontrado na URL.');
      return;
    }

    if (!validateGrades()) {
      toast.error('Por favor, preencha todas as notas antes de enviar.');
      return;
    }

    if (totalOral > 10 || totalTextual > 10) {
      toast.error('O total parcial não pode ultrapassar 10 pontos.');
      return;
    }

    const evaluationDetails = JSON.stringify({ oral: oralGrades, textual: textualGrades });
    const autoParecer = finalGrade >= 7 ? 'Aprovado' : 'Reprovado';

    try {
      setIsSubmitting(true);

      const res = await fetch(`${API_URL}/Tcc/evaluate`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          token,
          grade: parseFloat(finalGrade.toFixed(2)),
          evaluationComments: autoParecer,
          evaluationDetails
        })
      });

      if (!res.ok) {
        throw new Error('Falha ao enviar a avaliação');
      }

      const data = await res.json();
      if (data && data.tccId) {
        setEvaluatedTccId(data.tccId);
      }

      toast.success('Avaliação enviada com sucesso!');
      setSuccess(true);
      if (onSuccessCallback) {
        onSuccessCallback();
      }
    } catch (error) {
      toast.error('Ocorreu um erro ao enviar a avaliação. Tente novamente.');
      console.error(error);
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleOralChange = (key: keyof typeof oralGrades, value: string, max: number) => {
    const num = parseFloat(value);
    if (value !== '' && (isNaN(num) || num < 0 || num > max)) return;
    setOralGrades(prev => ({ ...prev, [key]: value }));
  };

  const handleTextualChange = (key: keyof typeof textualGrades, value: string, max: number) => {
    const num = parseFloat(value);
    if (value !== '' && (isNaN(num) || num < 0 || num > max)) return;
    setTextualGrades(prev => ({ ...prev, [key]: value }));
  };

  if (!token) {
    return (
      <div className={hideLogoAndMinHeight ? 'w-full p-4' : 'min-h-screen flex items-center justify-center bg-[#FAF9F5] p-4'}>
        <div className="w-full py-8 text-center max-w-md mx-auto">
          <PageTitle className="mb-4">Acesso Negado</PageTitle>
          <p className="text-gray-600">O link de avaliação é inválido ou está ausente.</p>
        </div>
      </div>
    );
  }

  if (success && !onSuccessCallback) {
    return (
      <div className={hideLogoAndMinHeight ? 'w-full p-4' : 'min-h-screen flex flex-col items-center py-12 bg-[#FAF9F5] p-4'}>
        <div className="w-full py-10 text-center flex flex-col items-center max-w-2xl mx-auto">
          <div className="w-20 h-20 bg-green-100 text-green-600 rounded-full flex items-center justify-center mb-6 shadow-sm">
            <CheckCircle2 className="w-12 h-12" />
          </div>
          <PageTitle className="mb-3">Avaliação Registrada com Sucesso!</PageTitle>
          <p className="text-gray-600 text-base max-w-md">As notas foram computadas com êxito na ata de defesa do TCC.</p>
          
          {pendingSignaturesCount > 0 ? (
            <div className="w-full mt-6">
              <BankingSignatures 
                signatures={pendingSignaturesData} 
                jwt={bankingJwt} 
                onSuccess={() => setRefetchTrigger(prev => prev + 1)} 
              />
            </div>
          ) : (
            <div className="mt-8 py-8 border-t border-[#ECEAE2] w-full text-center flex flex-col items-center">
              <div className="w-14 h-14 rounded-full bg-[#E8F3EC] text-[#006633] flex items-center justify-center mb-4">
                <BookOpen className="w-7 h-7" />
              </div>
              <SectionTitle className="mb-2">Muito Obrigado por sua Contribuição!</SectionTitle>
              <p className="text-slate-600 text-sm md:text-base max-w-lg leading-relaxed">
                Sua avaliação técnica foi registrada com sucesso no sistema institucional. Agradecemos imensamente sua dedicação e contribuição para a formação acadêmica do discente e a excelência do nosso corpo de avaliadores.
              </p>
              <div className="mt-5 inline-flex items-center gap-2 text-xs font-semibold text-[#002B18] bg-[#E8F3EC] px-3.5 py-1.5 rounded-full border border-[#DDD9CD]">
                <CheckCircle2 className="w-4 h-4 text-[#006633]" /> Avaliação Oficial Homologada
              </div>
            </div>
          )}
        </div>
      </div>
    );
  } else if (success && onSuccessCallback) {
    // Quando onSuccessCallback existe, depois de renderizar, já chamou o callback no handle.
    // Pode retornar null para sumir ou a mesma tela verde resumida.
    return (
      <div className="w-full py-10 text-center flex flex-col items-center max-w-md mx-auto">
        <CheckCircle2 className="w-16 h-16 text-green-500 mb-4" />
        <PageTitle className="mb-2">Avaliação Concluída!</PageTitle>
      </div>
    );
  }

  return (
    <div className={hideLogoAndMinHeight ? 'w-full' : 'min-h-screen flex flex-col items-center py-10 bg-[#FAF9F5] px-4'}>
      {!hideLogoAndMinHeight && (
        <div className="mb-8">
          <Image
            src="/images/logo.png"
            alt="Logo Gestão TCCs"
            width={150}
            height={60}
            priority
          />
        </div>
      )}
      <div className={`w-full py-6 md:py-10 ${hideLogoAndMinHeight ? '' : 'max-w-4xl'}`}>
        <div className="text-center mb-10">
          <PageTitle className="mb-3">Ficha Avaliativa de TCC</PageTitle>
          <p className="text-gray-500 text-sm md:text-base max-w-2xl mx-auto">Preencha as notas parciais abaixo baseadas no Anexo IV. A nota final será calculada automaticamente.</p>
        </div>

        <form onSubmit={handleSubmit} className="flex flex-col gap-10">
          
          {/* Seção 1: Apresentação Oral */}
          <div>
            <div className="flex items-center gap-3 text-[#006633]">
              <Mic className="w-5 h-5" aria-hidden="true" />
              <SectionTitle >Critérios: Apresentação Oral</SectionTitle>
            </div>
            
            <div className={`py-4 grid grid-cols-1 ${hideLogoAndMinHeight ? '' : 'md:grid-cols-2'} gap-x-8`}>
              {[
                { key: 'postura', label: 'I. Postura do/a estudante', max: 1 },
                { key: 'usoTempo', label: 'II. Uso adequado do tempo', max: 0.5 },
                { key: 'usoAudiovisual', label: 'III. Uso de recursos audiovisuais', max: 1 },
                { key: 'dominioAssunto', label: 'IV. Domínio e segurança', max: 2 },
                { key: 'clarezaComunicacao', label: 'V. Clareza na comunicação', max: 2 },
                { key: 'exposicaoIdeias', label: 'VI. Exposição das ideias', max: 2 },
              ].map(item => {
                const k = item.key as keyof typeof oralGrades;
                return (
                <GradeField key={k} label={item.label} max={item.max} value={oralGrades[k]} onChange={(value) => handleOralChange(k, value, item.max)} />
              )})}
              <div className={hideLogoAndMinHeight ? '' : 'md:col-span-2'}>
                <GradeField label="VII. Articulação (oral e escrita)" max={1.5} value={oralGrades.articulacao} onChange={(value) => handleOralChange('articulacao', value, 1.5)} />
              </div>
            </div>
            
            <div className="py-4 flex justify-between items-center border-b border-[#DDD9CD]">
              <span className="font-bold text-[#002B18] uppercase tracking-wide text-xs">Total Parcial (Oral)</span>
              <div className="flex items-baseline gap-1 text-[#002B18]">
                <span className="text-2xl font-black">{totalOral.toFixed(2)}</span>
                <span className="text-sm font-bold text-gray-500">/ 10</span>
              </div>
            </div>
          </div>

          {/* Seção 2: Produção Textual */}
          <div>
            <div className="flex items-center gap-3 text-[#006633]">
              <FileText className="w-5 h-5" aria-hidden="true" />
              <SectionTitle >Critérios: Produção Textual</SectionTitle>
            </div>
            
            <div className={`py-4 grid grid-cols-1 ${hideLogoAndMinHeight ? '' : 'md:grid-cols-2'} gap-x-8`}>
              {[
                { key: 'relevanciaTema', label: 'I. Relevância do tema', max: 1 },
                { key: 'clarezaObjetividade', label: 'II. Clareza e objetividade', max: 1 },
                { key: 'coerencia', label: 'III. Coerência', max: 1 },
                { key: 'desenvolvimento', label: 'IV. Desenvolvimento', max: 2 },
                { key: 'originalidade', label: 'V. Originalidade', max: 1 },
                { key: 'conteudoCientifico', label: 'VI. Conteúdo científico', max: 1 },
                { key: 'referencias', label: 'VII. Referências', max: 1 },
                { key: 'conclusoes', label: 'VIII. Conclusões', max: 1 },
                { key: 'normatizacao', label: 'IX. Normatização', max: 1 },
              ].map(item => {
                const k = item.key as keyof typeof textualGrades;
                return (
                <GradeField key={k} label={item.label} max={item.max} value={textualGrades[k]} onChange={(value) => handleTextualChange(k, value, item.max)} />
              )})}
            </div>

            <div className="py-4 flex justify-between items-center border-b border-[#DDD9CD]">
              <span className="font-bold text-[#002B18] uppercase tracking-wide text-xs">Total Parcial (Textual)</span>
              <div className="flex items-baseline gap-1 text-emerald-700">
                <span className="text-2xl font-black">{totalTextual.toFixed(2)}</span>
                <span className="text-sm font-bold text-emerald-500">/ 10</span>
              </div>
            </div>
          </div>

          {/* Resultado Final */}
          <div className="bg-[#002B18] text-white rounded-lg p-8 flex flex-col items-center justify-center relative overflow-hidden">
            <div className="absolute -right-6 -top-6 opacity-10">
              <BookOpen className="w-48 h-48" />
            </div>
            <span className="text-slate-400 font-bold uppercase tracking-widest text-xs mb-2 z-10">Nota Final do TCC</span>
            <span className="font-[family-name:var(--font-poppins)] text-6xl font-extrabold text-[#C8FF47] z-10">{finalGrade.toFixed(2)}</span>
            <span className="text-slate-400 text-sm mt-3 font-medium z-10">Média aritmética entre Oral e Textual</span>
            <div className="mt-4 z-10">
              <span className={`inline-flex items-center px-4 py-1.5 rounded-full text-sm font-bold ${
                finalGrade >= 7 ? 'bg-green-500/20 text-green-400 border border-green-500/30' : 'bg-red-500/20 text-red-400 border border-red-500/30'
              }`}>
                Parecer: {finalGrade >= 7 ? 'Aprovado' : 'Reprovado'}
              </span>
            </div>
          </div>

          <Button type="submit" size="lg" disabled={isSubmitting} className="w-full mt-4 py-7 font-bold">
            <BookOpen className="w-6 h-6 mr-3" />
            {isSubmitting ? 'Registrando Avaliação...' : 'Registrar Avaliação Oficial'}
          </Button>
        </form>
      </div>
    </div>
  );
}
