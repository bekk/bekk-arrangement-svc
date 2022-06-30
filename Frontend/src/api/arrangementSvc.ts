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

export const postEvent = (
  event: IEvent,
  editUrlTemplate: string
): Promise<INewEventViewModel> =>
  post({
    host: "",
    path: '/events',
    body: toEventWriteModel(event, editUrlTemplate),
  });

export const putEvent = (
  eventId: string,
  event: IEvent,
  editToken?: string
): Promise<IEventViewModel> =>
  put({
    host: "",
    path: `/events/${eventId}${queryStringStringify({ editToken })}`,
    body: toEventWriteModel(event),
  });

export const getEvent = (eventId: string): Promise<IEventViewModel> =>
  get({
    host: "",
    path: `/events/${eventId}`,
  });

export const getEvents = (): Promise<WithId<IEventViewModel>[]> =>
  get({
    host: "",
    path: `/events`,
  });

export const getPastEvents = (): Promise<WithId<IEventViewModel>[]> =>
  get({
    host: "",
    path: `/events/previous`,
  });

export const deleteEvent = (
  eventId: string,
  cancellationMessage: string,
  editToken?: string
) =>
  del({
    host: "",
    path: `/events/${eventId}${queryStringStringify({ editToken })}`,
    body: cancellationMessage,
  });

export const getParticipantsForEvent = (
  eventId: string,
  editToken?: string
): Promise<IParticipantViewModelsWithWaitingList> =>
  get({
    host: "",
    path: `/events/${eventId}/participants${queryStringStringify({
      editToken,
    })}`,
  });

export const getNumberOfParticipantsForEvent = (
  eventId: string
): Promise<number> =>
  get({
    host: "",
    path: `/events/${eventId}/participants/count`,
  });

export const getParticipantExportResponse = (
  eventId: string
): Promise<Response> =>
  getResponse({
    host: "",
    path: `/events/${eventId}/participants/export`,
  });

export const getWaitinglistSpot = (
  eventId: string,
  email: string
): Promise<number> =>
  get({
    host: "",
    path: `/events/${eventId}/participants/${encodeURIComponent(
      email
    )}/waitinglist-spot`,
  });

export const postParticipant = (
  eventId: string,
  participant: IParticipant,
  cancelUrlTemplate: string
): Promise<INewParticipantViewModel> =>
  post({
    host: "",
    path: `/events/${eventId}/participants/${encodeURIComponent(
      toEmailWriteModel(participant.email)
    )}`,
    body: toParticipantWriteModel(participant, cancelUrlTemplate),
  });

export const deleteParticipant = ({
  eventId,
  participantEmail,
  cancellationToken,
}: {
  eventId: string;
  participantEmail: string;
  cancellationToken?: string;
}) =>
  del({
    host: "",
    path: `/events/${eventId}/participants/${encodeURIComponent(
      participantEmail
    )}${queryStringStringify({ cancellationToken })}`,
  });

export const getEventsAndParticipationsForEmployee = (
  employeeId: number
): Promise<{
  editableEvents: EditEventToken[];
  participations: Participation[];
}> =>
  get({
    host: "",
    path: `/events-and-participations/${employeeId}`,
  });

export const getEventIdByShortname = (shortname: string): Promise<string> =>
  get({
    host: "",
    path: `/events/id${queryStringStringify({
      shortname: encodeURIComponent(shortname),
    })}`,
  });
