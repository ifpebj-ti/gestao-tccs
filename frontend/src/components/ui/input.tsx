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
  icon?: IconDefinition | React.ReactNode;
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
    const messageId = React.useId();
    const description =
      [
        props['aria-describedby'],
        helperText ? `${messageId}-helper` : null,
        errorText ? `${messageId}-error` : null
      ]
        .filter(Boolean)
        .join(' ') || undefined;

    const handleTogglePassword = () => {
      setShowPassword(!showPassword);
    };

    const isFontAwesome = (icon as IconDefinition)?.iconName !== undefined;

    return (
      <div className="flex flex-col gap-1.5 w-full">
        <div
          className={cn(
            'flex min-w-0 items-center border-0 border-b-2 border-[#002B18] py-2.5 bg-transparent transition-colors duration-150 focus-within:border-[#006633]',
            props.disabled &&
              'bg-gray-100 border-gray-200 cursor-not-allowed opacity-60',
            errorText &&
              'border-red-500 focus-within:border-red-500 focus-within:ring-red-500'
          )}
        >
          {icon && (
            <div
              className={cn(
                'mr-2 flex items-center text-gray-500',
                props.disabled && 'text-gray-400'
              )}
            >
              {isFontAwesome ? (
                <FontAwesomeIcon icon={icon as IconDefinition} />
              ) : (
                (icon as React.ReactNode)
              )}
            </div>
          )}
          <input
            type={isPassword ? (showPassword ? 'text' : 'password') : type}
            className={cn(
              'min-w-0 w-full flex-1 bg-transparent text-sm text-gray-800 placeholder:text-gray-400 focus:outline-none disabled:cursor-not-allowed disabled:text-gray-500',
              className
            )}
            ref={ref}
            {...props}
            aria-invalid={errorText ? true : props['aria-invalid']}
            aria-describedby={description}
          />
          {isPassword && (
            <button
              type="button"
              className="ml-2 text-gray-400 hover:text-gray-600 cursor-pointer focus:outline-none"
              onClick={handleTogglePassword}
              disabled={props.disabled}
              aria-label={showPassword ? 'Ocultar senha' : 'Exibir senha'}
            >
              <FontAwesomeIcon icon={showPassword ? faEyeSlash : faEye} />
            </button>
          )}
        </div>

        {helperText && (
          <p
            id={`${messageId}-helper`}
            className="flex items-center text-xs text-gray-500 gap-1 mt-0.5"
          >
            <FontAwesomeIcon icon={faCircleInfo} className="text-gray-400" />
            {helperText}
          </p>
        )}

        {errorText && (
          <p
            id={`${messageId}-error`}
            role="alert"
            className="flex items-center text-xs text-red-600 gap-1 mt-0.5 font-medium"
          >
            <FontAwesomeIcon icon={faCircleXmark} className="text-red-500" />
            {errorText}
          </p>
        )}
      </div>
    );
  }
);

Input.displayName = 'Input';

export { Input };
