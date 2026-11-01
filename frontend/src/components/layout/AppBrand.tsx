import Image from 'next/image';
import Link from 'next/link';
import IFPELogo from '../../../public/IFPE Logo.png';

export function AppBrand({ compact = false }: { compact?: boolean }) {
  return (
    <div className="flex h-16 shrink-0 items-center px-5">
      <Link
        href="/homePage"
        aria-label="Início do Gradus Flow"
        className="group flex items-center gap-3 focus-visible:outline-2 focus-visible:outline-offset-4 focus-visible:outline-[#006633]"
      >
        <span className="block h-8 w-6 shrink-0 overflow-hidden">
          <Image
            src={IFPELogo}
            alt="Instituto Federal de Pernambuco — Campus Belo Jardim"
            className="h-8 w-auto max-w-none"
            priority
          />
        </span>
        <span
          className={`${compact ? 'sr-only' : ''} whitespace-nowrap font-[family-name:var(--font-poppins)] text-base tracking-[-0.04em] text-[#002B18] transition-colors duration-150 group-hover:text-[#006633]`}
        >
          <span className="font-bold">Gradus</span>{' '}
          <span className="font-normal">Flow</span>
        </span>
      </Link>
    </div>
  );
}
