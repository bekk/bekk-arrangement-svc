import classNames from 'classnames';
import { isNumber } from 'lodash';
import React, { useEffect } from 'react';
import { Link } from 'react-router-dom';
import {
  hasChristmasSpirit,
  hasHalloweenSpirit,
  hasKittens,
} from 'src/components/Common/FunEffects/effectUtils';
import { ExternalIcon } from 'src/components/Common/Icons/ExternalIcon';
import { LocationIcon } from 'src/components/Common/Icons/LocationIcon';
import {
  ParticipationState,
  EventState,
} from 'src/components/ViewEventsCards/ParticipationState';
import { useNumberOfParticipants, useWaitinglistSpot } from 'src/hooks/cache';
import { useEditToken, useSavedParticipations } from 'src/hooks/saved-tokens';
import { hasLoaded } from 'src/remote-data';
import { viewEventRoute, viewEventShortnameRoute } from 'src/routing';
import {
  hav,
  hvit,
  kveld,
  regn,
  skyfritt,
  sol,
  solnedgang,
  soloppgang,
} from 'src/style/colors';
import { isSameDate, stringifyDate } from 'src/types/date';
import { isInThePast } from 'src/types/date-time';
import {
  IEvent,
  isMaxParticipantsLimited,
  maxParticipantsLimit,
} from 'src/types/event';
import { stringifyTime } from 'src/types/time';
import style from './EventCardElement.module.scss';

interface IProps {
  eventId: string;
  event: IEvent;
}

export const EventCardElement = ({ eventId, event }: IProps) => {
  const editToken = useEditToken(eventId);

  const { savedParticipations: participationsInLocalStorage } =
    useSavedParticipations();
  const participationsForThisEvent = participationsInLocalStorage.filter(
    (p) => p.eventId === eventId
  );

  const waitingListSpot = useWaitinglistSpot(
    eventId,
    participationsForThisEvent[0]?.email
  );

  const remoteNumberOfParticipants = useNumberOfParticipants(eventId);
  const numberOfParticipants = hasLoaded(remoteNumberOfParticipants)
    ? remoteNumberOfParticipants.data
    : undefined;

  const numberOfAvailableSpot =
    isMaxParticipantsLimited(event.maxParticipants) &&
    numberOfParticipants !== undefined
      ? maxParticipantsLimit(event.maxParticipants) - numberOfParticipants
      : undefined;

  const registrationState = !isMaxParticipantsLimited(event.maxParticipants)
    ? EventState.Plass
    : numberOfParticipants === undefined
    ? EventState.Laster
    : numberOfParticipants < maxParticipantsLimit(event.maxParticipants)
    ? EventState.Plass
    : numberOfParticipants >= maxParticipantsLimit(event.maxParticipants) &&
      event.hasWaitingList
    ? EventState.PlassPaVenteliste
    : EventState.Fullt;

  const dateTimeText = isSameDate(event.start.date, event.end.date)
    ? `${stringifyDate(event.start.date)}, ${stringifyTime(
        event.start.time
      )} - ${stringifyTime(event.end.time)}`
    : `${stringifyDate(event.start.date)} \n - ${stringifyDate(
        event.end.date
      )}`;

  const viewRoute =
    event.shortname !== undefined
      ? viewEventShortnameRoute(event.shortname)
      : viewEventRoute(eventId);

  const titleStyle = classNames(style.title, {
    [style.longTitle]: event.title.length >= 20,
    [style.shortTitle]: event.title.length < 20,
  });

  const eventState = getEventState({
    event,
    waitingListSpot: hasLoaded(waitingListSpot)
      ? waitingListSpot.data
      : EventState.Laster,
    registrationState,
    editToken,
  });

  const cardStyle = classNames(
    style.card,
    useEventColor(eventId, style, event.title, event.customHexColor).style,
    {
      [style.cardActive]:
        eventState !== EventState.Avsluttet && eventState !== EventState.Avlyst,
      [style.cardFaded]:
        eventState === EventState.Avsluttet ||
        eventState === EventState.Avlyst ||
        event.isHidden,
    }
  );

  return (
    <Link className={cardStyle} to={viewRoute}>
      <div className={style.smallFont}>{dateTimeText}</div>
      <div className={titleStyle}>{event.title}</div>
      <div className={style.location}>
        <div className={style.locationIcon}>
          <LocationIcon color="white" />
        </div>
        <div className={style.iconText}>{event.location}</div>
      </div>
      {event.isHidden && (
        <p className={style.hidden}>
          Psst! Arrangementet er skjult for andre{' '}
          <span role="img" aria-label="hysj">
            🤫
          </span>
        </p>
      )}
      {event.isExternal && (
        <div className={style.external}>
          <div className={style.externalIcon}>
            <ExternalIcon color="white" />
          </div>
          <div className={style.iconText}> Eksternt arrangement </div>
        </div>
      )}
      <div className={style.cardFooter}>
        <ParticipationState
          eventId={eventId}
          eventState={eventState}
          event={event}
          numberOfAvailableSpots={numberOfAvailableSpot}
          waitingListSpot={
            hasLoaded(waitingListSpot) && isNumber(waitingListSpot.data)
              ? waitingListSpot.data
              : undefined
          }
        />
      </div>
    </Link>
  );
};

