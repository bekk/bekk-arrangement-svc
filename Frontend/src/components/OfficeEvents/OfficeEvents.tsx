import React, {useState} from 'react';
import {WavySubHeader} from "src/components/Common/Header/WavySubHeader";
import style from './OfficeEvents.module.scss';
import {eachWeekOfInterval, endOfMonth, startOfMonth, addDays, getWeek, addMonths} from "date-fns";
import classnames from "classnames";
import {useOfficeEvents} from "src/hooks/cache";
import {isBad, isLoading, isNotRequested} from "src/remote-data";
import {månedsNavn} from "src/types/date";
import {Arrow} from "src/components/Common/Arrow/Arrow";
import { OfficeEvent} from "src/types/event";

export const OfficeEvents = () => {
  const [currentDate, setCurrentDate] = useState(new Date())
  const incrementMonth = () =>
    setCurrentDate(addMonths(new Date(currentDate), 1))
  const decrementMonth = () =>
    setCurrentDate(addMonths(new Date(currentDate), -1))

  const officeEvents = useOfficeEvents(currentDate)

  if (isNotRequested(officeEvents) || isLoading(officeEvents)) {
    return null;
  }

  if (isBad(officeEvents)) {
    return (
      <div>
        FEIL
      </div>
    );
  }

  const weekdaysAndEvents: DayAndEvents[][] =
    getWeekdaysInMonth(currentDate).map(weeks =>
      weeks.map(day => {
        return { day,
                 events: officeEvents.data.filter(event => event.startTime.toDateString() === day.toDateString()) }}))

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
        {weekdaysAndEvents.map((daysAndEvents, i) => <WeekDayCards key={i} daysAndEvents={daysAndEvents}/>)}
        </tbody>
      </table>
    </>
  )
}

const WeekDayCards = ({daysAndEvents}: { daysAndEvents: DayAndEvents[] }) => {
  return (
    <tr key={getWeek(daysAndEvents[0].day)} data-label={getWeek(daysAndEvents[0].day)}>
      {daysAndEvents.map(dayAndEvents => {
        const {day, events} = dayAndEvents
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
              {events.map(event => <Event key={`${event.title}:${event.contactPerson}`} event={event}/>)}
            </div>
          </td>
        )
      })}
    </tr>
  )
}

const Event = ({event}: {event: OfficeEvent}) => {
  const eventStyle = classnames(style.event, {
    [style.eventSolkontrast]: false,
    [style.eventHavkontrast]: false,
    [style.eventKveldkontrast]: false,
    [style.eventSolnedgangKontrast]: false,
  })
  return (<p className={eventStyle}>{event.title}</p>)
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

type DayAndEvents = {
  day: Date,
  events: OfficeEvent[]
}
