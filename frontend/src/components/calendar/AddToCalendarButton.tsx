'use client';

import React from 'react';
import { Calendar, ChevronDown, Download, ExternalLink } from 'lucide-react';
import { Button, ButtonProps } from '@/components/ui/button';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger
} from '@/components/ui/dropdown-menu';
import {
  CalendarEventData,
  buildGoogleCalendarUrl,
  buildOutlookCalendarUrl,
  getDownloadIcsUrl
} from '@/utils/calendar';

interface AddToCalendarButtonProps {
  eventData: CalendarEventData;
  variant?: ButtonProps['variant'];
  size?: ButtonProps['size'];
  className?: string;
  label?: string;
}

export function AddToCalendarButton({
  eventData,
  variant = 'outline',
  size = 'default',
  className = '',
  label = 'Adicionar à agenda'
}: AddToCalendarButtonProps) {
  const googleUrl = buildGoogleCalendarUrl(eventData);
  const outlookUrl = buildOutlookCalendarUrl(eventData);
  const icsUrl = getDownloadIcsUrl(eventData.tccId);

  const handleDownloadIcs = () => {
    // Open or download the ICS file directly
    window.open(icsUrl, '_blank');
  };

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button
          variant={variant}
          size={size}
          className={`inline-flex items-center gap-2 ${className}`}
        >
          <Calendar className="size-4" />
          <span>{label}</span>
          <ChevronDown className="size-3.5 opacity-60" />
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end" className="w-56 p-2 bg-white rounded-xl shadow-lg border border-gray-200">
        <DropdownMenuLabel className="text-xs font-bold text-gray-500 uppercase px-2 py-1">
          Salvar defesa na agenda
        </DropdownMenuLabel>
        <DropdownMenuSeparator className="my-1" />
        {googleUrl && (
          <DropdownMenuItem asChild>
            <a
              href={googleUrl}
              target="_blank"
              rel="noopener noreferrer"
              className="flex items-center justify-between cursor-pointer py-2 px-2 text-sm text-[#002B18] hover:bg-gray-100 rounded-lg"
            >
              <span>Google Agenda</span>
              <ExternalLink className="size-3.5 opacity-50" />
            </a>
          </DropdownMenuItem>
        )}
        {outlookUrl && (
          <DropdownMenuItem asChild>
            <a
              href={outlookUrl}
              target="_blank"
              rel="noopener noreferrer"
              className="flex items-center justify-between cursor-pointer py-2 px-2 text-sm text-[#002B18] hover:bg-gray-100 rounded-lg"
            >
              <span>Outlook</span>
              <ExternalLink className="size-3.5 opacity-50" />
            </a>
          </DropdownMenuItem>
        )}
        <DropdownMenuItem
          onClick={handleDownloadIcs}
          className="flex items-center justify-between cursor-pointer py-2 px-2 text-sm text-[#002B18] hover:bg-gray-100 rounded-lg"
        >
          <span>Arquivo iCalendar (.ics)</span>
          <Download className="size-3.5 opacity-50" />
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  );
}

