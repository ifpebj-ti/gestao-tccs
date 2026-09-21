import { useState, useMemo } from 'react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import { toast } from 'react-toastify';
import { env } from 'next-runtime-env';
import { CheckCircle2, FileText, Mic, BookOpen } from 'lucide-react';
import Image from 'next/image';

interface AvaliacaoFormProps {
  token?: string | null;
  onSuccessCallback?: () => void;
  hideLogoAndMinHeight?: boolean;
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

  const [comments, setComments] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [success, setSuccess] = useState(false);

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

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!token) {
      toast.error('Token não encontrado na URL.');
      return;
    }

    if (totalOral > 10 || totalTextual > 10) {
      toast.error('O total parcial não pode ultrapassar 10 pontos.');
      return;
    }

    const evaluationDetails = JSON.stringify({ oral: oralGrades, textual: textualGrades });

    setIsSubmitting(true);
    try {
      const res = await fetch(`${API_URL}/Tcc/evaluate`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({ 
          token, 
          grade: parseFloat(finalGrade.toFixed(2)), 
          evaluationComments: comments,
          evaluationDetails
        })
      });

      if (!res.ok) {
        const errorData = await res.json().catch(() => null);
        throw new Error(errorData?.title || 'Erro ao enviar a avaliação.');
      }

      setSuccess(true);
      if (onSuccessCallback) {
        onSuccessCallback();
      }
    } catch (err: unknown) {
      const errorMessage = err instanceof Error ? err.message : 'Erro ao enviar a avaliação.';
      toast.error(errorMessage);
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
      <div className={hideLogoAndMinHeight ? "w-full p-4" : "min-h-screen flex items-center justify-center bg-gray-50 p-4"}>
        <div className="w-full bg-white p-8 rounded-xl shadow-md text-center max-w-md mx-auto">
          <h1 className="text-2xl font-bold text-red-600 mb-4">Acesso Negado</h1>
          <p className="text-gray-600">O link de avaliação é inválido ou está ausente.</p>
        </div>
      </div>
    );
  }

  if (success && !onSuccessCallback) {
    // Se tiver callback de success, não renderiza a tela de sucesso, deixa quem chamou tratar
    return (
      <div className={hideLogoAndMinHeight ? "w-full p-4" : "min-h-screen flex flex-col items-center justify-center bg-gray-50 p-4"}>
        <div className="w-full bg-white p-10 rounded-xl shadow-md text-center flex flex-col items-center max-w-md mx-auto">
          <CheckCircle2 className="w-20 h-20 text-green-500 mb-6" />
          <h1 className="text-2xl font-bold text-gray-800 mb-4">Avaliação Concluída!</h1>
          <p className="text-gray-600">A sua nota e o seu parecer foram registrados com sucesso. Muito obrigado pela sua contribuição!</p>
        </div>
      </div>
    );
  } else if (success && onSuccessCallback) {
    // Quando onSuccessCallback existe, depois de renderizar, já chamou o callback no handle.
    // Pode retornar null para sumir ou a mesma tela verde resumida.
    return (
      <div className="w-full bg-white p-10 rounded-xl text-center flex flex-col items-center max-w-md mx-auto">
        <CheckCircle2 className="w-16 h-16 text-green-500 mb-4" />
        <h1 className="text-xl font-bold text-gray-800 mb-2">Avaliação Concluída!</h1>
      </div>
    );
  }

  return (
    <div className={hideLogoAndMinHeight ? "w-full" : "min-h-screen flex flex-col items-center py-10 bg-gray-100 px-4"}>
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
      <div className={`w-full bg-white p-6 md:p-10 rounded-2xl ${hideLogoAndMinHeight ? '' : 'max-w-4xl shadow-xl'}`}>
        <div className="text-center mb-10">
          <h1 className="text-3xl md:text-4xl font-extrabold text-gray-900 mb-3 tracking-tight">Ficha Avaliativa de TCC</h1>
          <p className="text-gray-500 text-sm md:text-base max-w-2xl mx-auto">Preencha as notas parciais abaixo baseadas no Anexo IV. A nota final será calculada automaticamente.</p>
        </div>

        <form onSubmit={handleSubmit} className="flex flex-col gap-10">
          
          {/* Seção 1: Apresentação Oral */}
          <div className="rounded-xl border border-blue-100 bg-blue-50/30 overflow-hidden shadow-sm">
            <div className="bg-blue-600 px-6 py-4 flex items-center">
              <Mic className="w-6 h-6 mr-3 text-blue-50" />
              <h2 className="text-xl font-bold text-white">Critérios: Apresentação Oral</h2>
            </div>
            
            <div className={`p-6 grid grid-cols-1 ${hideLogoAndMinHeight ? '' : 'md:grid-cols-2'} gap-4`}>
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
                <div key={k} className="flex flex-row justify-between items-center gap-3 bg-white p-4 rounded-lg border shadow-sm hover:border-blue-300 transition-colors">
                  <Label className="text-sm font-semibold text-slate-700 flex-1">{item.label} <span className="block text-xs text-slate-400 mt-1 font-normal">(0 a {item.max.toString().replace('.', ',')})</span></Label>
                  <Input type="number" step="0.1" required className="w-20 shrink-0 text-center font-bold text-lg bg-slate-50 focus:bg-white" placeholder="0.0" value={oralGrades[k]} onChange={e => handleOralChange(k, e.target.value, item.max)} />
                </div>
              )})}
              <div className={`flex flex-row justify-between items-center gap-3 bg-white p-4 rounded-lg border shadow-sm hover:border-blue-300 transition-colors ${hideLogoAndMinHeight ? '' : 'md:col-span-2'}`}>
                <Label className="text-sm font-semibold text-slate-700 flex-1">VII. Articulação (oral e escrita) <span className="block text-xs text-slate-400 mt-1 font-normal">(0 a 1,5)</span></Label>
                <Input type="number" step="0.1" required className="w-24 shrink-0 text-center font-bold text-lg bg-slate-50 focus:bg-white" placeholder="0.0" value={oralGrades.articulacao} onChange={e => handleOralChange('articulacao', e.target.value, 1.5)} />
              </div>
            </div>
            
            <div className="bg-blue-100/50 px-6 py-4 flex justify-between items-center border-t border-blue-100">
              <span className="font-bold text-blue-900 uppercase tracking-wide text-sm">Total Parcial (Oral)</span>
              <div className="flex items-baseline gap-1 text-blue-700">
                <span className="text-2xl font-black">{totalOral.toFixed(2)}</span>
                <span className="text-sm font-bold text-blue-500">/ 10</span>
              </div>
            </div>
          </div>

          {/* Seção 2: Produção Textual */}
          <div className="rounded-xl border border-emerald-100 bg-emerald-50/30 overflow-hidden shadow-sm">
            <div className="bg-emerald-600 px-6 py-4 flex items-center">
              <FileText className="w-6 h-6 mr-3 text-emerald-50" />
              <h2 className="text-xl font-bold text-white">Critérios: Produção Textual</h2>
            </div>
            
            <div className={`p-6 grid grid-cols-1 ${hideLogoAndMinHeight ? '' : 'md:grid-cols-2 lg:grid-cols-3'} gap-4`}>
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
                <div key={k} className="flex flex-row justify-between items-center gap-3 bg-white p-4 rounded-lg border shadow-sm hover:border-emerald-300 transition-colors">
                  <Label className="text-sm font-semibold text-slate-700 flex-1">{item.label} <span className="block text-xs text-slate-400 mt-1 font-normal">(0 a {item.max.toString().replace('.', ',')})</span></Label>
                  <Input type="number" step="0.1" required className="w-20 shrink-0 text-center font-bold text-lg bg-slate-50 focus:bg-white" placeholder="0.0" value={textualGrades[k]} onChange={e => handleTextualChange(k, e.target.value, item.max)} />
                </div>
              )})}
            </div>

            <div className="bg-emerald-100/50 px-6 py-4 flex justify-between items-center border-t border-emerald-100">
              <span className="font-bold text-emerald-900 uppercase tracking-wide text-sm">Total Parcial (Textual)</span>
              <div className="flex items-baseline gap-1 text-emerald-700">
                <span className="text-2xl font-black">{totalTextual.toFixed(2)}</span>
                <span className="text-sm font-bold text-emerald-500">/ 10</span>
              </div>
            </div>
          </div>

          {/* Resultado Final */}
          <div className="bg-slate-900 text-white rounded-2xl p-8 flex flex-col items-center justify-center shadow-lg relative overflow-hidden">
            <div className="absolute -right-6 -top-6 opacity-10">
              <BookOpen className="w-48 h-48" />
            </div>
            <span className="text-slate-400 font-bold uppercase tracking-widest text-xs mb-2 z-10">Nota Final do TCC</span>
            <span className="text-6xl font-black text-amber-400 drop-shadow-md z-10">{finalGrade.toFixed(2)}</span>
            <span className="text-slate-400 text-sm mt-3 font-medium z-10">Média aritmética entre Oral e Textual</span>
          </div>

          <div className="grid gap-3">
            <Label htmlFor="comments" className="text-lg font-bold text-slate-800">Parecer Final / Considerações <span className="text-slate-400 font-normal text-sm ml-2">(Opcional)</span></Label>
            <Textarea
              id="comments"
              placeholder="Escreva aqui o seu parecer final descritivo sobre o trabalho do estudante..."
              value={comments}
              onChange={(e) => setComments(e.target.value)}
              className="min-h-[140px] bg-white border-slate-300 focus:border-blue-500 text-base p-4 rounded-xl shadow-sm resize-y"
            />
          </div>

          <Button type="submit" size="lg" disabled={isSubmitting} className="w-full text-lg mt-4 py-7 font-bold rounded-xl shadow-md hover:shadow-lg transition-all">
            <BookOpen className="w-6 h-6 mr-3" />
            {isSubmitting ? 'Registrando Avaliação...' : 'Registrar Avaliação Oficial'}
          </Button>
        </form>
      </div>
    </div>
  );
}
