import {
  MaxParticipants,
  isMaxParticipantsLimited,
  maxParticipantsLimit,
} from 'src/types/event';
import { validate, IError } from './validation';

export type Optional<T> = T | undefined;
export type WithId<T> = T & { id: string };

export type ReactChild = JSX.Element | string | false | undefined;

export const parseDescription = (value: string): string | IError[] => {
  const validator = validate<string>({
    'Beskrivelse må ha minst tre tegn': value.length < 3,
  });
  return validator.resolve(value);
};

export const parseLocation = (value: string): string | IError[] => {
  const validator = validate<string>({
    'Sted må ha minst tre tegn': value.length < 3,
    'Sted kan ha maks 60 tegn': value.length > 60,
  });
  return validator.resolve(value);
};

export const parseTitle = (value: string): string | IError[] => {
  const validator = validate<string>({
    'Tittel må ha minst tre tegn': value.length < 3,
    'Tittel kan ha maks 60 tegn': value.length > 60,
  });
  return validator.resolve(value);
};

export const parseHost = (value: string): string | IError[] => {
  const validator = validate<string>({
    'Arrangør må ha minst tre tegn': value.length < 3,
    'Arrangør kan ha maks 50 tegn': value.length > 50,
  });
  return validator.resolve(value);
};

export const parseMaxAttendees = (
  max: MaxParticipants<string>
): MaxParticipants<number> | IError[] => {
  if (!isMaxParticipantsLimited(max)) {
    return ['unlimited'];
  }
  const value = maxParticipantsLimit(max);
  const number = Number(value);
  const validator = validate<MaxParticipants<number>>({
    'Verdien må være et tall': Number.isNaN(number),
    'Du kan kun invitere et helt antall mennesker😎': !Number.isInteger(number),
    'Antallet kan ikke være over 5000, sett 0 hvis uendelig er ønsket':
      number > 5000,
    'Verdien må være positiv': number < 0,
    'Antall deltakere må settes': value === '',
  });
  return validator.resolve(['limited', number]);
};

export const toEditMaxAttendees = (
  value: MaxParticipants<number>
): MaxParticipants<string> => {
  if (!isMaxParticipantsLimited(value)) {
    return ['unlimited'];
  }
  return ['limited', maxParticipantsLimit(value).toString()];
};

export const parseQuestions = (value: string[]): string[] | IError[] => {
  if (value.length === 0) {
    return value;
  }
  const validator = validate<string[]>({
    'Spørsmål til deltaker må ha minst 5 tegn': value.some((s) => s.length < 5),
    'Spørsmål til deltaker kan ha maks 500 tegn': value.some(
      (s) => s.length > 500
    ),
  });
  return validator.resolve(value);
};

export const parseShortname = (
  value?: string
): string | undefined | IError[] => {
  if (value === undefined) {
    return undefined;
  }

  const chars = value.split('');

  const hasSlash = chars.includes('/');
  const hasQuestionMark = chars.includes('?');
  const hasHash = chars.includes('#');
  const hasPercentSign = chars.includes('%');

  const validator = validate<string>({
    'Kortnavn kan ikke inneholde URL-reserverte tegn som /, ? og #':
      hasSlash || hasQuestionMark || hasHash || hasPercentSign,
    'Kortnavn kan ikke være over 100 tegn': value.length > 100,
  });
  return validator.resolve(value);
};
