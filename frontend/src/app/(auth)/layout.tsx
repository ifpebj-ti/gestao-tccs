import Header from '@/components/Header';

export default function AuthRoutesLayout({
  children
}: {
  children: React.ReactNode;
}) {
  return (
    <>
      <Header />
      <main className="mt-30 md:mt-24 px-5 md:px-10">{children}</main>
    </>
  );
}
