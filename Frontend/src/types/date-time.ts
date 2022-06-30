import { isAfter, isBefore } from 'date-fns';
import {
  datesInOrder,
  EditDate,
  IDate,
  IDateContract,
  isSameDate,
  parseDateViewModel,
  parseEditDate,
  stringifyDate,
  toDateWriteModel,
  toEditDate,
} from 'src/types/date';
import {
  EditTime,
  ITime,
  ITimeContract,
  parseEditTime,
  parseTimeViewModel,
  stringifyTime,
  timesInOrder,
  toEditTime,
  toTimeWriteModel,
} from 'src/types/time';
import { IError, isValid } from 'src/types/validation';
import { concatLists } from 'src/utils';

export type IDateTimeContract = {
  date: IDateContract;
  time: ITimeContract;
};
export type IDateTimeWriteModel = IDateTimeContract;
export type IDateTimeViewModel = IDateTimeContract;

export interface IDateTime {
  date: IDate;
  time: ITime;
}

export type EditDateTime = {
  date: EditDate;
  time: EditTime;
};

export const parseEditDateTime = (
  datetime: EditDateTime
): IDateTime | IError[] => {
  const date = parseEditDate(datetime.date);
  const time = parseEditTime(datetime.time);

  if (isValid(date) && isValid(time)) {
    return { date, time };
  }

  const errors = concatLists<IError>(date, time);
  return errors;
};

export const toEditDateTime = ({ date, time }: IDateTime): EditDateTime => ({
  date: toEditDate(date),
  time: toEditTime(time),
});

export const parseDateTimeViewModel = ({
  date,
  time,
}: IDateTimeViewModel): IDateTime => ({
  date: parseDateViewModel(date),
  time: parseTimeViewModel(time),
});

export const toDateTimeWriteModel = ({
  date,
  time,
}: IDateTime): IDateTimeWriteModel => ({
  date: toDateWriteModel(date),
  time: toTimeWriteModel(time),
});

// Utils function

export const isInTheFuture = ({ date, time }: IDateTime) => {
  const now = new Date();
  return isAfter(toDate({ date, time }), now);
};

export const isInThePast = ({ date, time }: IDateTime) => {
  const now = new Date();
  return isBefore(toDate({ date, time }), now);
};

export const isInOrder = ({
  first,
  last,
}: {
  first: IDateTime;
  last: IDateTime;
}) => {
  if (datesInOrder({ first: first.date, last: last.date })) {
    return true;
  }
  if (
    isSameDate(first.date, last.date) &&
    timesInOrder({ first: first.time, last: last.time })
  ) {
    return true;
  }
  return false;
};

const toDate = ({ date, time }: IDateTime) =>
  new Date(date.year, date.month - 1, date.day, time.hour, time.minute);

export const stringifyDateTime = ({ date, time }: IDateTime) =>
  `${stringifyDate(date)}T${stringifyTime(time)}`;
