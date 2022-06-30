import React, { createRef, useRef } from 'react';
import { useHistory } from 'react-router';
import { getParticipantExportResponse } from 'src/api/arrangementSvc';
import {
  isAuthenticated,
  needsToAuthenticate,
  redirectToAuth0,
  userIsAdmin,
  userIsLoggedIn,
} from 'src/auth';
import { Button } from 'src/components/Common/Button/Button';
import { DownloadIcon } from 'src/components/Common/Icons/DownloadIcon/DownloadIcon';
import { Christmas } from 'src/components/Common/FunEffects/Christmas/Christmas';
import { Page } from 'src/components/Page/Page';
import { AddParticipant } from 'src/components/ViewEvent/AddParticipant';
import { ViewEvent } from 'src/components/ViewEvent/ViewEvent';
import { ViewParticipants } from 'src/components/ViewEvent/ViewParticipants';
import { ViewParticipantsLimited } from 'src/components/ViewEvent/ViewParticipantsLimited';
import {
  useEmailAndName,
  useEvent,
  useNumberOfParticipants,
} from 'src/hooks/cache';
import {
  Participation,
  useEditToken,
  useSavedParticipations,
} from 'src/hooks/saved-tokens';
import { useTimeLeft } from 'src/hooks/timeleftHooks';
import { hasLoaded, isBad } from 'src/remote-data';
import { cancelParticipantRoute } from 'src/routing';
import { dateAsText, dateToIDate } from 'src/types/date';
import { isInThePast } from 'src/types/date-time';
import {
  IEvent,
  isMaxParticipantsLimited,
  maxParticipantsLimit,
} from 'src/types/event';
import { dateToITime, stringifyTime } from 'src/types/time';
import { plural } from 'src/utils';
import { asString, ITimeLeft } from 'src/utils/timeleft';
import style from './ViewEventContainer.module.scss';
import {
  hasChristmasSpirit,
  hasHalloweenSpirit,
  hasKittens,
} from 'src/components/Common/FunEffects/effectUtils';
import { Halloween } from 'src/components/Common/FunEffects/Halloween/Halloween';
import { Kittens } from 'src/components/Common/FunEffects/Kittens/Kittens';

interface IProps {
  eventId: string;
}

