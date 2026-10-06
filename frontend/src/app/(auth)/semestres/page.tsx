'use client';

import { useEffect, useState } from 'react';
import { BreadcrumbAuto } from '@/components/ui/breadcrumb';
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
    let success = false;
    // convert strings to proper ISO string for C# DateTime
    const startIso = new Date(startDate).toISOString();
    const endIso = new Date(endDate).toISOString();

    if (editingSemester) {
      success = await updateSemester(editingSemester.id, name, startIso, endIso, isActive);
    } else {
      success = await createSemester(name, startIso, endIso, isActive);
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
      <BreadcrumbAuto />
      <h1 className="md:text-4xl text-3xl font-semibold md:font-normal text-gray-800">
        Gerenciar Semestres Letivos
      </h1>

      {loading ? (
        <p>Carregando...</p>
      ) : (
        <div className="flex flex-col gap-6">
          <div className="bg-white p-6 rounded-xl shadow-sm border border-gray-200">
            <div className="flex justify-between items-center mb-6">
              <h2 className="text-2xl font-semibold text-gray-800">Semestres</h2>
              <Button onClick={() => handleOpenSemesterModal()} className="bg-blue-600 hover:bg-blue-700 text-white">
                <FontAwesomeIcon icon={faPlus} className="mr-2" /> Novo Semestre
              </Button>
            </div>
            
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
              {semesters.map((semester) => (
                <div key={semester.id} className="border rounded-lg bg-gray-50 p-4 flex flex-col gap-2">
                  <div className="flex justify-between items-start">
                    <div>
                      <h3 className="font-bold text-lg text-gray-800">{semester.name}</h3>
                      <span className={`text-xs px-2 py-1 rounded ${semester.isActive ? 'bg-green-100 text-green-800' : 'bg-red-100 text-red-800'}`}>
                        {semester.isActive ? 'Ativo' : 'Inativo'}
                      </span>
                    </div>
                    <div className="flex gap-2">
                      <Button variant="ghost" size="sm" onClick={() => handleOpenSemesterModal(semester)}>
                        <FontAwesomeIcon icon={faEdit} className="text-blue-600" />
                      </Button>
                      <Button variant="ghost" size="sm" onClick={() => openDeleteConfirm(semester.id)}>
                        <FontAwesomeIcon icon={faTrash} className="text-red-600" />
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
                <p className="text-gray-500 italic">Nenhum semestre cadastrado.</p>
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
              <label className="text-sm font-medium">Nome do Semestre</label>
              <Input value={name} onChange={(e) => setName(e.target.value)} placeholder="Ex: 2024.1" />
            </div>
            <div className="grid grid-cols-2 gap-4">
              <div>
                <label className="text-sm font-medium">Data de Início</label>
                <Input type="date" value={startDate} onChange={(e) => setStartDate(e.target.value)} />
              </div>
              <div>
                <label className="text-sm font-medium">Data de Fim</label>
                <Input type="date" value={endDate} onChange={(e) => setEndDate(e.target.value)} />
              </div>
            </div>
            <div className="flex items-center gap-2">
              <input 
                type="checkbox" 
                id="isActive" 
                checked={isActive} 
                onChange={(e) => setIsActive(e.target.checked)} 
                className="w-4 h-4 text-blue-600 bg-gray-100 border-gray-300 rounded focus:ring-blue-500"
              />
              <label htmlFor="isActive" className="text-sm font-medium text-gray-900">
                Semestre Ativo
              </label>
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setSemesterModalOpen(false)}>Cancelar</Button>
            <Button onClick={handleSaveSemester} className="bg-blue-600 hover:bg-blue-700 text-white">
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
            <AlertDialogAction onClick={handleDelete} className="bg-red-600 hover:bg-red-700 text-white">
              Excluir
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}
