import { cachedRemoteData, RemoteData } from 'src/remote-data';
import {IEvent, IEventViewModel, parseEventViewModel} from 'src/types/event';
import { useCallback, useMemo } from 'react';
import {
  getEvent,
  getEventIdByShortname,
  getEvents,
  getNumberOfParticipantsForEvent,
  getOfficeEventsByDate,
  getParticipantsForEvent,
  getPastEvents,
  getWaitinglistSpot,
} from 'src/api/arrangementSvc';
import {
  parseParticipantViewModel,
  IParticipantsWithWaitingList,
} from 'src/types/participant';
import { getEmailNameAndDepartment } from 'src/api/employeeSvc';
import { getEmployeeId } from 'src/auth';
import { EventState } from 'src/components/ViewEventsCards/ParticipationState';
import { OfficeEvent } from '../types/OfficeEvent';
import {isInOrder} from "../types/date-time";
import { WithId } from 'src/types';

//**  Event  **//

const eventCache = cachedRemoteData<string, IEvent>();

export const useEvent = (id: string) => {
  return eventCache.useOne({
    key: id,
    fetcher: useCallback(async () => {
      const retrievedEvent = await getEvent(id);
      return parseEventViewModel(retrievedEvent);
    }, [id]),
  });
};

export const useEvents = (): Map<string, RemoteData<IEvent>> => {
  return eventCache.useAll(
    useCallback(async () => {
      const futureEvents = await getEvents();
      const pastEvents = await getPastEvents();
      const allEvents = [...sortEventsByStartDate(futureEvents), ...sortEventsByEndDate(pastEvents)];
      return allEvents.map(({ id, ...event }) => [
        id,
        parseEventViewModel(event),
      ]) as [string, IEvent][];
    }, [])
  );
};

const shortnameCache = cachedRemoteData<string, string>();

export const useShortname = (shortname: string) => {
  return shortnameCache.useOne({
    key: shortname,
    fetcher: useCallback(async () => {
      return getEventIdByShortname(shortname);
    }, [shortname]),
  });
};

const officeEventCache = cachedRemoteData<string, OfficeEvent[]>();
export const useOfficeEvents = (date: Date) => {
  const dateKey = useMemo(() => new Date(date).toISOString(), [date]);
  return officeEventCache.useOne({
    key: dateKey,
    fetcher: useCallback(async () => {
      return getOfficeEventsByDate(dateKey);
    }, [dateKey]),
  });
};

//**  Participant  **//

const participantsCache = cachedRemoteData<
  string,
  IParticipantsWithWaitingList
>();

export const useParticipants = (eventId: string, editToken?: string) => {
  return participantsCache.useOne({
    key: eventId,
    fetcher: useCallback(async () => {
      const { attendees, waitingList } = await getParticipantsForEvent(
        eventId,
        editToken
      );
      return {
        attendees: attendees.map(parseParticipantViewModel),
        waitingList: waitingList?.map(parseParticipantViewModel),
      };
    }, [eventId, editToken]),
  });
};

const numberOfParticipantsCache = cachedRemoteData<string, number>();

export const useNumberOfParticipants = (eventId: string) => {
  return numberOfParticipantsCache.useOne({
    key: eventId,
    fetcher: useCallback(async () => {
      const numberOfParticipants = await getNumberOfParticipantsForEvent(
        eventId
      );
      return numberOfParticipants;
    }, [eventId]),
  });
};

const waitinglistSpotCache = cachedRemoteData<
  string,
  number | EventState.IkkePameldt
>();

export const useWaitinglistSpot = (eventId: string, email?: string) => {
  return waitinglistSpotCache.useOne({
    key: `${eventId}:${email}`,
    fetcher: useCallback(async () => {
      if (email === undefined) {
        return EventState.IkkePameldt;
      }
      const waitinglistSpot = await getWaitinglistSpot(eventId, email);
      return waitinglistSpot;
    }, [eventId, email]),
  });
};

const userEmailNameAndDepartmentCache = cachedRemoteData<
  string,
  { name: string; email: string; department: string } | undefined
>();

export const useEmailNameAndDepartment = () => {
  let employeeId: null | number;
  try {
    employeeId = getEmployeeId();
  } catch {
    employeeId = null;
  }
  return userEmailNameAndDepartmentCache.useOne({
    key: '',
    fetcher: useCallback(async () => {
      if (employeeId === null) {
        return undefined;
      }
      return await getEmailNameAndDepartment(employeeId);
    }, [employeeId]),
  });
};

const sortEventsByStartDate = (events: WithId<IEventViewModel>[]) => {
    return events.sort((a, b) =>
        isInOrder({ first: a.startDate, last: b.startDate }) ? -1 : 1
    );
};

const sortEventsByEndDate = (events: WithId<IEventViewModel>[]) => {
    return events.sort((a, b) =>
        isInOrder({ first: a.startDate, last: b.startDate }) ? 1 : -1
    );
};
