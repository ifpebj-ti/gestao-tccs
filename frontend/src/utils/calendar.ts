import { env } from 'next-runtime-env';

export interface CalendarEventData {
  tccId: number | string;
  title: string;
  summary?: string | null;
  location?: string | null;
  presentationDate?: string | null;
  presentationTime?: string | null;
}

export function parsePresentationDateTime(
  dateStr?: string | null,
  timeStr?: string | null
): { start: Date; end: Date } | null {
  if (!dateStr) return null;

  let year = 0;
  let month = 0;
  let day = 0;

  if (dateStr.includes('/')) {
    const parts = dateStr.split('/');
    day = parseInt(parts[0], 10);
    month = parseInt(parts[1], 10) - 1;
    year = parseInt(parts[2], 10);
  } else if (dateStr.includes('-')) {
    const parts = dateStr.split('-');
    year = parseInt(parts[0], 10);
    month = parseInt(parts[1], 10) - 1;
    day = parseInt(parts[2], 10);
  } else {
    const d = new Date(dateStr);
    if (isNaN(d.getTime())) return null;
    year = d.getFullYear();
    month = d.getMonth();
    day = d.getDate();
  }

  let hours = 14;
  let minutes = 0;
  if (timeStr && timeStr.includes(':')) {
    const timeParts = timeStr.split(':');
    hours = parseInt(timeParts[0], 10) || 0;
    minutes = parseInt(timeParts[1], 10) || 0;
  }

  const start = new Date(Date.UTC(year, month, day, hours, minutes));
  const end = new Date(start.getTime() + 2 * 60 * 60 * 1000); // 2h

  return { start, end };
}

export function formatUtcToCalendarIso(d: Date): string {
  return d.toISOString().replace(/[-:]/g, '').split('.')[0] + 'Z';
}

export function buildGoogleCalendarUrl(data: CalendarEventData): string | null {
  const dates = parsePresentationDateTime(data.presentationDate, data.presentationTime);
  if (!dates) return null;

  const startIso = formatUtcToCalendarIso(dates.start);
  const endIso = formatUtcToCalendarIso(dates.end);

  const title = `Defesa de TCC: ${data.title}`;
  const details = `Apresentação e avaliação do Trabalho de Conclusão de Curso.\n\nTítulo: ${data.title}\nLocal: ${data.location || 'Não informado'}\nResumo: ${data.summary || ''}`;

  return `https://calendar.google.com/calendar/render?action=TEMPLATE&text=${encodeURIComponent(title)}&dates=${startIso}/${endIso}&details=${encodeURIComponent(details)}&location=${encodeURIComponent(data.location || '')}`;
}

export function buildOutlookCalendarUrl(data: CalendarEventData): string | null {
  const dates = parsePresentationDateTime(data.presentationDate, data.presentationTime);
  if (!dates) return null;

  const title = `Defesa de TCC: ${data.title}`;
  const details = `Apresentação e avaliação do Trabalho de Conclusão de Curso.\n\nTítulo: ${data.title}\nLocal: ${data.location || 'Não informado'}\nResumo: ${data.summary || ''}`;

  return `https://outlook.live.com/calendar/0/deeplink/compose?subject=${encodeURIComponent(title)}&startdt=${dates.start.toISOString()}&enddt=${dates.end.toISOString()}&body=${encodeURIComponent(details)}&location=${encodeURIComponent(data.location || '')}`;
}

export function getDownloadIcsUrl(tccId: number | string): string {
  const apiUrl = env('NEXT_PUBLIC_API_URL') || '';
  return `${apiUrl}/api/Tcc/${tccId}/schedule/calendar.ics`;
}

