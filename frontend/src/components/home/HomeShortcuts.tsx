import type { LucideIcon } from 'lucide-react';
import { CardHome } from '@/components/CardHome';
import { SectionTitle } from '@/components/layout/SectionTitle';
import { PageState } from '@/components/layout/PageState';
import { FeatureActionCard } from '@/components/layout/FeatureActionCard';

export interface HomeShortcut {
  title: string;
  icon: LucideIcon;
  description: string;
  group: 'attention' | 'tracking' | 'management';
  visible: boolean;
  indicatorNumber?: number;
  indicatorColor?: string;
  onClick: () => void;
}

const groups = [
  { id: 'attention', title: 'Precisa da sua atenção' },
  { id: 'tracking', title: 'Acompanhamento' },
  { id: 'management', title: 'Gestão' }
] as const;

export function HomeShortcuts({
  cards,
  attentionOnly = false,
  loading = false
}: {
  cards: HomeShortcut[];
  attentionOnly?: boolean;
  loading?: boolean;
}) {
  const displayedCards = cards.filter((card) =>
    card.group === 'attention'
      ? (card.indicatorNumber ?? 0) > 0
      : !attentionOnly
  );
  if (attentionOnly && !displayedCards.some((card) => card.visible)) {
    return (
      <PageState loading={loading}>
        {loading
          ? 'Consultando suas pendências…'
          : 'Nenhuma pendência para exibir.'}
      </PageState>
    );
  }
  return (
    <div className="space-y-9">
      {groups.map((group) => {
        const visibleCards = displayedCards.filter(
          (card) => card.visible && card.group === group.id
        );
        if (visibleCards.length === 0) return null;

        return (
          <section key={group.id} aria-labelledby={`home-${group.id}`}>
            <SectionTitle id={`home-${group.id}`}>{group.title}</SectionTitle>
            <div className="grid grid-cols-1 gap-x-8 md:grid-cols-2">
              {visibleCards.map(({ title, icon: Icon, ...card }) =>
                group.id === 'attention' ? (
                  <FeatureActionCard
                    key={title}
                    heading={title}
                    description={card.description}
                    icon={Icon}
                    count={card.indicatorNumber}
                    actionLabel="Ver pendências"
                    onClick={card.onClick}
                    className="mt-4 max-w-md"
                  />
                ) : (
                  <CardHome
                    key={title}
                    variant="flat"
                    title={title}
                    icon={
                      <Icon
                        className="size-5"
                        strokeWidth={1.75}
                        aria-hidden="true"
                      />
                    }
                    description={card.description}
                    indicatorNumber={card.indicatorNumber}
                    indicatorColor={card.indicatorColor}
                    onClick={card.onClick}
                  />
                )
              )}
            </div>
          </section>
        );
      })}
    </div>
  );
}
