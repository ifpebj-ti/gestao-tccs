import Image from 'next/image';
import Link from 'next/link';
import IFPELogo from '../../../public/IFPE Logo.png';
import { cn } from '@/lib/utils';

export function AppBrand({
  compact = false,
  className
}: {
  compact?: boolean;
  className?: string;
}) {
  return (
    <div className={cn('flex h-16 shrink-0 items-center px-5', className)}>
      <Link
        href="/homePage"
        aria-label="Início do Gradus Flow"
        className="group flex items-center gap-2.5 sm:gap-3 focus-visible:outline-2 focus-visible:outline-offset-4 focus-visible:outline-[#006633]"
      >
        <span className="block h-7 w-5 sm:h-8 sm:w-6 shrink-0 overflow-hidden">
          <Image
            src={IFPELogo}
            alt="Instituto Federal de Pernambuco — Campus Belo Jardim"
            className="h-7 sm:h-8 w-auto max-w-none"
            priority
          />
        </span>
        <span
          className={`${compact ? 'sr-only' : ''} whitespace-nowrap font-[family-name:var(--font-poppins)] text-sm sm:text-base tracking-[-0.04em] text-[#002B18] transition-colors duration-150 group-hover:text-[#006633]`}
        >
          <span className="font-bold">Gradus</span>{' '}
          <span className="font-normal">Flow</span>
        </span>
      </Link>
    </div>
  );
}
