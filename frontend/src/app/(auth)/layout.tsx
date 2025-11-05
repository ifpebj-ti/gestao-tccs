import Header from '@/components/Header';

export default function AuthRoutesLayout({
  children
}: {
  children: React.ReactNode;
}) {
  return (
    <>
      <Header />
      <main className="mt-6 px-5 md:px-10 pb-5">{children}</main>
    </>
  );
}
