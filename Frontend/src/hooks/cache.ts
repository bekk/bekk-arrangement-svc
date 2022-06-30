import { cachedRemoteData, hasLoaded } from 'src/remote-data';
import { IEvent, parseEventViewModel } from 'src/types/event';
import { useCallback } from 'react';
import {
  getEvent,
  getEventIdByShortname,
  getEvents,
  getNumberOfParticipantsForEvent,
  getParticipantsForEvent,
  getPastEvents,
  getWaitinglistSpot,
} from 'src/api/arrangementSvc';
import {
  parseParticipantViewModel,
  IParticipantsWithWaitingList,
} from 'src/types/participant';
import { isInThePast } from 'src/types/date-time';
import { getEmailAndName } from 'src/api/employeeSvc';
import { getEmployeeId } from 'src/auth';
import { EventState } from 'src/components/ViewEventsCards/ParticipationState';

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

export const useEvents = () => {
  return eventCache.useAll(
    useCallback(async () => {
      const [eventContracts, eventsContractPastEvents] = await Promise.all([
        getEvents(),
        getPastEvents(),
      ]);
      return eventContracts
        .concat(eventsContractPastEvents)
        .map(({ id, ...event }) => {
          return [id, parseEventViewModel(event)];
        });
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

export const usePastEvents = () => {
  const map = useEvents();
  return new Map(
    [...map].filter(
      ([_, event]) => hasLoaded(event) && isInThePast(event.data.end)
    )
  );
};

export const useUpcomingEvents = () => {
  const map = useEvents();
  return new Map(
    [...map].filter(
      ([_, event]) => hasLoaded(event) && !isInThePast(event.data.end)
    )
  );
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

const userEmailAndNameCache = cachedRemoteData<
  string,
  { name: string; email: string } | undefined
>();

export const useEmailAndName = () => {
  let employeeId: null | number;
  try {
    employeeId = getEmployeeId();
  } catch {
    employeeId = null;
  }
  return userEmailAndNameCache.useOne({
    key: '',
    fetcher: useCallback(async () => {
      if (employeeId === null) {
        return undefined;
      }
      const userAndEmail = await getEmailAndName(employeeId);
      return userAndEmail;
    }, [employeeId]),
  });
};
