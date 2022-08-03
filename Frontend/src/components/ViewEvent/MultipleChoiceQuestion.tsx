import { Checkbox } from '@bekk/storybook';
import React from 'react';

interface Props {
  question: string;
  alternatives: string[];
  value: string;
  onChange: (s: string) => void;
}

export const MultipleChoiceQuestion = ({
  question,
  alternatives,
  value,
  onChange,
}: Props) => {
  const currentlySelectedAlternatives = parseAlternatives(value);
  return (
    <div key={question}>
      <div>{question}</div>
      {alternatives.map((alternative) => (
        <Checkbox
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
          }}
          isChecked={currentlySelectedAlternatives.includes(alternative)}
        />
      ))}
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
