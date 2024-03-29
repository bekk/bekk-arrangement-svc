import {
  IEvent,
  IEventViewModel,
  INewEventViewModel,
  toEventWriteModel,
} from 'src/types/event';
import { post, get, del, put, getResponse } from './crud';
import { WithId } from 'src/types';
import {
  IParticipant,
  INewParticipantViewModel,
  toParticipantWriteModel,
  IParticipantViewModelsWithWaitingList,
} from 'src/types/participant';
import { queryStringStringify } from 'src/utils/browser-state';
import { toEmailWriteModel } from 'src/types/email';
import { EditEventToken, Participation } from 'src/hooks/saved-tokens';
import { OfficeEvent, OfficeEventDecoder } from '../types/OfficeEvent';

export const postEvent = (event: IEvent): Promise<INewEventViewModel> =>
  post({
    host: '',
    path: '/api/events',
    body: toEventWriteModel(event),
  });

export const putEvent = (
  eventId: string,
  event: IEvent,
  editToken?: string
): Promise<IEventViewModel> => {
  return put({
    host: '',
    path: `/api/events/${eventId}${queryStringStringify({ editToken })}`,
    body: toEventWriteModel(event),
  });
};

export const getEvent = (eventId: string): Promise<IEventViewModel> =>
  get({
    host: '',
    path: `/api/events/${eventId}`,
  });

export const getEvents = (): Promise<WithId<IEventViewModel>[]> =>
  get({
    host: '',
    path: `/api/events`,
  });

export const getPastEvents = (): Promise<WithId<IEventViewModel>[]> =>
  get({
    host: '',
    path: `/api/events/previous`,
  });

export const deleteEvent = (
  eventId: string,
  cancellationMessage: string,
  editToken?: string
) =>
  del({
    host: '',
    path: `/api/events/${eventId}${queryStringStringify({ editToken })}`,
    body: cancellationMessage,
  });

export const getParticipantsForEvent = (
  eventId: string,
  editToken?: string
): Promise<IParticipantViewModelsWithWaitingList> =>
  get({
    host: '',
    path: `/api/events/${eventId}/participants${queryStringStringify({
      editToken,
    })}`,
  });

export const getNumberOfParticipantsForEvent = (
  eventId: string
): Promise<number> =>
  get({
    host: '',
    path: `/api/events/${eventId}/participants/count`,
  });

export const getParticipantExportResponse = (
  eventId: string
): Promise<Response> =>
  getResponse({
    host: '',
    path: `/api/events/${eventId}/participants/export`,
  });

export const getWaitinglistSpot = (
  eventId: string,
  email: string
): Promise<number> =>
  get({
    host: '',
    path: `/api/events/${eventId}/participants/${encodeURIComponent(
      email
    )}/waitinglist-spot`,
  });

export const postParticipant = (
  event: IEvent,
  eventId: string,
  participant: IParticipant
): Promise<INewParticipantViewModel> =>
  post({
    host: '',
    path: `/api/events/${eventId}/participants/${encodeURIComponent(
      toEmailWriteModel(participant.email)
    )}`,
    body: toParticipantWriteModel(participant, event),
  });

export const deleteParticipant = ({
  eventId,
  participantEmail,
  cancellationToken,
  editToken,
}: {
  eventId: string;
  participantEmail: string;
  cancellationToken?: string;
  editToken?: string;
}) =>
  del({
    host: '',
    path: `/api/events/${eventId}/participants/${encodeURIComponent(
      participantEmail
    )}${queryStringStringify({ cancellationToken, editToken })}`,
  });

export const getEventsAndParticipationsForEmployee = (
  employeeId: number
): Promise<{
  editableEvents: EditEventToken[];
  participations: Participation[];
}> =>
  get({
    host: '',
    path: `/api/events-and-participations/${employeeId}`,
  });

export const getEventIdByShortname = (shortname: string): Promise<string> =>
  get({
    host: '',
    path: `/api/events/id${queryStringStringify({
      shortname: encodeURIComponent(shortname),
    })}`,
  });

export const getOfficeEventsByDate = (date: string): Promise<OfficeEvent[]> =>
  get({
    host: '',
    path: `/api/office-events/${date}`,
  }).then((x) => x.map(OfficeEventDecoder));
