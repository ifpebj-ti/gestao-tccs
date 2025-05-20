import * as React from 'react';
import { cn } from '@/lib/utils';
import { IconDefinition } from '@fortawesome/fontawesome-svg-core';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import {
  faCircleInfo,
  faEye,
  faEyeSlash,
  faCircleXmark
} from '@fortawesome/free-solid-svg-icons';

interface InputProps extends React.ComponentProps<'input'> {
  icon?: IconDefinition;
  isPassword?: boolean;
  helperText?: string;
  errorText?: string;
}

const Input = React.forwardRef<HTMLInputElement, InputProps>(
  (
    {
      className,
      type,
      icon,
      isPassword = false,
      helperText,
      errorText,
      ...props
    },
    ref
  ) => {
    const [showPassword, setShowPassword] = React.useState(false);

    const handleTogglePassword = () => {
      setShowPassword(!showPassword);
    };

    return (
      <div className="flex flex-col gap-2 w-full">
        <div className="flex items-center border border-gray-400 bg-white rounded-xs px-3 py-2 focus-within:ring-2 focus-within:ring-blue-500 transition-all">
          {icon && (
            <FontAwesomeIcon icon={icon} className="mr-2 text-gray-500" />
          )}
          <input
            type={isPassword ? (showPassword ? 'text' : 'password') : type}
            className={cn(
              'flex-1 bg-transparent text-sm placeholder-gray-400 focus:outline-none',
              className
            )}
            ref={ref}
            {...props}
          />
          {isPassword && (
            <button
              type="button"
              className="ml-2 text-gray-500 cursor-pointer focus:outline-none"
              onClick={handleTogglePassword}
            >
              <FontAwesomeIcon icon={showPassword ? faEyeSlash : faEye} />
            </button>
          )}
        </div>

        {helperText && (
          <div className="flex items-center bg-[#155BCB] text-white w-min text-nowrap text-sm p-1 gap-1">
            <FontAwesomeIcon icon={faCircleInfo} />
            {helperText}
          </div>
        )}

        {errorText && (
          <div className="flex items-center bg-red-500 text-white w-min text-nowrap text-sm p-1 gap-1">
            <FontAwesomeIcon icon={faCircleXmark} />
            {errorText}
          </div>
        )}
      </div>
    );
  }
);

Input.displayName = 'Input';

export { Input };
