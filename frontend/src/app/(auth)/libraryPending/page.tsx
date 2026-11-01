'use client';

import { useEffect, useState, useCallback } from 'react';
import { useRouter } from 'next/navigation';
import Cookies from 'js-cookie';
import { jwtDecode } from 'jwt-decode';
import { toast } from 'react-toastify';
import { env } from 'next-runtime-env';
import {
  Library,
  Download,
  CheckCircle2,
  Clock,
  BookOpen,
  User,
  GraduationCap,
  LoaderCircle,
  Search
} from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { PageTitle } from '@/components/layout/PageTitle';
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle
} from '@/components/ui/alert-dialog';

interface DecodedToken {
  role: string | string[];
  userId: string;
}

interface LibraryPendingTccItem {
  tccId: number;
  title: string;
  studentNames: string[];
  advisorName: string;
  courseName: string;
  forwardedAt: string | null;
  finalTccFile: string | null;
  catalogCode?: string | null;
  catalogedAt?: string | null;
}

interface LibraryPendingData {
  pendingCount: number;
  catalogedCount: number;
  pendingCataloging: LibraryPendingTccItem[];
  recentlyCataloged: LibraryPendingTccItem[];
}

function formatDate(dateStr?: string | null): string {
  if (!dateStr) return 'Data não informada';
  const d = new Date(dateStr);
  if (isNaN(d.getTime())) return dateStr;
  return d.toLocaleDateString('pt-BR', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric'
  });
}

