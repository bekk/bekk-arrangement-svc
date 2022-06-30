import { IError, validate } from './validation';
import { EditDate } from './date';
import { EditTime } from './time';
import { addWeeks, format } from 'date-fns';
import { nb } from 'date-fns/esm/locale';

export const timezoneStart = -11;
export const timezoneEnd = 12;

export type TimeInstanceContract = string; // BigInt
export type TimeInstanceViewModel = TimeInstanceContract;
export type TimeInstanceWriteModel = TimeInstanceContract;

export type TimeInstance = Date;

export type TimeInstanceEdit = {
  date: EditDate;
  time: EditTime;
  timezone: number;
};

export const parseEditTimeInstance = (
  time: TimeInstanceEdit
): Date | IError[] => {
  const timeDateString = `${time.date}T${time.time[0]}:${
    time.time[1]
  }${getTimezoneInISOFormat(time.timezone)}`;

  const dateTimeStamp = Date.parse(timeDateString);

  const validator = validate<Date>({
    'Tiden er ugyldig': isNaN(dateTimeStamp),
  });
  return validator.resolve(new Date(dateTimeStamp));
};

export const toTimeInstanceWriteModel = (
  time: TimeInstance
): TimeInstanceWriteModel => time.getTime().toString();

export const parseTimeInstanceViewModel = (
  time: TimeInstanceViewModel
): TimeInstance => new Date(Number(time));

export const toEditTimeInstance = (date: TimeInstance): TimeInstanceEdit => {
  const year = date.getFullYear().toString();

  // Month range is 0-11. We need to '+1' to make the month readable for Date()
  const month = twoDigitValue(date.getMonth() + 1);
  const day = twoDigitValue(date.getDate());
  const hour = twoDigitValue(date.getHours());
  const minute = twoDigitValue(date.getMinutes());

  // If the timezoneOffset is negative, timezone must be positive.
  // Therefore we must divide by -60
  // (TimezoneOffset is also time difference in minutes)
  const timezone = date.getTimezoneOffset() / -60;

  return {
    date: `${year}-${month}-${day}`,
    time: [hour, minute],
    timezone,
  };
};

// Util functions

const twoDigitValue = (value: number): string =>
  value.toString().padStart(2, '0');

const getTimezoneInISOFormat = (timezone: number): string => {
  const positiveTimezone = twoDigitValue(Math.abs(timezone));
  if (timezone > 0) {
    return `+${positiveTimezone}:00`;
  }
  return `-${positiveTimezone}:00`;
};

export const stringifyTimeInstanceDate = (date: TimeInstance): string =>
  `${twoDigitValue(date.getDay())}.${twoDigitValue(
    date.getMonth() + 1
  )}.${date.getFullYear()}`;

export const stringifyTimeInstanceTime = (date: TimeInstance): string =>
  `${twoDigitValue(date.getHours())}:${twoDigitValue(date.getMinutes())}`;

export const stringifyTimeInstanceWithDayName = (
  date: TimeInstance
): string => {
  return format(date, 'cccc dd. MMMM yyyy', { locale: nb });
};

export const addWeekToTimeInstance = (date: TimeInstance): TimeInstance => {
  return addWeeks(date, 1);
};
