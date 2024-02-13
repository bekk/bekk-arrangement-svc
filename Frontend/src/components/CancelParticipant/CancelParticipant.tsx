import React, { useState } from 'react';
import { deleteParticipant } from 'src/api/arrangementSvc';
import { Page } from '../Page/Page';
import { Button } from '../Common/Button/Button';
import { stringifyDate } from 'src/types/date';
import { stringifyTime } from 'src/types/time';
import style from './CancelParticipant.module.scss';
import { useNotification } from '../NotificationHandler/NotificationHandler';
import { hasLoaded, isBad } from 'src/remote-data';
import {
  viewEventRoute,
  cancellationTokenKey,
  emailKey,
  eventIdKey,
} from 'src/routing';
import { useQuery, useParam } from 'src/utils/browser-state';
import { BlockLink } from 'src/components/Common/BlockLink/BlockLink';
import { useEvent, useWaitinglistSpot } from 'src/hooks/cache';
import { useSavedParticipations } from 'src/hooks/saved-tokens';
import { useSetTitle } from 'src/hooks/setTitle';
import { appTitle } from 'src/Constants';
import { Spinner } from 'src/components/Common/Spinner/spinner';
import {
  authenticateUser,
  isAuthenticated,
  needsToAuthenticate,
} from 'src/auth';

export const CancelParticipant = () => {
  const eventId = useParam(eventIdKey);
  const participantEmail = decodeURIComponent(useParam(emailKey));
  const cancellationToken = useQuery(cancellationTokenKey);
  useSetTitle(appTitle);

  const remoteEvent = useEvent(eventId);

  const [wasDeleted, setWasDeleted] = useState(false);
  const { catchAndNotify } = useNotification();
  const { removeSavedParticipant } = useSavedParticipations();

  const remoteWaitinglistSpot = useWaitinglistSpot(eventId, participantEmail);
  const isWaitlisted = hasLoaded(remoteWaitinglistSpot)
    ? remoteWaitinglistSpot.data !== 'ikke-påmeldt' &&
      remoteWaitinglistSpot.data >= 1
    : undefined;

  const cancelParticipant = catchAndNotify(async () => {
    if (eventId && participantEmail) {
      await deleteParticipant({
        eventId,
        participantEmail,
        cancellationToken,
      });
      setWasDeleted(true);
      removeSavedParticipant({ eventId, email: participantEmail });
    }
  });

  if (isBad(remoteEvent)) {
    if (!isAuthenticated() && needsToAuthenticate(remoteEvent.statusCode)) {
      authenticateUser();
      return <p className={style.errorText}>Redirigerer til innlogging</p>;
    }
    return <p className={style.errorText}>{remoteEvent.userMessage}</p>;
  }

  if (!hasLoaded(remoteEvent)) {
    return <Spinner />;
  }

  const event = remoteEvent.data;

  const HasCancelledView = () => (
    <>
      <h1 className={style.header}>Avmelding bekreftet!</h1>
      <div className={style.text}>
        Da er du avmeldt {event.title} den {stringifyDate(event.start.date)} kl{' '}
        {stringifyTime(event.start.time)} - {stringifyTime(event.end.time)}
      </div>
      <div className={style.buttonContainer}>
        <BlockLink onLightBackground to={viewEventRoute(eventId)}>
          Tilbake til arrangement
        </BlockLink>
      </div>
    </>
  );

  const CancelView = () => (
    <>
      <h1 className={style.header}>Avmelding</h1>
      <div className={style.text}>Vil du melde deg av {event.title}?</div>
      {isWaitlisted === true && hasLoaded(remoteWaitinglistSpot) && (
        <div className={style.text}>
          {`Du er på plass ${remoteWaitinglistSpot.data} på ventelisten`}
        </div>
      )}
      <div className={style.buttonContainer}>
        <Button onClick={cancelParticipant}>Meld av</Button>
        <BlockLink onLightBackground to={viewEventRoute(eventId)}>
          Tilbake til arrangement
        </BlockLink>
      </div>
    </>
  );

  return <Page>{wasDeleted ? <HasCancelledView /> : <CancelView />}</Page>;
};
