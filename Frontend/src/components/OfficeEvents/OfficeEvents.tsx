import React, { useState } from 'react';
import { WavySubHeader } from 'src/components/Common/Header/WavySubHeader';
import style from './OfficeEvents.module.scss';
import {
  eachWeekOfInterval,
  endOfMonth,
  startOfMonth,
  addDays,
  getWeek,
  addMonths,
} from 'date-fns';
import classnames from 'classnames';
import { useOfficeEvents } from 'src/hooks/cache';
import { isBad, isLoading, isNotRequested } from 'src/remote-data';
import {
  dateAsText,
  dateToStringWithoutTime,
  månedsNavn,
} from 'src/types/date';
import { Arrow } from 'src/components/Common/Arrow/Arrow';
import { Modal } from 'src/components/Common/Modal/Modal';
import { dateToTime } from 'src/types/time';
import { useHistory } from 'react-router';
import { officeEventRoute, officeEventsMonthKey } from 'src/routing';
import { useParam } from 'src/utils/browser-state';
import { useEffectOnce } from 'src/hooks/utils';
import { Spinner } from 'src/components/Common/Spinner/spinner';
import { OfficeEvent } from 'src/types/OfficeEvent';

export const OfficeEvents = () => {
  const urlDate = useParam(officeEventsMonthKey);
  const history = useHistory();
  const parsedDate = isNaN(Date.parse(urlDate))
    ? new Date()
    : new Date(urlDate);
  useEffectOnce(() =>
    history.push(officeEventRoute(dateToStringWithoutTime(parsedDate)))
  );
  const [selectedEvent, setSelectedEvent] = useState<OfficeEvent | undefined>(
    undefined
  );
  const [currentDate, setCurrentDate] = useState(parsedDate);
  const incrementMonth = () => {
    const date = addMonths(currentDate, 1);
    setCurrentDate(date);
    history.push(officeEventRoute(dateToStringWithoutTime(date)));
  };
  const decrementMonth = () => {
    const date = addMonths(currentDate, -1);
    setCurrentDate(date);
    history.push(officeEventRoute(dateToStringWithoutTime(date)));
  };

  const officeEvents = useOfficeEvents(currentDate);

  if (isNotRequested(officeEvents) || isLoading(officeEvents)) {
    return <Spinner />;
  }

  if (isBad(officeEvents)) {
    return (
      <div className={style.error}>
        Det har skjedd en feil under henting av events fra office.
      </div>
    );
  }

  const weekdaysAndEvents: DayAndEvents[][] = getWeekdaysInMonth(
    currentDate
  ).map((weeks) =>
    weeks.map((day) => {
      return {
        day,
        events: officeEvents.data.filter(
          (event) => event.startTime.toDateString() === day.toDateString()
        ),
      };
    })
  );

  return (
    <>
      <WavySubHeader eventId={'all-events'}>
        <div role="heading" aria-level={3} className={style.header}>
          <h1 className={style.headerText}>Hva skjer i Bekk?</h1>
        </div>
      </WavySubHeader>
      {selectedEvent !== undefined && (
        <EventModal
          event={selectedEvent}
          closeModal={() => setSelectedEvent(undefined)}
        />
      )}
      <table className={style.table}>
        <caption>
          <div>
            <Arrow
              onClick={decrementMonth}
              className={style.arrow}
              direction="left"
              color="white"
              noCircle
            />
            <p>{månedsNavn[currentDate.getMonth()]}</p>
            <Arrow
              onClick={incrementMonth}
              className={style.arrow}
              direction="right"
              color="white"
              noCircle
            />
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
          {weekdaysAndEvents.map((daysAndEvents, i) => (
            <WeekDayCards
              key={i}
              daysAndEvents={daysAndEvents}
              setSelectedEvent={setSelectedEvent}
            />
          ))}
        </tbody>
      </table>
    </>
  );
};

const WeekDayCards = ({
  daysAndEvents,
  setSelectedEvent,
}: {
  daysAndEvents: DayAndEvents[];
  setSelectedEvent: (x: OfficeEvent) => void;
}) => {
  const currentDate = new Date();
  const isPreviousDay = (day: Date) => day < currentDate;
  const isToday = (day: Date) =>
    day.toDateString() === currentDate.toDateString();
  const isNextMonth = (day: Date) => day.getMonth() > currentDate.getMonth();
  const weekNr = getWeek(daysAndEvents[0].day);
  return (
    <tr key={weekNr} data-label={weekNr}>
      {daysAndEvents.map((dayAndEvents) => {
        const { day, events } = dayAndEvents;
        const borderStyle = classnames({
          [style.notCurrentMonthDay]: isPreviousDay(day),
        });
        const dateStyle = classnames(style.tableContent, {
          [style.notCurrentMonthDate]: isPreviousDay(day),
          [style.currentDate]: isToday(day) || isNextMonth(day),
        });
        const dateHighlighter = classnames({
          [style.todayHighlighter]: isToday(day),
        });
        return (
          <td data-label={weekNr} className={borderStyle} key={day.getDate()}>
            <div className={dateStyle}>
              <div className={dateHighlighter}>{day.getDate()}</div>
              {events.map((event) => (
                <Event
                  isPreviousDayOrNextMonth={
                    isPreviousDay(day) || isNextMonth(day)
                  }
                  key={`${event.title}:${event.contactPerson}`}
                  event={event}
                  setSelectedEvent={setSelectedEvent}
                />
              ))}
            </div>
          </td>
        );
      })}
    </tr>
  );
};

const Event = ({
  isPreviousDayOrNextMonth,
  event,
  setSelectedEvent,
}: {
  isPreviousDayOrNextMonth: boolean;
  event: OfficeEvent;
  setSelectedEvent: (x: OfficeEvent) => void;
}) => {
  // Tanken bak at disse alltid er false er å på sikt kunne differensiere mellom forskjellige typer events og fargelegge dem forskjellig.
  const eventStyle = classnames(style.event, {
    [style.eventOverskyetKontrast]: isPreviousDayOrNextMonth,
    [style.eventSolkontrast]: false,
    [style.eventHavkontrast]: false,
    [style.eventKveldkontrast]: false,
    [style.eventSolnedgangKontrast]: false,
  });
  return (
    <p onClick={() => setSelectedEvent(event)} className={eventStyle}>
      {event.title}
    </p>
  );
};

const EventModal = ({
  event,
  closeModal,
}: {
  event: OfficeEvent;
  closeModal: () => void;
}) => {
  return (
    <Modal closeModal={closeModal}>
      <div className={style.eventDate}>
        {dateAsText(event.startTime)}, {dateToTime(event.startTime)} -{' '}
        {dateToTime(event.endTime)}
      </div>
      <div className={style.eventLocation}>
        <div>{event.location} </div>
        <div>{event.contactPerson} </div>
      </div>
      <hr />
      {/*TODO: Her vil vi ha kategori*/}
      <div></div>
      <h2>{event.title}</h2>
      <p className={style.eventDescription}>{event.description}</p>
      {/*TODO: Her vil vi ha temaer*/}
      <div></div>
    </Modal>
  );
};

const getWeekdaysInMonth = (date: Date) => {
  const start = startOfMonth(date);
  const end = endOfMonth(date);
  const eachWeek = eachWeekOfInterval({ start, end }, { weekStartsOn: 1 });
  return eachWeek.map((date) => {
    const weekdays = new Array(7).fill(0);
    return weekdays.map((_, dayInWeek) => addDays(date, dayInWeek));
  });
};

type DayAndEvents = {
  day: Date;
  events: OfficeEvent[];
};
