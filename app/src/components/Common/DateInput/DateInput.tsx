import React, { useState } from 'react';
import style from './DateInput.module.scss';
import { EditDate } from 'src/types/date';
import {
  CalendarDate,
  monthNumber,
  parseIso8601String,
  serializeIso8601String,
  addMonths,
  CalendarMonth,
  startOfWeek,
  lastDateInMonth,
  addDays,
  periodOfDates,
  datesEqual,
  calendarDateFromJsDateObject,
} from 'typescript-calendar-date';
import classNames from 'classnames';
import { useOnClickOutside } from 'src/hooks/navigation';
import { Arrow } from 'src/components/Common/Arrow/Arrow';

interface IProps {
  value: EditDate;
  onChange: (value: EditDate) => void;
  label?: string;
}

export const DateInput = ({ value, onChange, label }: IProps): JSX.Element => {
  const [isOpen, setIsOpen] = useState(false);
  const ref = useOnClickOutside(() => setIsOpen(false));

  const date = parseIso8601String(value);
  const setDate = (date: CalendarDate) =>
    onChange(serializeIso8601String(date));

  const [displayMonth, setDisplayMonth] = useState<CalendarMonth>(date);
  const decDisplayMonth = () => setDisplayMonth(addMonths(displayMonth, -1));
  const incDisplayMonth = () => setDisplayMonth(addMonths(displayMonth, 1));

  const firstDayOfDisplayMonth: CalendarDate = { ...displayMonth, day: 1 };
  const startOfFirstDisplayWeek = startOfWeek(firstDayOfDisplayMonth);

  const lastDayOfDisplayMonth: CalendarDate = lastDateInMonth(displayMonth);
  const endOfLastDisplayWeek = addDays(startOfWeek(lastDayOfDisplayMonth), 6);

  const daysInPrevMonth = periodOfDates(
    startOfFirstDisplayWeek,
    addDays(firstDayOfDisplayMonth, -1)
  );

  const daysInDisplayMonth = periodOfDates(
    firstDayOfDisplayMonth,
    lastDayOfDisplayMonth
  );

  const daysInNextMonth = periodOfDates(
    addDays(lastDayOfDisplayMonth, 1),
    endOfLastDisplayWeek
  );

  const today = calendarDateFromJsDateObject(new Date());

  const daysInWeek = ['M', 'T', 'O', 'T', 'F', 'L', 'S'];
  return (
    <>
      <label className={style.textLabel} htmlFor={label}>
        {label}
      </label>
      <div className={style.datePickerContainer}>
        <div className={style.dateInput} onClick={() => setIsOpen(true)}>
          {date.day}. {formatMonthYear(date)}
        </div>
        {isOpen && (
          <div ref={ref} className={style.datepicker}>
            <div className={style.header}>
              <div className={style.button} onClick={decDisplayMonth}>
                <Arrow direction="left" noCircle />
              </div>
              <div className={style.displayMonth}>
                {formatMonthYear(displayMonth)}
              </div>
              <div className={style.button} onClick={incDisplayMonth}>
                <Arrow direction="right" noCircle />
              </div>
            </div>
            <div className={style.grid}>
              {daysInWeek.map((d, i) => (
                <div key={i} className={style.dayInWeek}>
                  {d}
                </div>
              ))}
              {daysInPrevMonth.map((d, i) => (
                <div
                  key={i}
                  className={classNames(style.date, style.inactiveDate, {
                    [style.today]: datesEqual(d, today),
                    [style.selectedInactiveDate]: datesEqual(d, date),
                  })}
                  onClick={() => {
                    decDisplayMonth();
                    setDate(d);
                    setIsOpen(false);
                  }}
                >
                  <div>{d.day}</div>
                </div>
              ))}
              {daysInDisplayMonth.map((d, i) => (
                <div
                  key={i}
                  className={classNames(style.date, style.activeDate, {
                    [style.today]: datesEqual(d, today),
                    [style.selectedActiveDate]: datesEqual(d, date),
                  })}
                  onClick={() => {
                    setDate(d);
                    setIsOpen(false);
                  }}
                >
                  <div>{d.day}</div>
                </div>
              ))}
              {daysInNextMonth.map((d, i) => (
                <div
                  key={i}
                  className={classNames(style.date, style.inactiveDate, {
                    [style.today]: datesEqual(d, today),
                    [style.selectedInactiveDate]: datesEqual(d, date),
                  })}
                >
                  <div
                    onClick={() => {
                      incDisplayMonth();
                      setDate(d);
                      setIsOpen(false);
                    }}
                  >
                    {d.day}
                  </div>
                </div>
              ))}
            </div>
          </div>
        )}
      </div>
    </>
  );
};

const formatMonthYear = ({ month, year }: CalendarMonth) => {
  const månedsNavn = [
    'januar',
    'februar',
    'mars',
    'april',
    'mai',
    'juni',
    'juli',
    'august',
    'september',
    'oktober',
    'november',
    'desember',
  ];
  const monthName = månedsNavn[monthNumber(month) - 1];
  return `${monthName} ${year}`;
};
