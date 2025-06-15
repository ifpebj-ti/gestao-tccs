import { redirect } from 'next/navigation';

export default function TCCRoot({ params }: { params: { id: string } }) {
  redirect(`/ongoingTCCs/${params.id}/signatures`);
}
