'use client';
import { semesterSchema } from '@/app/schemas/managementSchema';
import { toast } from 'react-toastify';

import { PageState } from '@/components/layout/PageState';

import { PageTitle } from '@/components/layout/PageTitle';
import { SectionTitle } from '@/components/layout/SectionTitle';

import { useEffect, useState } from 'react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogFooter
} from '@/components/ui/dialog';
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
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faPlus, faEdit, faTrash } from '@fortawesome/free-solid-svg-icons';
import { useSemestres, Semester } from '@/app/hooks/useSemestres';

export default function SemestresPage() {
  const {
    semesters,
    loading,
    fetchSemesters,
    createSemester,
    updateSemester,
    deleteSemester
  } = useSemestres();

  useEffect(() => {
    fetchSemesters();
  }, [fetchSemesters]);

  // Modais State
  const [isSemesterModalOpen, setSemesterModalOpen] = useState(false);
  const [deleteConfirmOpen, setDeleteConfirmOpen] = useState(false);

  // Form State
  const [editingSemester, setEditingSemester] = useState<Semester | null>(null);
  const [name, setName] = useState('');
  const [startDate, setStartDate] = useState('');
  const [endDate, setEndDate] = useState('');
  const [isActive, setIsActive] = useState(true);

  // Delete State
  const [deleteId, setDeleteId] = useState<number | null>(null);

  const handleOpenSemesterModal = (semester?: Semester) => {
    if (semester) {
      setEditingSemester(semester);
      setName(semester.name);
      setStartDate(semester.startDate.split('T')[0]);
      setEndDate(semester.endDate.split('T')[0]);
      setIsActive(semester.isActive);
    } else {
      setEditingSemester(null);
      setName('');
      setStartDate('');
      setEndDate('');
      setIsActive(true);
    }
    setSemesterModalOpen(true);
  };

  const handleSaveSemester = async () => {
    const parsed = semesterSchema.safeParse({ name, startDate, endDate });
    if (!parsed.success) {
      toast.error(parsed.error.issues[0].message);
      return;
    }
    let success = false;
    // A API recebe as datas em ISO.
    const startIso = new Date(startDate).toISOString();
    const endIso = new Date(endDate).toISOString();

    if (editingSemester) {
      success = await updateSemester(editingSemester.id, parsed.data.name, startIso, endIso, isActive);
    } else {
      success = await createSemester(parsed.data.name, startIso, endIso, isActive);
    }
    if (success) setSemesterModalOpen(false);
  };

  const handleDelete = async () => {
    if (!deleteId) return;
    await deleteSemester(deleteId);
    setDeleteConfirmOpen(false);
  };

  const openDeleteConfirm = (id: number) => {
    setDeleteId(id);
    setDeleteConfirmOpen(true);
  };

  return (
    <div className="flex flex-col gap-6">
      <PageTitle >
        Gerenciar Semestres Letivos
      </PageTitle>

      {loading ? (
        <PageState loading>Carregando...</PageState>
      ) : (
        <div className="flex flex-col gap-6">
          <div className="py-6 border-b border-[#ECEAE2]">
            <div className="flex justify-between items-center mb-6">
              <SectionTitle >Semestres</SectionTitle>
              <Button onClick={() => handleOpenSemesterModal()} className="bg-[#C8FF47] hover:bg-[#002B18] text-[#002B18] hover:text-[#C8FF47]">
                <FontAwesomeIcon icon={faPlus} className="mr-2" /> Novo Semestre
              </Button>
            </div>
            
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
              {semesters.map((semester) => (
                <div key={semester.id} className="border-b border-[#ECEAE2] py-5 flex flex-col gap-2">
                  <div className="flex justify-between items-start">
                    <div>
                      <h3 className="font-bold text-lg text-gray-800">{semester.name}</h3>
                      <span className={`text-xs px-2 py-1 rounded ${semester.isActive ? 'bg-green-100 text-green-800' : 'bg-red-100 text-red-800'}`}>
                        {semester.isActive ? 'Ativo' : 'Inativo'}
                      </span>
                    </div>
                    <div className="flex gap-2">
                      <Button variant="ghost" size="sm" aria-label={`Editar semestre ${semester.name}`} onClick={() => handleOpenSemesterModal(semester)}>
                        <FontAwesomeIcon icon={faEdit} className="text-[#006633]" />
                      </Button>
                      <Button variant="destructive" size="sm" aria-label={`Excluir semestre ${semester.name}`} onClick={() => openDeleteConfirm(semester.id)}>
                        <FontAwesomeIcon icon={faTrash} className="" />
                      </Button>
                    </div>
                  </div>
                  <div className="text-sm text-gray-600 mt-2">
                    <p>Início: {new Date(semester.startDate).toLocaleDateString()}</p>
                    <p>Fim: {new Date(semester.endDate).toLocaleDateString()}</p>
                  </div>
                </div>
              ))}
              {semesters.length === 0 && (
                <PageState>Nenhum semestre cadastrado.</PageState>
              )}
            </div>
          </div>
        </div>
      )}

      {/* Modal de Semestre */}
      <Dialog open={isSemesterModalOpen} onOpenChange={setSemesterModalOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{editingSemester ? 'Editar Semestre' : 'Adicionar Novo Semestre'}</DialogTitle>
          </DialogHeader>
          <div className="space-y-4 py-4">
            <div>
              <label className="block mb-2 font-[family-name:var(--font-poppins)] text-[0.72rem] font-bold uppercase tracking-[0.06em] text-gray-700">Nome do Semestre</label>
              <Input value={name} onChange={(e) => setName(e.target.value)} placeholder="Ex: 2024.1" />
            </div>
            <div className="grid grid-cols-2 gap-4">
              <div>
                <label className="block mb-2 font-[family-name:var(--font-poppins)] text-[0.72rem] font-bold uppercase tracking-[0.06em] text-gray-700">Data de Início</label>
                <Input type="date" value={startDate} onChange={(e) => setStartDate(e.target.value)} />
              </div>
              <div>
                <label className="block mb-2 font-[family-name:var(--font-poppins)] text-[0.72rem] font-bold uppercase tracking-[0.06em] text-gray-700">Data de Fim</label>
                <Input type="date" value={endDate} onChange={(e) => setEndDate(e.target.value)} />
              </div>
            </div>
            <div className="flex items-center gap-2">
              <input 
                type="checkbox" 
                id="isActive" 
                checked={isActive} 
                onChange={(e) => setIsActive(e.target.checked)} 
                className="w-4 h-4 text-[#006633] bg-gray-100 border-gray-300 rounded focus:ring-[#006633]"
              />
              <label htmlFor="isActive" className="block mb-2 font-[family-name:var(--font-poppins)] text-[0.72rem] font-bold uppercase tracking-[0.06em] text-gray-700">
                Semestre Ativo
              </label>
            </div>
          </div>
          <DialogFooter>
            <Button variant="destructive" onClick={() => setSemesterModalOpen(false)}>Cancelar</Button>
            <Button onClick={handleSaveSemester} className="bg-[#C8FF47] hover:bg-[#002B18] text-[#002B18] hover:text-[#C8FF47]">
              Salvar
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Modal de Confirmação de Exclusão */}
      <AlertDialog open={deleteConfirmOpen} onOpenChange={setDeleteConfirmOpen}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Tem certeza que deseja excluir?</AlertDialogTitle>
            <AlertDialogDescription>
              Esta ação não pode ser desfeita.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancelar</AlertDialogCancel>
            <AlertDialogAction variant="destructive" onClick={handleDelete} className="text-white">
              Excluir
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}
