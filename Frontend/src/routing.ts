import { useRouteMatch } from 'react-router';
import { queryStringStringify } from 'src/utils/browser-state';

export const eventIdKey = 'eventId';
export const emailKey = 'email';
export const editTokenKey = 'editToken';
export const cancellationTokenKey = 'cancellationToken';
export const shortnameKey = 'shortname';

export const rootRoute = '/';
export const eventsRoute = '/events';
export const createRoute = '/events/create';
export const previewNewEventRoute = `/events/create/preview`;

export const viewEventShortnameRoute = (shortname: string) => `/${shortname}`;
export const viewEventRoute = (eventId: string) => `/events/${eventId}`;

export const editEventRoute = (eventId: string, editToken?: string) =>
  `/events/${eventId}/edit${queryStringStringify({
    [editTokenKey]: editToken,
  })}`;

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

export const useIsEditingRoute = () => {
  let routematch = useRouteMatch(editEventRoute(':' + eventIdKey));
  return routematch !== null;
};
export const useIsPreviewRoute = () => {
  let routematch = useRouteMatch(previewEventRoute(':' + eventIdKey));
  return routematch !== null;
};

export const useIsCreateRoute = () => {
  let routematch = useRouteMatch(createRoute);
  return routematch !== null;
};
