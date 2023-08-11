import { useRouteMatch } from 'react-router';
import { queryStringStringify } from 'src/utils/browser-state';
import { IEvent } from './types/event';

export const eventIdKey = 'eventId';
export const emailKey = 'email';
export const editTokenKey = 'editToken';
export const cancellationTokenKey = 'cancellationToken';
export const shortnameKey = 'shortname';

export const rootRoute = '/';
export const eventsRoute = '/events';
export const createRoute = '/events/create';
export const previewNewEventRoute = `/events/create/preview`;
export const officeEventsMonthKey = 'date';

export const viewEventShortnameRoute = (shortname: string) => `/${shortname}`;
export const viewEventRoute = (eventId: string) => `/events/${eventId}`;

export const createViewUrlTemplate = (event: IEvent) => {
  const hostAndProtocol = document.location.origin;
  return event.shortname
    ? hostAndProtocol + viewEventShortnameRoute('{shortname}')
    : hostAndProtocol + viewEventRoute('{eventId}');
};

export const officeEventRoute = (date: string) => `/office-events/${date}`;

export const editEventRoute = (eventId: string, editToken?: string) =>
  `/events/${eventId}/edit${queryStringStringify({
    [editTokenKey]: editToken,
  })}`;

export const editUrlTemplate =
  document.location.origin + editEventRoute('{eventId}', '{editToken}');

export const previewEventRoute = (eventId: string) =>
  `/events/${eventId}/preview`;

export const confirmParticipantRoute = ({
  eventId,
  email,
}: {
  eventId: string;
  email: string;
}) => `/events/${eventId}/confirm/${email}`;

export const cancelParticipantRoute = ({
  eventId,
  email,
  cancellationToken,
}: {
  eventId: string;
  email: string;
  cancellationToken?: string;
}) =>
  `/events/${eventId}/cancel/${email}${queryStringStringify({
    [cancellationTokenKey]: cancellationToken,
  })}`;

export const cancelParticipationUrlTemplate =
  document.location.origin +
  cancelParticipantRoute({
    eventId: '{eventId}',
    email: '{email}',
    cancellationToken: '{cancellationToken}',
  });

export const useIsEditingRoute = () => {
  const routeMatch = useRouteMatch(editEventRoute(':' + eventIdKey));
  return routeMatch?.isExact;
};

export const useIsCreateRoute = () => {
  const routeMatch = useRouteMatch(createRoute);
  return routeMatch?.isExact;
};