export const ViewEventContainer = ({ eventId }: IProps) => {
  const history = useHistory();
  const remoteEvent = useEvent(eventId);

  const editTokenFound = useEditToken(eventId);

  const remoteNumberOfParticipants = useNumberOfParticipants(eventId);
  const numberOfParticipants = hasLoaded(remoteNumberOfParticipants)
    ? remoteNumberOfParticipants.data
    : Infinity;

  const { savedParticipations: participationsInLocalStorage } =
    useSavedParticipations();
  const participationsForThisEvent = participationsInLocalStorage.filter(
    (p) => p.eventId === eventId
  );

  const timeLeft = useTimeLeft(
    hasLoaded(remoteEvent) && remoteEvent.data.openForRegistrationTime
  );

  const hasCloseRegTime =
    (hasLoaded(remoteEvent) && remoteEvent.data.closeRegistrationTime) ?? false;
  const closeRegistrationTimeLeft = useTimeLeft(
    hasLoaded(remoteEvent) && remoteEvent.data.closeRegistrationTime
      ? remoteEvent.data.closeRegistrationTime
      : false
  );

  const oneMinute = 60000;
  const oneHour = oneMinute * 60;

  const emailAndName = useEmailAndName();

  // ref til boks hvor man ikke skal spawne juletrær og ræl fordi
  // det er annoying når man prøver å fylle ut påmeldingsskjemaet
  const noSpawnClick = useRef<HTMLDivElement>(null);

  if (isBad(remoteEvent)) {
    if (!isAuthenticated() && needsToAuthenticate(remoteEvent.statusCode)) {
      redirectToAuth0();
      return <p>Redirigerer til innlogging</p>;
    }
    return <div>{remoteEvent.userMessage}</div>;
  }

  if (!hasLoaded(remoteEvent) || !hasLoaded(emailAndName)) {
    return <div>Loading</div>;
  }

  const event = remoteEvent.data;
  const { email, name } = emailAndName.data ?? {};

  const eventIsFull =
    isMaxParticipantsLimited(event.maxParticipants) &&
    maxParticipantsLimit(event.maxParticipants) <= numberOfParticipants;

  const waitingList =
    hasLoaded(remoteNumberOfParticipants) &&
    eventIsFull &&
    isMaxParticipantsLimited(event.maxParticipants)
      ? remoteNumberOfParticipants.data -
        maxParticipantsLimit(event.maxParticipants)
      : '-';

  const shortParticipantsText = `${
    eventIsFull
      ? isMaxParticipantsLimited(event.maxParticipants) &&
        maxParticipantsLimit(event.maxParticipants)
      : numberOfParticipants
  }${
    !isMaxParticipantsLimited(event.maxParticipants)
      ? ' av ∞ påmeldte'
      : ' av ' + maxParticipantsLimit(event.maxParticipants) + ' påmeldte'
  }`;

  const avilableSpots = isMaxParticipantsLimited(event.maxParticipants)
    ? maxParticipantsLimit(event.maxParticipants) - numberOfParticipants
    : Infinity;

  const participantsText = `${
    eventIsFull
      ? isMaxParticipantsLimited(event.maxParticipants) &&
        plural(
          maxParticipantsLimit(event.maxParticipants),
          'påmeldt',
          'påmeldte'
        )
      : plural(numberOfParticipants, 'påmeldt', ' påmeldte')
  }${
    !isMaxParticipantsLimited(event.maxParticipants)
      ? '. Ubegrenset antall plasser igjen'
      : event.hasWaitingList && eventIsFull
      ? ` og ${waitingList} på venteliste`
      : `. ${plural(
          maxParticipantsLimit(event.maxParticipants) - numberOfParticipants,
          'plass',
          'plasser'
        )} igjen.`
  }`;

  const isClosingSoon =
    hasCloseRegTime &&
    closeRegistrationTimeLeft.difference < oneHour &&
    closeRegistrationTimeLeft.difference > 0;
  const closedEventText = getClosedEventText(
    event,
    timeLeft,
    hasCloseRegTime && closeRegistrationTimeLeft,
    isClosingSoon,
    eventIsFull
  );

  const waitlistText =
    eventIsFull && waitingList
      ? 'Arrangementet er dessverre fullt, men du kan fortsatt bli med på ventelisten!'
      : undefined;

  const numberOfPossibleParticipantsText = !isMaxParticipantsLimited(
    event.maxParticipants
  )
    ? 'Ubegrenset antall plasser'
    : avilableSpots > 0
    ? plural(avilableSpots, 'ledig plass', 'ledige plasser')
    : 'Ingen ledige plasser';

  const goToRemoveParticipantRoute = ({
    eventId,
    cancellationToken,
    email,
  }: Participation) => {
    history.push(
      cancelParticipantRoute({
        eventId,
        cancellationToken,
        email: encodeURIComponent(email),
      })
    );
  };

  const isPossibleToRegister = () => {
    const now = new Date();
    return (
      now > event.openForRegistrationTime &&
      (!event.closeRegistrationTime || now < event.closeRegistrationTime)
    );
  };

  return (
    <>
      {hasChristmasSpirit(event.title) && (
        <Christmas noSpawnClick={noSpawnClick} />
      )}
      {hasHalloweenSpirit(event.title) && <Halloween />}
      {hasKittens(event.title) && <Kittens />}
      <ViewEvent
        eventId={eventId}
        isPossibleToRegister={isPossibleToRegister()}
        event={event}
        participantsText={shortParticipantsText}
        userCanEdit={editTokenFound || userIsAdmin() ? true : false}
      />
      <Page>
        <section>
          {participationsForThisEvent.length >= 1 ? (
            <div>
              <h2 className={style.subHeader}>
                Du er påmeldt{' '}
                <span role="img" aria-label="konfetti">
                  🎉
                </span>
              </h2>
              <p className={style.content}>
                Hurra, du er påmeldt {event.title}! Vi gleder oss til å se deg.
                En bekreftelse er sendt på e-post til{' '}
                {participationsForThisEvent[0].email}
              </p>
              <h2 className={style.subHeader}>Kan du ikke likevel?</h2>
              <Button
                onClick={() =>
                  goToRemoveParticipantRoute(participationsForThisEvent[0])
                }
              >
                Meld meg av
              </Button>
            </div>
          ) : (
            <div ref={noSpawnClick} className={style.registrationContainer}>
              <div className={style.påmeldt}>
                <h2 className={style.subHeader}>Meld deg på</h2>
                <div className={style.numberOfParticipants}>
                  ({numberOfPossibleParticipantsText})
                </div>
              </div>
              {!isInThePast(event.end) &&
                timeLeft.difference < oneMinute &&
                !event.isCancelled &&
                !(eventIsFull && !event.hasWaitingList) &&
                (hasCloseRegTime
                  ? closeRegistrationTimeLeft.difference > 0
                  : true) && (
                  <AddParticipant
                    eventId={eventId}
                    event={event}
                    email={email}
                    name={name}
                  />
                )}
              <div className={style.boxHolder}>
                {closedEventText !== undefined ? (
                  <div>
                    <p className={style.marginKiller}>
                      Påmeldingen{' '}
                      {isClosingSoon ? 'stenger snart' : 'er stengt'} <br />
                      <div className={style.closedEventText}>
                        {closedEventText}
                      </div>
                    </p>
                  </div>
                ) : waitlistText ? (
                  <p>{waitlistText}</p>
                ) : null}
              </div>
              <div className={style.registrationDeadlineText}>
                {event.closeRegistrationTime &&
                  !isPossibleToRegister() &&
                  `Frist for å melde seg på er ${dateAsText(
                    dateToIDate(event.closeRegistrationTime)
                  )}, kl ${stringifyTime(
                    dateToITime(event.closeRegistrationTime)
                  )}.`}
              </div>
            </div>
          )}
          {editTokenFound || userIsAdmin() ? (
            <>
              <div className={style.attendeesTitleContainer}>
                <h2 className={style.subHeader}>Påmeldte</h2>
                {(editTokenFound || userIsAdmin()) && (
                  <DownloadExportLink eventId={eventId} />
                )}
              </div>
              <p>{participantsText}</p>
              <ViewParticipants eventId={eventId} editToken={editTokenFound} />
            </>
          ) : (
            userIsLoggedIn() &&
            !event.isExternal && (
              <>
                <div className={style.attendeesTitleContainer}>
                  <h2 className={style.subHeader}>Påmeldte</h2>
                  {(editTokenFound || userIsAdmin()) && (
                    <DownloadExportLink eventId={eventId} />
                  )}
                </div>
                <p>{participantsText}</p>
                <ViewParticipantsLimited
                  eventId={eventId}
                  editToken={editTokenFound}
                />
              </>
            )
          )}
          {!userIsLoggedIn() && event.isExternal && (
            <>
              <div className={style.attendeesTitleContainer}>
                <h2 className={style.subHeader}>Påmeldte</h2>
              </div>
              <p>{participantsText}</p>
            </>
          )}
        </section>
      </Page>
    </>
  );
};