export default function LibraryPendingPage() {
  const API_URL = env('NEXT_PUBLIC_API_URL');
  const router = useRouter();

  const [loading, setLoading] = useState(true);
  const [data, setData] = useState<LibraryPendingData>({
    pendingCount: 0,
    catalogedCount: 0,
    pendingCataloging: [],
    recentlyCataloged: []
  });
  const [search, setSearch] = useState('');
  const [activeTab, setActiveTab] = useState<'pending' | 'cataloged'>('pending');

  // Modal de Homologação
  const [selectedTcc, setSelectedTcc] = useState<LibraryPendingTccItem | null>(null);
  const [catalogCode, setCatalogCode] = useState('');
  const [submitting, setSubmitting] = useState(false);

  const fetchPendingData = useCallback(async () => {
    const token = Cookies.get('token');
    if (!token) {
      router.push('/');
      return;
    }

    try {
      setLoading(true);
      const res = await fetch(`${API_URL}/Tcc/library-pending`, {
        headers: { Authorization: `Bearer ${token}` }
      });

      if (!res.ok) {
        if (res.status === 403 || res.status === 401) {
          router.push('/unauthorized');
          return;
        }
        throw new Error('Falha ao carregar fila da biblioteca.');
      }

      const result: LibraryPendingData = await res.json();
      setData(result);
    } catch (err: unknown) {
      toast.error(err instanceof Error ? err.message : 'Erro ao buscar dados.');
    } finally {
      setLoading(false);
    }
  }, [API_URL, router]);

  useEffect(() => {
    const token = Cookies.get('token');
    if (!token) {
      router.push('/');
      return;
    }
    try {
      const decoded = jwtDecode<DecodedToken>(token);
      const roles = Array.isArray(decoded.role) ? decoded.role : [decoded.role];
      if (!roles.includes('LIBRARY') && !roles.includes('ADMIN')) {
        router.push('/unauthorized');
        return;
      }
    } catch {
      router.push('/');
      return;
    }

    fetchPendingData();
  }, [fetchPendingData, router]);

  const handleDownloadPdf = async (tccId: number) => {
    const token = Cookies.get('token');
    if (!token) return;

    try {
      const res = await fetch(`${API_URL}/Tcc/${tccId}/final-file/download`, {
        headers: { Authorization: `Bearer ${token}` }
      });
      if (!res.ok) {
        toast.error('Arquivo final não disponível para download.');
        return;
      }
      const blob = await res.blob();
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `TCC_${tccId}_Versao_Final.pdf`;
      document.body.appendChild(a);
      a.click();
      a.remove();
      window.URL.revokeObjectURL(url);
    } catch {
      toast.error('Erro ao baixar o arquivo final.');
    }
  };

  const handleConfirmCataloging = async () => {
    if (!selectedTcc) return;
    const trimmed = catalogCode.trim();
    if (!trimmed) {
      toast.warn('Por favor, informe o código de catalogação.');
      return;
    }

    const token = Cookies.get('token');
    if (!token) return;

    try {
      setSubmitting(true);
      const res = await fetch(`${API_URL}/Tcc/${selectedTcc.tccId}/confirm-library`, {
        method: 'PATCH',
        headers: {
          'Content-Type': 'application/json',
          Authorization: `Bearer ${token}`
        },
        body: JSON.stringify({ libraryCatalogCode: trimmed })
      });

      if (!res.ok) {
        const errorData = await res.json().catch(() => null);
        throw new Error(
          errorData?.detail ||
            errorData?.message ||
            'Não foi possível confirmar a homologação no acervo.'
        );
      }

      toast.success('TCC homologado e incluído no acervo da biblioteca com sucesso!');
      setSelectedTcc(null);
      setCatalogCode('');
      await fetchPendingData();
    } catch (err: unknown) {
      toast.error(err instanceof Error ? err.message : 'Erro ao homologar TCC.');
    } finally {
      setSubmitting(false);
    }
  };

  const filterItems = (items: LibraryPendingTccItem[]) => {
    if (!search.trim()) return items;
    const q = search.toLowerCase();
    return items.filter(
      (item) =>
        item.title.toLowerCase().includes(q) ||
        item.advisorName?.toLowerCase().includes(q) ||
        item.courseName?.toLowerCase().includes(q) ||
        item.catalogCode?.toLowerCase().includes(q) ||
        item.studentNames.some((s) => s.toLowerCase().includes(q))
    );
  };

  const pendingList = filterItems(data.pendingCataloging);
  const catalogedList = filterItems(data.recentlyCataloged);

  return (
    <div className="flex flex-col w-full gap-6">
      <div className="mb-2">
        <PageTitle>
          Acervo e <span className="text-[#00A355]">Catalogação</span>
        </PageTitle>
        <p className="mt-2 text-sm text-gray-500 max-w-2xl">
          Gerencie a homologação e inclusão no acervo institucional dos Trabalhos de Conclusão de Curso encaminhados pela Coordenação.
        </p>
      </div>

      {/* CARDS DE RESUMO */}
      <div className="grid grid-cols-1 sm:grid-cols-2 gap-4 my-6">
        <div className="bg-white border border-gray-200 rounded-xl p-5 shadow-sm flex items-center justify-between">
          <div>
            <span className="text-xs font-semibold uppercase tracking-wider text-amber-700">
              Aguardando Catalogação
            </span>
            <div className="text-2xl font-bold text-gray-900 mt-1">
              {data.pendingCount}
            </div>
            <p className="text-xs text-gray-500 mt-0.5">
              TCCs com capa dura entregue prontos para inclusão
            </p>
          </div>
          <div className="w-12 h-12 rounded-lg bg-amber-50 border border-amber-200 flex items-center justify-center text-amber-700">
            <Clock className="w-6 h-6" />
          </div>
        </div>

        <div className="bg-white border border-gray-200 rounded-xl p-5 shadow-sm flex items-center justify-between">
          <div>
            <span className="text-xs font-semibold uppercase tracking-wider text-emerald-700">
              Catalogados no Acervo
            </span>
            <div className="text-2xl font-bold text-gray-900 mt-1">
              {data.catalogedCount}
            </div>
            <p className="text-xs text-gray-500 mt-0.5">
              Trabalhos disponíveis com código catalográfico
            </p>
          </div>
          <div className="w-12 h-12 rounded-lg bg-emerald-50 border border-emerald-200 flex items-center justify-center text-emerald-700">
            <Library className="w-6 h-6" />
          </div>
        </div>
      </div>

      {/* BARRA DE CONTROLE: ABAS E BUSCA */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 mb-6">
        <div className="flex items-center gap-2 border-b border-gray-200">
          <button
            onClick={() => setActiveTab('pending')}
            className={`pb-3 px-3 text-sm font-semibold transition-colors border-b-2 -mb-px flex items-center gap-2 ${
              activeTab === 'pending'
                ? 'border-[#002B18] text-[#002B18]'
                : 'border-transparent text-gray-500 hover:text-gray-900'
            }`}
          >
            <Clock className="w-4 h-4" />
            Para Catalogar ({data.pendingCount})
          </button>
          <button
            onClick={() => setActiveTab('cataloged')}
            className={`pb-3 px-3 text-sm font-semibold transition-colors border-b-2 -mb-px flex items-center gap-2 ${
              activeTab === 'cataloged'
                ? 'border-[#002B18] text-[#002B18]'
                : 'border-transparent text-gray-500 hover:text-gray-900'
            }`}
          >
            <CheckCircle2 className="w-4 h-4" />
            Já Catalogados ({data.catalogedCount})
          </button>
        </div>

        <div className="relative w-full sm:w-72">
          <Search className="w-4 h-4 absolute left-3 top-1/2 -translate-y-1/2 text-gray-400" />
          <Input
            placeholder="Buscar por título, discente..."
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            className="pl-9 h-9 text-sm"
          />
        </div>
      </div>

      {/* CONTEÚDO */}
      {loading ? (
        <div className="flex flex-col items-center justify-center py-20 text-gray-500">
          <LoaderCircle className="w-8 h-8 animate-spin text-[#002B18] mb-3" />
          <p className="text-sm font-medium">Carregando acervo e pendências...</p>
        </div>
      ) : activeTab === 'pending' ? (
        pendingList.length === 0 ? (
          <div className="bg-gray-50 border border-dashed border-gray-300 rounded-xl p-12 text-center text-gray-500">
            <CheckCircle2 className="w-10 h-10 text-emerald-500 mx-auto mb-3" />
            <h4 className="text-base font-semibold text-gray-800">
              Nenhum TCC aguardando catalogação
            </h4>
            <p className="text-sm text-gray-500 mt-1 max-w-md mx-auto">
              Quando a Coordenação encaminhar trabalhos com versão final e capa dura aprovadas, eles aparecerão aqui.
            </p>
          </div>
        ) : (
          <div className="space-y-4">
            {pendingList.map((item) => (
              <div
                key={item.tccId}
                className="bg-white border border-gray-200 rounded-xl p-5 shadow-sm hover:border-gray-300 transition-all flex flex-col md:flex-row md:items-center justify-between gap-5"
              >
                <div className="space-y-2 flex-1">
                  <div className="flex items-center gap-2 flex-wrap text-xs text-gray-500">
                    <span className="font-semibold px-2 py-0.5 rounded bg-amber-100 text-amber-800">
                      Encaminhado em {formatDate(item.forwardedAt)}
                    </span>
                    {item.courseName && (
                      <span className="px-2 py-0.5 rounded bg-gray-100 text-gray-700">
                        {item.courseName}
                      </span>
                    )}
                  </div>

                  <h3 className="text-base font-bold text-gray-900 leading-snug">
                    {item.title}
                  </h3>

                  <div className="flex flex-wrap gap-y-1 gap-x-4 text-xs text-gray-600">
                    <span className="inline-flex items-center gap-1">
                      <User className="w-3.5 h-3.5 text-gray-400" />
                      <strong>Discente(s):</strong> {item.studentNames?.join(', ') || 'Não informado'}
                    </span>
                    <span className="inline-flex items-center gap-1">
                      <GraduationCap className="w-3.5 h-3.5 text-gray-400" />
                      <strong>Orientador:</strong> {item.advisorName || 'Não informado'}
                    </span>
                  </div>
                </div>

                <div className="flex flex-wrap items-center gap-2.5 shrink-0">
                  <Button
                    variant="outline"
                    size="sm"
                    className="text-xs h-9"
                    onClick={() => handleDownloadPdf(item.tccId)}
                  >
                    <Download className="w-3.5 h-3.5 mr-1.5" />
                    Baixar PDF Final
                  </Button>

                  <Button
                    size="sm"
                    className="text-xs h-9 font-semibold bg-[#002B18] hover:bg-[#003820] text-white"
                    onClick={() => {
                      setSelectedTcc(item);
                      setCatalogCode(item.catalogCode || '');
                    }}
                  >
                    <Library className="w-3.5 h-3.5 mr-1.5 text-[#C8FF47]" />
                    Homologar no Acervo
                  </Button>
                </div>
              </div>
            ))}
          </div>
        )
      ) : (
        catalogedList.length === 0 ? (
          <div className="bg-gray-50 border border-dashed border-gray-300 rounded-xl p-12 text-center text-gray-500">
            <BookOpen className="w-10 h-10 text-gray-400 mx-auto mb-3" />
            <h4 className="text-base font-semibold text-gray-800">
              Nenhum trabalho catalogado encontrado
            </h4>
            <p className="text-sm text-gray-500 mt-1 max-w-md mx-auto">
              Os trabalhos com registro no acervo homologado serão listados neste histórico.
            </p>
          </div>
        ) : (
          <div className="space-y-4">
            {catalogedList.map((item) => (
              <div
                key={item.tccId}
                className="bg-white border border-gray-200 rounded-xl p-5 shadow-sm flex flex-col md:flex-row md:items-center justify-between gap-5"
              >
                <div className="space-y-2 flex-1">
                  <div className="flex items-center gap-2 flex-wrap text-xs">
                    <span className="font-semibold px-2 py-0.5 rounded bg-emerald-100 text-emerald-800 inline-flex items-center gap-1">
                      <CheckCircle2 className="w-3.5 h-3.5" />
                      Catalogado em {formatDate(item.catalogedAt)}
                    </span>
                    {item.catalogCode && (
                      <span className="font-mono font-bold px-2 py-0.5 rounded bg-blue-50 text-blue-800 border border-blue-200">
                        Código: {item.catalogCode}
                      </span>
                    )}
                    {item.courseName && (
                      <span className="px-2 py-0.5 rounded bg-gray-100 text-gray-700">
                        {item.courseName}
                      </span>
                    )}
                  </div>

                  <h3 className="text-base font-bold text-gray-900 leading-snug">
                    {item.title}
                  </h3>

                  <div className="flex flex-wrap gap-y-1 gap-x-4 text-xs text-gray-600">
                    <span className="inline-flex items-center gap-1">
                      <User className="w-3.5 h-3.5 text-gray-400" />
                      <strong>Discente(s):</strong> {item.studentNames?.join(', ') || 'Não informado'}
                    </span>
                    <span className="inline-flex items-center gap-1">
                      <GraduationCap className="w-3.5 h-3.5 text-gray-400" />
                      <strong>Orientador:</strong> {item.advisorName || 'Não informado'}
                    </span>
                  </div>
                </div>

                <div className="flex flex-wrap items-center gap-2.5 shrink-0">
                  <Button
                    variant="outline"
                    size="sm"
                    className="text-xs h-9"
                    onClick={() => handleDownloadPdf(item.tccId)}
                  >
                    <Download className="w-3.5 h-3.5 mr-1.5" />
                    Baixar PDF
                  </Button>

                  <Button
                    variant="outline"
                    size="sm"
                    className="text-xs h-9 text-gray-700"
                    onClick={() => {
                      setSelectedTcc(item);
                      setCatalogCode(item.catalogCode || '');
                    }}
                  >
                    Editar Código
                  </Button>
                </div>
              </div>
            ))}
          </div>
        )
      )}

      {/* MODAL DE HOMOLOGAÇÃO / CÓDIGO DE CATALOGAÇÃO */}
      <AlertDialog open={!!selectedTcc} onOpenChange={(open) => !open && setSelectedTcc(null)}>
        <AlertDialogContent className="max-w-md">
          <AlertDialogHeader>
            <AlertDialogTitle className="flex items-center gap-2 text-lg font-bold text-[#002B18]">
              <Library className="w-5 h-5 text-emerald-600" />
              Homologação no Acervo da Biblioteca
            </AlertDialogTitle>
            <AlertDialogDescription className="text-sm text-gray-600 space-y-2 pt-2">
              <span className="block text-gray-900 font-semibold line-clamp-2">
                {selectedTcc?.title}
              </span>
              <span className="block text-xs text-gray-500">
                Discente(s): {selectedTcc?.studentNames?.join(', ')}
              </span>
              <span className="block pt-2">
                Informe o código de catalogação / registro bibliográfico no repositório institucional:
              </span>
            </AlertDialogDescription>
          </AlertDialogHeader>

          <div className="py-2">
            <label className="text-xs font-semibold text-gray-700 block mb-1">
              Código de Catalogação <span className="text-red-500">*</span>
            </label>
            <Input
              autoFocus
              placeholder="Ex: CDU 004.42:378 / B-2026-042"
              value={catalogCode}
              onChange={(e) => setCatalogCode(e.target.value)}
              className="text-sm"
              onKeyDown={(e) => {
                if (e.key === 'Enter') {
                  e.preventDefault();
                  handleConfirmCataloging();
                }
              }}
            />
          </div>

          <AlertDialogFooter>
            <AlertDialogCancel disabled={submitting}>Cancelar</AlertDialogCancel>
            <AlertDialogAction
              onClick={(e) => {
                e.preventDefault();
                handleConfirmCataloging();
              }}
              disabled={submitting || !catalogCode.trim()}
              className="bg-[#002B18] text-white hover:bg-[#003820]"
            >
              {submitting ? (
                <>
                  <LoaderCircle className="w-4 h-4 animate-spin mr-1.5" />
                  Homologando...
                </>
              ) : (
                'Salvar e Homologar'
              )}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}
