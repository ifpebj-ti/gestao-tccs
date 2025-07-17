'use client';

import React, { useRef } from 'react';
import { Button } from '../ui/button';
import { faFilePdf, faTimesCircle } from '@fortawesome/free-solid-svg-icons';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';

interface CustomFileInputProps {
  selectedFile: File | null;
  onFileSelect: (file: File | null) => void;
  disabled?: boolean;
  accept?: string;
}

export function CustomFileInput({
  selectedFile,
  onFileSelect,
  disabled = false,
  accept = '.pdf'
}: CustomFileInputProps) {
  const fileInputRef = useRef<HTMLInputElement>(null);

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files ? e.target.files[0] : null;
    onFileSelect(file);
  };

  const handleButtonClick = () => {
    fileInputRef.current?.click();
  };

  const handleRemoveFile = () => {
    onFileSelect(null);
    if (fileInputRef.current) {
      fileInputRef.current.value = '';
    }
  };

  return (
    <div>
      <input
        type="file"
        ref={fileInputRef}
        onChange={handleFileChange}
        className="hidden"
        accept={accept}
        disabled={disabled}
      />

      <Button
        type="button"
        variant="outline"
        onClick={handleButtonClick}
        disabled={disabled}
        className="w-full"
      >
        Selecionar Arquivo
      </Button>

      {selectedFile && (
        <div className="mt-3 flex items-center justify-between p-2 bg-gray-100 border rounded-md">
          <div className="flex items-center gap-2 overflow-hidden">
            <FontAwesomeIcon
              icon={faFilePdf}
              className="text-red-500 flex-shrink-0"
            />
            <span
              className="text-sm text-gray-700 truncate"
              title={selectedFile.name}
            >
              {selectedFile.name}
            </span>
          </div>
          <Button
            variant="ghost"
            size="sm"
            onClick={handleRemoveFile}
            className="h-auto p-1 text-gray-500 hover:text-red-600"
          >
            <FontAwesomeIcon icon={faTimesCircle} />
          </Button>
        </div>
      )}
    </div>
  );
}
