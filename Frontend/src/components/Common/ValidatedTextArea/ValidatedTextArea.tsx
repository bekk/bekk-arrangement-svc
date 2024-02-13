import React, { useMemo } from 'react';
import { useState } from 'react';
import { TextArea } from 'src/components/Common/TextArea/TextArea';
import { ValidationResult } from 'src/components/Common/ValidationResult/ValidationResult';
import { IError, isValid } from 'src/types/validation';

interface ValidTextAreaProps {
  label?: string;
  placeholder?: string;
  value: string;
  validation: (value: string) => unknown | IError[];
  onChange: (value: string) => void;
  onLightBackground?: boolean;
  isSubmitClicked?: boolean;
  className?: string;
}

export const ValidatedTextArea = ({
  label,
  placeholder,
  value,
  validation,
  onChange,
  onLightBackground = false,
  isSubmitClicked = false,
  className = '',
}: ValidTextAreaProps) => {
  const [isEdited, setIsEdited] = useState(false);
  const validationResult = useMemo(() => validation(value), [value]);

  return (
    <>
      <TextArea
        className={className}
        label={label}
        placeholder={placeholder}
        value={value}
        onChange={onChange}
        isError={(isSubmitClicked || isEdited) && !isValid(validationResult)}
        onBlur={() => setIsEdited(true)}
        onLightBackground={onLightBackground}
      />
      {(isSubmitClicked || isEdited) && !isValid(validationResult) && (
        <ValidationResult
          validationResult={validationResult}
          onLightBackground={onLightBackground}
        />
      )}
    </>
  );
};