interface IPropsDownloadExport {
  eventId: string;
}

export function DownloadExportLink({ eventId }: IPropsDownloadExport) {
  const link = createRef<HTMLAnchorElement>();

  const handleAction = async () => {
    if (link.current?.href) {
      return;
    }

    const result = await getParticipantExportResponse(eventId);
    const blob = await result.blob();

    const href = window.URL.createObjectURL(blob);

    if (link.current) {
      link.current.download = eventId + '.csv';
      link.current.href = href;
      link.current.click();
    }
  };

  return (
    // eslint-disable-next-line
    <a
      role="button"
      ref={link}
      onClick={handleAction}
      className={style.downloadIcon}
    >
      <DownloadIcon title="Last ned deltageroversikt" />
    </a>
  );
}

const getClosedEventText = (
  event: IEvent,
  timeLeft: ITimeLeft,
  closeRegistrationTimeLeft: ITimeLeft | false,
  isClosingSoon: boolean,
  eventIsFull: boolean
) => {
  if (isInThePast(event.end)) {
    return 'Arrangementet har allerede funnet sted';
  }

  if (timeLeft.difference > 0) {
    const openDate = dateAsText(dateToIDate(event.openForRegistrationTime));
    const openTime = stringifyTime(dateToITime(event.openForRegistrationTime));
    return `Påmeldingen åpner ${openDate}, kl ${openTime}.`;
  }
  if (eventIsFull && !event.hasWaitingList) {
    return 'Arrangementet er dessverre fullt';
  }

  if (event.isCancelled) {
    return 'Arrangementet er desverre avlyst';
  }

  if (closeRegistrationTimeLeft && closeRegistrationTimeLeft.difference <= 0) {
    return '';
  }

  if (
    isClosingSoon &&
    closeRegistrationTimeLeft &&
    event.closeRegistrationTime
  ) {
    const closeDate = dateAsText(dateToIDate(event.closeRegistrationTime));
    const closeTime = stringifyTime(dateToITime(event.closeRegistrationTime));
    return `Stenger ${closeDate}, kl ${closeTime}, om ${asString(
      closeRegistrationTimeLeft
    )}`;
  }

  return undefined;
};
