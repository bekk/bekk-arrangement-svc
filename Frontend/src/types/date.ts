import { validate, IError } from './validation';
import { format } from 'date-fns';
import { nb } from 'date-fns/esm/locale';
import { identityFunction } from 'src/utils';
import {
  addDays,
  CalendarDate,
  monthName,
  monthNumber,
} from 'typescript-calendar-date';

export type IDateContract = IDate;
export type IDateWriteModel = IDateContract;
export type IDateViewModel = IDateContract;

export interface IDate {
  day: number;
  month: number;
  year: number;
}

export type EditDate = string;

export const parseEditDate = (date: EditDate): IDate | IError[] => {
  const dateISO8601 = /^([0-9]{1,4})-([0-9]{1,2})-([0-9]{1,2})/;
  const dates = date.match(dateISO8601) || [];

  const year = Number(dates[1]);
  const month = Number(dates[2]);
  const day = Number(dates[3]);

  const validator = validate<IDate>({
    'Trenger år, måned og dato i DD-MM-YYYY format': dates.length <= 3,
    'År må være et heltall': !Number.isInteger(year),
    'Måned må være et heltall': !Number.isInteger(month),
    'Dag må være et heltall': !Number.isInteger(day),
  });

  return validator.resolve({
    year,
    month,
    day,
  });
};

export const parseDateViewModel = identityFunction;
export const toDateWriteModel = identityFunction;

export const toEditDate = ({ year, month, day }: IDate): string =>
  `${year.toString().padStart(4, '0')}-${month
    .toString()
    .padStart(2, '0')}-${day.toString().padStart(2, '0')}`;

// Util functions

export const dateAsText = (date: IDate): string => {
  return format(new Date(date.year, date.month - 1, date.day), 'cccc d. MMMM', {
    locale: nb,
  });
};

export const isSameDate = (date: IDate, otherDate: IDate) =>
  date.year === otherDate.year &&
  date.month === otherDate.month &&
  date.day === otherDate.day;

export const datesInOrder = ({
  first,
  last,
}: {
  first: IDate;
  last: IDate;
}) => {
  if (first.year < last.year) {
    return true;
  }
  if (first.year === last.year && first.month < last.month) {
    return true;
  }
  if (
    first.year === last.year &&
    first.month === last.month &&
    first.day < last.day
  ) {
    return true;
  }
  return false;
};

export const stringifyDate = ({ year, month, day }: IDate): EditDate =>
  `${day.toString().padStart(2, '0')}.${month
    .toString()
    .padStart(2, '0')}.${year}`;

export const dateToIDate = (date: Date): IDate => ({
  year: date.getFullYear(),
  month: date.getMonth() + 1,
  day: date.getDate(),
});

const fromCalendarDate = ({ day, month, year }: CalendarDate): IDate => {
  return { day, month: monthNumber(month), year };
};

const toCalendarDate = ({ day, month, year }: IDate): CalendarDate => {
  return { day, month: monthName(month), year };
};

export const addWeek = (date: IDate): IDate => {
  const oldDate = toCalendarDate(date);
  const newDate = addDays(oldDate, 7);
  return fromCalendarDate(newDate);
};
