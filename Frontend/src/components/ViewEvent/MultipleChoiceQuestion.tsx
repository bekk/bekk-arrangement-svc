import React, { useState } from 'react';
import { CheckBox } from '../Common/Checkbox/CheckBox';
import { IError, isValid } from 'src/types/validation';
import { ValidationResult } from 'src/components/Common/ValidationResult/ValidationResult';
import style from './ViewEventContainer.module.scss';

interface Props {
  question: string;
  alternatives: string[];
  value: string;
  onChange: (s: string) => void;
  validation: (value: string) => string | IError[];
  isSubmitClicked: boolean;
}

export const MultipleChoiceQuestion = ({
  question,
  alternatives,
  value,
  onChange,
  validation,
  isSubmitClicked,
}: Props) => {
  const validationResult = validation(value);
  const [isEdited, setIsEdited] = useState(false);

  const currentlySelectedAlternatives = parseAlternatives(value);
  return (
    <div key={question}>
      <div className={style.multipleChoiceHeading}>{question}</div>
      {alternatives.map((alternative) => (
        <CheckBox
          key={alternative}
          onDarkBackground
          label={alternative}
          onChange={(selected) => {
            if (selected) {
              const newlySelectedAlternatives = alternatives.filter(
                (x) =>
                  currentlySelectedAlternatives.includes(x) || x === alternative
              );
              onChange(serializeAlternatives(newlySelectedAlternatives));
            } else {
              const newlySelectedAlternatives =
                currentlySelectedAlternatives.filter((x) => x !== alternative);
              onChange(serializeAlternatives(newlySelectedAlternatives));
            }
            setIsEdited(true);
          }}
          isChecked={currentlySelectedAlternatives.includes(alternative)}
        />
      ))}
      {(isSubmitClicked || isEdited) && !isValid(validationResult) && (
        <ValidationResult validationResult={validationResult} />
      )}
    </div>
  );
};

const serializeAlternatives = (alternatives: string[]) =>
  alternatives.join(';');

const parseAlternatives = (alternatives: string | null) =>
  alternatives
    ?.split(';')
    ?.map((s) => s.trim())
    ?.filter((s) => s !== '') ?? [];

export const multipleChoiceAlternatives = (q: string) => {
  const alternativesRegex = /\/\/\s?Alternativer:(.+)$/;
  const [match, alternatives] = q.match(alternativesRegex) ?? [null, null];
  return {
    isMultipleChoiceQuestion: alternatives !== null,
    alternatives: parseAlternatives(alternatives),
    actualQuestion: q.slice(0, q.length - (match?.length ?? 0)).trim(),
  };
};
