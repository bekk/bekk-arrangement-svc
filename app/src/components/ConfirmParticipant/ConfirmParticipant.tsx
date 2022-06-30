import React from 'react';
import { stringifyDate } from 'src/types/date';
import { stringifyTime } from 'src/types/time';
import style from './ConfirmParticipant.module.scss';
import {
  viewEventRoute,
  eventIdKey,
  emailKey,
  viewEventShortnameRoute,
} from 'src/routing';
import { hasLoaded } from 'src/remote-data';
import { BlockLink } from 'src/components/Common/BlockLink/BlockLink';
import { useParam } from 'src/utils/browser-state';
import { useEvent, useWaitinglistSpot } from 'src/hooks/cache';
import { Page } from 'src/components/Page/Page';
import { WavySubHeader } from 'src/components/Common/Header/WavySubHeader';
import {useSetTitle} from "src/hooks/setTitle";
import {appTitle} from "src/Constants";

export const ConfirmParticipant = () => {
  const eventId = useParam(eventIdKey);
  const participantEmail = decodeURIComponent(useParam(emailKey));

  const remoteEvent = useEvent(eventId);
  const remoteWaitinglistSpot = useWaitinglistSpot(eventId, participantEmail);
  useSetTitle(appTitle)

  if (!hasLoaded(remoteEvent) || !hasLoaded(remoteWaitinglistSpot)) {
    return <div>Loading...</div>;
  }

  const event = remoteEvent.data;
  const isWaitlisted =
    remoteWaitinglistSpot.data !== 'ikke-påmeldt' &&
    remoteWaitinglistSpot.data >= 1;

  const viewRoute =
    event.shortname !== undefined
      ? viewEventShortnameRoute(event.shortname)
      : viewEventRoute(eventId);

  return isWaitlisted ? (
    <>
      <WavySubHeader
        eventId={eventId}
        eventTitle={event.title}
        customHexColor={event.customHexColor}
      />
      <Page>
        <h1 className={style.header}>
          Du er nummer {remoteWaitinglistSpot.data} på ventelisten!
        </h1>
        <div className={style.text}>
          Du er nå på venteliste for {event.title} den{' '}
          {stringifyDate(event.start.date)} kl {stringifyTime(event.start.time)}{' '}
          - {stringifyTime(event.end.time)}{' '}
          <span role="img" aria-label="hugging face">
            🤗
          </span>
          <br />
          <br />
          Bekreftelse er sendt på e-post til {participantEmail}. Detaljer for
          avmelding står i e-posten. <br /> Du vil få beskjed på e-post om du
          får plass på arrangementet.
        </div>
        <BlockLink to={viewRoute}>Tilbake til arrangement</BlockLink>
      </Page>
    </>
  ) : (
    <>
      <WavySubHeader
        eventId={eventId}
        eventTitle={event.title}
        customHexColor={event.customHexColor}
      />
      <Page>
        <h1 className={style.header}>Du er påmeldt!</h1>
        <div className={style.text}>
          Gratulerer, du er nå påmeldt {event.title} den{' '}
          {stringifyDate(event.start.date)} kl {stringifyTime(event.start.time)}{' '}
          - {stringifyTime(event.end.time)}! Vi gleder oss til å se deg{' '}
          <span role="img" aria-label="hugging face">
            🤗
          </span>
          <br />
          Bekreftelse er sendt på e-post til {participantEmail}. Detaljer for
          avmelding står i e-posten.
        </div>
        <BlockLink to={viewRoute}>Tilbake til arrangement</BlockLink>
      </Page>
    </>
  );
};
