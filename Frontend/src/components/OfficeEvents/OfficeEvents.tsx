import React, {useState} from 'react';
import {WavySubHeader} from "src/components/Common/Header/WavySubHeader";
import style from './OfficeEvents.module.scss';
import {eachWeekOfInterval, endOfMonth, startOfMonth, addDays, getWeek, addMonths} from "date-fns";
import classnames from "classnames";
import {useOfficeEvents} from "src/hooks/cache";
import {isLoading, isNotRequested} from "src/remote-data";
import {månedsNavn} from "src/types/date";
import {Chevron} from "src/components/Common/Chevron/Chevron";
import {Arrow} from "src/components/Common/Arrow/Arrow";

export const OfficeEvents = () => {
  const [currentDate, setCurrentDate] = useState(new Date())
  const weekdaysInMonth = getWeekdaysInMonth(currentDate)
  const incrementMonth = () =>
    setCurrentDate(addMonths(new Date(currentDate), 1))
  const decrementMonth = () =>
    setCurrentDate(addMonths(new Date(currentDate), -1))

  const officeEvents = useOfficeEvents(currentDate)

  if (isNotRequested(officeEvents) || isLoading(officeEvents)) {
    return null;
  }

  return (
    <>
      <WavySubHeader eventId={'all-events'}>
        <div role="heading" aria-level={3} className={style.header}>
          <h1 className={style.headerText}>Hva skjer i Bekk?</h1>
        </div>
      </WavySubHeader>
      <table>
        <caption>
          <div>
            <Arrow onClick={decrementMonth} className={style.arrow} direction="left" color="white" noCircle />
            <p>{månedsNavn[currentDate.getMonth()]}</p>
            <Arrow onClick={incrementMonth} className={style.arrow} direction="right" color="white" noCircle />
          </div>
        </caption>
        <thead>
        <tr>
          <th>Mandag</th>
          <th>Tirsdag</th>
          <th>Onsdag</th>
          <th>Torsdag</th>
          <th>Fredag</th>
          <th>Lørdag</th>
          <th>Søndag</th>
        </tr>
        </thead>
        <tbody>
        {weekdaysInMonth.map(days => <WeekDayCards key={days.toLocaleString()} days={days}/>)}
        </tbody>
      </table>
    </>
  )
}

const WeekDayCards = ({days}: { days: Date[] }) => {
  return (
    <tr key={getWeek(days[0])} data-label={getWeek(days[0])}>
      {days.map(day => {
        const borderStyle = classnames({
          [style.oldDay]: day < addDays(new Date(), -1)
        })
        const dateStyle = classnames({
          [style.oldDate]: day < addDays(new Date(), -1),
          [style.dateToday]: day.toDateString() === new Date().toDateString()
        })
        return (
          <td className={borderStyle} key={day.getDate()}>
            <div className={dateStyle}>
              {day.getDate()}
            </div>
          </td>
        )
      })}
    </tr>
  )
}

const getWeekdaysInMonth = (date: Date) => {
  const start = startOfMonth(date);
  const end = endOfMonth(date);
  const eachWeek = eachWeekOfInterval({start, end}, {weekStartsOn: 1})
  return eachWeek.map((date) => {
    const weekdays = new Array(7).fill(0);
    return weekdays.map((_, dayInWeek) => addDays(date, dayInWeek))
  })
}
