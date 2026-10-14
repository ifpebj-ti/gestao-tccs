import type { StudentNavigation } from '@/components/Sidebar/menuConfig';

export function resolveStudentNavigation(
  completed: unknown,
  active: unknown
): StudentNavigation {
  if (!Array.isArray(completed) || !Array.isArray(active))
    return { status: 'error' };
  const first = completed[0] ?? active[0];
  if (first === undefined) return { status: 'none' };
  if (
    typeof first?.tccId !== 'number' ||
    !Number.isSafeInteger(first.tccId) ||
    first.tccId <= 0
  )
    return { status: 'error' };
  return {
    status: completed.length > 0 ? 'completed' : 'active',
    tccId: first.tccId
  };
}
