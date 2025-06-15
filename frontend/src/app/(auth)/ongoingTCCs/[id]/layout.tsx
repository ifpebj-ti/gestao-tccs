import TccTabs from '@/components/TccTabs';
import { BreadcrumbAuto } from '@/components/ui/breadcrumb';

export default function TccLayout({
  children,
  params
}: {
  children: React.ReactNode;
  params: { id: string };
}) {
  return (
    <div>
      <BreadcrumbAuto />
      <TccTabs id={params.id} />
      <div className="mt-4">{children}</div>
    </div>
  );
}
