import React from 'react';
import { IError, isIErrorList } from 'src/types/validation';
import { useState } from 'react';
import { ValidationResult } from '../ValidationResult/ValidationResult';
import { TextInput } from '../TextInput/TextInput';

interface ValidTextInputProps {
  label: string;
  placeholder?: string;
  value: string;
  isNumber?: boolean;
  validation: (value: string) => unknown | IError[];
  onChange: (value: string) => void;
  onLightBackground?: boolean;
  isSubmitClicked?: boolean;
}

export const ValidatedTextInput = ({
  label,
  placeholder,
  value,
  isNumber,
  validation,
  onChange,
  onLightBackground,
  isSubmitClicked = false,
}: ValidTextInputProps) => {
  const validationResult = validation(value);
  const [shouldShowErrors, setShouldShowErrors] = useState(false);

  return (
    <>
      <TextInput
        label={label}
        placeholder={placeholder}
        value={value}
        isNumber={isNumber}
        onChange={onChange}
        isError={(isSubmitClicked || shouldShowErrors) && isIErrorList(validationResult)}
        onBlur={() => setShouldShowErrors(true)}
        onLightBackground={onLightBackground}
      />
      {(isSubmitClicked || shouldShowErrors) && isIErrorList(validationResult) && (
        <ValidationResult
          validationResult={validationResult}
          onLightBackground={onLightBackground}
        />
      )}
    </>
  );
};
