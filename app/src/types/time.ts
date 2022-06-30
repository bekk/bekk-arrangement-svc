import { validate, IError } from './validation';
import { identityFunction } from 'src/utils';

export type ITimeContract = ITime;
export type ITimeViewModel = ITimeContract;
export type ITimeWriteModel = ITimeContract;

export interface ITime {
  hour: number;
  minute: number;
}

export type EditTime = [string, string];

export const parseEditTime = ([_hour, _minutes]: EditTime):
  | ITime
  | IError[] => {
  const hour = Number(_hour);
  const minute = Number(_minutes);

  const validator = validate<ITime>({
    'Kan ikke ha mer enn 60 minutter i en time': minute > 59,
    'Minutt kan ikke ha negativ verdi': minute < 0,
    'Det er ikke mer enn 24 timer i et døgn': hour > 23,
    'Time kan ikke ha negativ verdi': hour < 0,
    'Time må være et heltall':
      isNaN(hour) || _hour === '' || !Number.isInteger(hour),
    'Minutt må være et heltall':
      isNaN(minute) || _minutes === '' || !Number.isInteger(minute),
  });

  return validator.resolve({ hour, minute });
};

export const toEditTime = ({ hour, minute }: ITime): EditTime => [
  hour.toString().padStart(2, '0'),
  minute.toString().padStart(2, '0'),
];

export const parseTimeViewModel = identityFunction;
export const toTimeWriteModel = identityFunction;

// Util functions

export const stringifyTime = ({ hour, minute }: ITime): string =>
  `${hour.toString().padStart(2, '0')}:${minute.toString().padStart(2, '0')}`;

export const dateToITime = (date: Date): ITime => {
  return { hour: date.getHours(), minute: date.getMinutes() };
};

export const timesInOrder = ({
  first,
  last,
}: {
  first: ITime;
  last: ITime;
}) => {
  if (first.hour < last.hour) {
    return true;
  }
  if (first.hour === last.hour && first.minute < last.minute) {
    return true;
  }
  return false;
};
