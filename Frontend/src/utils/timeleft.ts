import { plural } from 'src/utils';

export interface ITimeLeft {
  days: number;
  hours: number;
  minutes: number;
  seconds: number;
  difference: number;
}

export const asString = ({ days, hours, minutes, seconds }: ITimeLeft) => {
  const dager = plural(days, 'dag', 'dager');
  const timer = plural(hours, 'time', 'timer');
  const minutter = plural(minutes, 'minutt', 'minutter');
  const sekunder = plural(seconds, 'sekund', 'sekunder');

  if (days > 0) {
    return `${dager}, ${timer} og ${minutter}`;
  }
  if (hours > 0) {
    return `${timer}, ${minutter} og ${sekunder}`;
  }
  if (minutes > 0) {
    return `${minutter} og ${sekunder}`;
  }
  if (seconds >= 0) {
    return `${sekunder}`;
  }
};

export const calculateTimeLeft = (date: Date): ITimeLeft => {
  const differenceInMs = date.valueOf() - new Date().valueOf();

  if (differenceInMs > 0) {
    return {
      days: Math.floor(differenceInMs / (1000 * 60 * 60 * 24)),
      hours: Math.floor((differenceInMs / (1000 * 60 * 60)) % 24),
      minutes: Math.floor((differenceInMs / 1000 / 60) % 60),
      seconds: Math.ceil((differenceInMs / 1000) % 60),
      difference: differenceInMs,
    };
  }

  return {
    days: 0,
    hours: 0,
    minutes: 0,
    seconds: 0,
    difference: 0,
  };
};