const colors = (style: any) =>
  new Map([
    ['Hav', { style: style.hav, colorCode: hav }],
    ['Kveld', { style: style.kveld, colorCode: kveld }],
    ['Regn', { style: style.regn, colorCode: regn }],
    ['Skyfritt', { style: style.skyfritt, colorCode: skyfritt }],
    ['Sol', { style: style.sol, colorCode: sol }],
    ['Solnedgang', { style: style.solnedgang, colorCode: solnedgang }],
    ['Soloppgang', { style: style.soloppgang, colorCode: soloppgang }],
  ]);

const getEventHash = (eventId: string): number =>
  [...eventId].map((char) => char.charCodeAt(0)).reduce((a, x) => a + x, 0);

export const useEventColor = (
  eventId: string | undefined,
  style: any,
  eventTitle: string,
  customHexColor?: string
): { style: string; colorCode: string } => {
  const className = `event-${eventId}-hover-color`;
  useEffect(() => {
    const style = document.createElement('style');
    style.type = 'text/css';
    style.innerHTML = `.${className}:hover { background-color: #${customHexColor}; }`;
    const head = (document.getElementsByTagName('head') as any)[0];
    if (head) head.appendChild(style);
  }, [className, customHexColor]);
  if (customHexColor) {
    return { style: className, colorCode: `#${customHexColor}` };
  }
  const defaultStyle = { style: style.none, colorCode: hvit };
  if (eventId === undefined) {
    return defaultStyle;
  }
  if (eventId === 'all-events') {
    return { style: style.soloppgang, colorCode: soloppgang };
  }
  if (hasChristmasSpirit(eventTitle)) {
    return { style: style.christmas, colorCode: '#D6001C' };
  }
  if (hasHalloweenSpirit(eventTitle)) {
    return { style: style.halloween, colorCode: '#FF7518' };
  }
  if (hasKittens(eventTitle)) {
    return { style: style.kittens, colorCode: '#f59fce' };
  }
  return (
    colors(style).get(
      [...colors(style).keys()][getEventHash(eventId) % colors(style).size]
    ) ?? defaultStyle
  );
};

interface EventStateProps {
  event: IEvent;
  editToken?: string;
  waitingListSpot: number | EventState.IkkePameldt | EventState.Laster;
  registrationState:
    | EventState.Plass
    | EventState.PlassPaVenteliste
    | EventState.Laster
    | EventState.Fullt;
}

const getEventState = ({
  event,
  editToken,
  waitingListSpot,
  registrationState,
}: EventStateProps): EventState => {
  if (event.isCancelled) return EventState.Avlyst;

  // if (editToken) return 'Rediger';

  if (isInThePast(event.end)) return EventState.Avsluttet;

  if (isNumber(waitingListSpot)) {
    if (waitingListSpot > 0) return EventState.PaVenteliste;
    else return EventState.Pameldt;
  }

  if (event.closeRegistrationTime && event.closeRegistrationTime <= new Date())
    return EventState.PameldingHarStengt;

  if (event.openForRegistrationTime >= new Date()) return EventState.IkkeApnet;

  return registrationState;
};
