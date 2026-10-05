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
import { faPlus, faEdit, faTrash, faChevronDown, faChevronUp } from '@fortawesome/free-solid-svg-icons';
import { useInstituicoes, Campi, Course } from '@/app/hooks/useInstituicoes';

export default function InstituicoesPage() {
  const {
    campis,
    loading,
    fetchCampis,
    createCampi,
    updateCampi,
    deleteCampi,
    createCourse,
    updateCourse,
    deleteCourse
  } = useInstituicoes();

  useEffect(() => {
    fetchCampis();
  }, [fetchCampis]);

  // Modais State
  const [isCampiModalOpen, setCampiModalOpen] = useState(false);
  const [isCourseModalOpen, setCourseModalOpen] = useState(false);
  const [deleteConfirmOpen, setDeleteConfirmOpen] = useState(false);
  const [expandedCampis, setExpandedCampis] = useState<number[]>([]);

  // Form State
  const [editingCampi, setEditingCampi] = useState<Campi | null>(null);
  const [editingCourse, setEditingCourse] = useState<Course | null>(null);
  const [campiName, setCampiName] = useState('');
  const [campiCity, setCampiCity] = useState('');
  const [courseName, setCourseName] = useState('');
  const [courseLevel, setCourseLevel] = useState('');
  const [targetCampiId, setTargetCampiId] = useState<number | null>(null);

  // Delete State
  const [deleteType, setDeleteType] = useState<'campi' | 'course' | null>(null);
  const [deleteId, setDeleteId] = useState<number | null>(null);

  const toggleCampi = (id: number) => {
    if (expandedCampis.includes(id)) {
      setExpandedCampis(expandedCampis.filter((campiId) => campiId !== id));
    } else {
      setExpandedCampis([...expandedCampis, id]);
    }
  };

  const handleOpenCampiModal = (campi?: Campi) => {
    if (campi) {
      setEditingCampi(campi);
      setCampiName(campi.name);
      setCampiCity(campi.city || '');
    } else {
      setEditingCampi(null);
      setCampiName('');
      setCampiCity('');
    }
    setCampiModalOpen(true);
  };

  const handleOpenCourseModal = (campiId: number, course?: Course) => {
    setTargetCampiId(campiId);
    if (course) {
      setEditingCourse(course);
      setCourseName(course.name);
      setCourseLevel(course.level || '');
    } else {
      setEditingCourse(null);
      setCourseName('');
      setCourseLevel('');
    }
    setCourseModalOpen(true);
  };

  const handleSaveCampi = async () => {
    let success = false;
    if (editingCampi) {
      success = await updateCampi(editingCampi.id, campiName, campiCity);
    } else {
      success = await createCampi(campiName, campiCity);
    }
    if (success) setCampiModalOpen(false);
  };

  const handleSaveCourse = async () => {
    let success = false;
    if (editingCourse) {
      success = await updateCourse(editingCourse.id, courseName, courseLevel);
    } else {
      if (targetCampiId) {
        success = await createCourse(courseName, courseLevel, targetCampiId);
      }
    }
    if (success) setCourseModalOpen(false);
  };

  const handleDelete = async () => {
    if (!deleteId || !deleteType) return;
    if (deleteType === 'campi') {
      await deleteCampi(deleteId);
    } else {
      await deleteCourse(deleteId);
    }
    setDeleteConfirmOpen(false);
  };

  const openDeleteConfirm = (type: 'campi' | 'course', id: number) => {
    setDeleteType(type);
    setDeleteId(id);
    setDeleteConfirmOpen(true);
  };

  return (
    <div className="flex flex-col gap-6">
      <BreadcrumbAuto />
      <h1 className="md:text-4xl text-3xl font-semibold md:font-normal text-gray-800">
        Gerenciar Instituições e Cursos
      </h1>

      {loading ? (
        <p>Carregando...</p>
      ) : (
        <div className="flex flex-col gap-6">
          <div className="bg-white p-6 rounded-xl shadow-sm border border-gray-200">
            <div className="flex justify-between items-center mb-6">
              <h2 className="text-2xl font-semibold text-gray-800">Campus</h2>
              <Button onClick={() => handleOpenCampiModal()} className="bg-blue-600 hover:bg-blue-700 text-white">
                <FontAwesomeIcon icon={faPlus} className="mr-2" /> Novo Campus
              </Button>
            </div>
            
            <div className="flex flex-col gap-4">
              {campis.map((campi) => {
                const isExpanded = expandedCampis.includes(campi.id);
                return (
                  <div key={campi.id} className="border rounded-lg bg-gray-50 overflow-hidden">
                    {/* Header do Campus */}
                    <div className="p-4 flex justify-between items-center bg-white border-b border-gray-200">
                      <div className="flex items-center gap-4 cursor-pointer" onClick={() => toggleCampi(campi.id)}>
                        <Button variant="ghost" size="sm" className="w-8 h-8 p-0">
                          <FontAwesomeIcon icon={isExpanded ? faChevronUp : faChevronDown} className="text-gray-500" />
                        </Button>
                        <div>
                          <h3 className="font-bold text-lg">{campi.name}</h3>
                          <p className="text-sm text-gray-600">{campi.city}</p>
                        </div>
                      </div>
                      <div className="flex gap-2">
                        <Button variant="ghost" size="sm" onClick={() => handleOpenCampiModal(campi)}>
                          <FontAwesomeIcon icon={faEdit} className="text-blue-600" />
                        </Button>
                        <Button variant="ghost" size="sm" onClick={() => openDeleteConfirm('campi', campi.id)}>
                          <FontAwesomeIcon icon={faTrash} className="text-red-600" />
                        </Button>
                      </div>
                    </div>

                    {/* Area Expandida - Cursos do Campus */}
                    {isExpanded && (
                      <div className="p-4 bg-gray-50">
                        <div className="flex justify-between items-center mb-4">
                          <h4 className="text-md font-semibold text-gray-700">Cursos deste Campus</h4>
                          <Button onClick={() => handleOpenCourseModal(campi.id)} variant="outline" size="sm" className="border-emerald-600 text-emerald-600 hover:bg-emerald-50">
                            <FontAwesomeIcon icon={faPlus} className="mr-2" /> Adicionar Curso
                          </Button>
                        </div>

                        {(!campi.courses || campi.courses.length === 0) ? (
                          <p className="text-sm text-gray-500 italic">Nenhum curso cadastrado neste campus.</p>
                        ) : (
                          <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
                            {campi.courses.map((course) => (
                              <div key={course.id} className="p-3 border rounded bg-white flex justify-between items-center shadow-sm">
                                <div>
                                  <h5 className="font-semibold text-gray-800">{course.name}</h5>
                                  <span className="text-xs px-2 py-1 bg-gray-100 rounded text-gray-600">{course.level || 'Sem nível'}</span>
                                </div>
                                <div className="flex gap-2">
                                  <Button variant="ghost" size="sm" onClick={() => handleOpenCourseModal(campi.id, course)}>
                                    <FontAwesomeIcon icon={faEdit} className="text-blue-600" />
                                  </Button>
                                  <Button variant="ghost" size="sm" onClick={() => openDeleteConfirm('course', course.id)}>
                                    <FontAwesomeIcon icon={faTrash} className="text-red-600" />
                                  </Button>
                                </div>
                              </div>
                            ))}
                          </div>
                        )}
                      </div>
                    )}
                  </div>
                );
              })}
            </div>
          </div>
        </div>
      )}

      {/* Modal de Campus */}
      <Dialog open={isCampiModalOpen} onOpenChange={setCampiModalOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{editingCampi ? 'Editar Campus' : 'Adicionar Novo Campus'}</DialogTitle>
          </DialogHeader>
          <div className="space-y-4 py-4">
            <div>
              <label className="text-sm font-medium">Nome do Campus</label>
              <Input value={campiName} onChange={(e) => setCampiName(e.target.value)} placeholder="Ex: Campus Recife" />
            </div>
            <div>
              <label className="text-sm font-medium">Cidade</label>
              <Input value={campiCity} onChange={(e) => setCampiCity(e.target.value)} placeholder="Ex: Recife" />
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setCampiModalOpen(false)}>Cancelar</Button>
            <Button onClick={handleSaveCampi} className="bg-blue-600 hover:bg-blue-700 text-white">
              Salvar
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Modal de Curso */}
      <Dialog open={isCourseModalOpen} onOpenChange={setCourseModalOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{editingCourse ? 'Editar Curso' : 'Adicionar Novo Curso'}</DialogTitle>
          </DialogHeader>
          <div className="space-y-4 py-4">
            <div>
              <label className="text-sm font-medium">Nome do Curso</label>
              <Input value={courseName} onChange={(e) => setCourseName(e.target.value)} placeholder="Ex: Engenharia de Software" />
            </div>
            <div>
              <label className="text-sm font-medium">Nível</label>
              <Input value={courseLevel} onChange={(e) => setCourseLevel(e.target.value)} placeholder="Ex: Graduação" />
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setCourseModalOpen(false)}>Cancelar</Button>
            <Button onClick={handleSaveCourse} className="bg-emerald-600 hover:bg-emerald-700 text-white">
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
              Esta ação não pode ser desfeita. Isso excluirá permanentemente o {deleteType === 'campi' ? 'campus (e todos os cursos atrelados a ele)' : 'curso'}.
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
