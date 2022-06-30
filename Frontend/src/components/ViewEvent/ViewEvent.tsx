import React from 'react';
import { useHistory } from 'react-router';
import { userIsLoggedIn } from 'src/auth';
import { Button } from 'src/components/Common/Button/Button';
import { WavySubHeader } from 'src/components/Common/Header/WavySubHeader';
import { ClockIcon } from 'src/components/Common/Icons/ClockIcon';
import { ExternalIcon } from 'src/components/Common/Icons/ExternalIcon';
import { GentlemanIcon } from 'src/components/Common/Icons/GentlemanIcon';
import { LocationIcon } from 'src/components/Common/Icons/LocationIcon';
import { useGotoCreateDuplicate } from 'src/hooks/history';
import { createRoute, editEventRoute } from 'src/routing';
import { dateAsText, dateToIDate, isSameDate } from 'src/types/date';
import { IDateTime } from 'src/types/date-time';
import { stringifyEmail } from 'src/types/email';
import {
  IEvent,
  incrementOneWeek,
  isMaxParticipantsLimited,
  maxParticipantsLimit,
  toEditEvent,
  urlFromShortname,
} from 'src/types/event';
import { dateToITime, stringifyTime } from 'src/types/time';
import style from './ViewEvent.module.scss';
import { useSetTitle } from 'src/hooks/setTitle';

interface IProps {
  eventId?: string;
  event: IEvent;
  isPossibleToRegister?: boolean;
  participantsText: string;
  userCanEdit: boolean;
  isPreview?: true;
}

export const ViewEvent = ({
  eventId,
  event,
  isPossibleToRegister,
  userCanEdit,
  participantsText,
  isPreview,
}: IProps) => {
  const hasOpenedForRegistration = event.openForRegistrationTime < new Date();

  const history = useHistory();
  const gotoDuplicate = useGotoCreateDuplicate(createRoute);
  useSetTitle(`${event.title} | Skjer`);

  return (
    <section className={style.container}>
      {userCanEdit && eventId !== undefined && (
        <div className={style.editGroup}>
          <Button
            onClick={() => history.push(editEventRoute(eventId))}
            color={'Secondary'}
          >
            Rediger
          </Button>
          <Button
            displayAsLink
            onLightBackground
            onClick={() => gotoDuplicate(toEditEvent(incrementOneWeek(event)))}
            color={'Secondary'}
          >
            Dupliser
          </Button>
        </div>
      )}
      <WavySubHeader
        eventId={eventId}
        eventTitle={event.title}
        customHexColor={event.customHexColor}
      >
        <div className={style.headerContainer}>
          <h1 className={style.header}>{event.title}</h1>
        </div>
        <div className={style.generalInfoContainer}>
          <div className={style.iconTextContainer}>
            <ClockIcon color="black" className={style.clockIcon} />
            <DateSection startDate={event.start} endDate={event.end} />
          </div>
          <div className={style.iconTextContainer}>
            <LocationIcon color="black" className={style.icon} />
            <p>{event.location}</p>
          </div>
          <div className={style.iconTextContainer}>
            <GentlemanIcon color="black" className={style.icon} />
            {hasOpenedForRegistration ? (
              <p>{participantsText}</p>
            ) : (
              <p>
                {!isMaxParticipantsLimited(event.maxParticipants)
                  ? ' ∞'
                  : maxParticipantsLimit(event.maxParticipants)}{' '}
                plasser
              </p>
            )}
          </div>
          {event.isExternal && userIsLoggedIn() && (
            <div className={style.iconTextContainer}>
              <ExternalIcon color="black" className={style.externalIcon} />
              <p>Eksternt arrangement</p>
            </div>
          )}
        </div>
      </WavySubHeader>
      <div className={style.contentContainer}>
        <p className={style.description}>
          <Description description={event.description} />
        </p>
        <p>—</p>
        <p className={style.organizerText}>
          Arrangementet holdes av {event.organizerName}. Har du spørsmål ta
          kontakt på {stringifyEmail(event.organizerEmail)}!
        </p>
        <p className={style.registrationDeadlineText}>
          {event.closeRegistrationTime &&
            isPossibleToRegister &&
            `Frist for å melde seg på er ${dateAsText(
              dateToIDate(event.closeRegistrationTime)
            )}, kl ${stringifyTime(dateToITime(event.closeRegistrationTime))}.`}
        </p>
        {isPreview && (
          <div className={style.preview}>
            {event.participantQuestions.length > 0 && (
              <div>
                <h3>Spørsmål til deltaker</h3>
                {event.participantQuestions.map((q) => (
                  <div>{q}</div>
                ))}
              </div>
            )}
            {event.shortname && (
              <div>
                <h3>Arrangementet kan nåes via:</h3>
                <div>{urlFromShortname(event.shortname)}</div>
              </div>
            )}
          </div>
        )}
      </div>
    </section>
  );
};

interface IDateProps {
  startDate: IDateTime;
  endDate: IDateTime;
}

const DateSection = ({ startDate, endDate }: IDateProps) => {
  if (isSameDate(startDate.date, endDate.date)) {
    return (
      <p className={style.text}>
        {dateAsText(startDate.date)}, {stringifyTime(startDate.time)} -{' '}
        {stringifyTime(endDate.time)}
      </p>
    );
  }
  return (
    <p className={style.text}>
      {dateAsText(startDate.date)}, {stringifyTime(startDate.time)} -{' '}
      {dateAsText(endDate.date)}, {stringifyTime(endDate.time)}
    </p>
  );
};

export const capitalize = (text: string) =>
  text.charAt(0).toUpperCase() + text.substring(1);

const Description = ({ description }: { description: string }) => {
  const paragraphs = description.split('\n');
  return (
    <>
      {paragraphs.map((paragraph) => (
        <div key={paragraph} className={style.paragraph}>
          {formatLinks(paragraph).map(formatHeadersAndStuff)}
        </div>
      ))}
    </>
  );
};

const formatLinks = (line: string): (string | JSX.Element)[] => {
  const linkRegex = /(https?:\/\/[^\s]+)/;
  return line.split(linkRegex).flatMap((link, i) => {
    if (linkRegex.test(link)) {
      return [
        <a href={link} key={`${link}:${i}`} className={style.link}>
          {link}
        </a>,
      ] as (string | JSX.Element)[];
    }
    // This regex matches **something** and __something__
    const boldRegex = /(\*\*.+?\*\*|__.+?__)/g;
    return link.split(boldRegex).flatMap((bold, j) => {
      if (boldRegex.test(bold)) {
        return [
          <span key={`${bold}:${i}:${j}`} className={style.bold}>
            {bold.substring(2, bold.length - 2)}
          </span>,
        ];
      }
      // This regex matches *something* and _something_
      const italicsRegex = /(\*.+?\*|_.+?_)/g;
      return bold.split(italicsRegex).map((italic, k) => {
        if (italicsRegex.test(italic)) {
          return (
            <span key={`${italic}:${i}:${j}:${k}`} className={style.italic}>
              {italic.substring(1, italic.length - 1)}
            </span>
          );
        }
        return italic;
      });
    });
  });
};

const formatHeadersAndStuff = (s: string | JSX.Element, i: number) => {
  if (i === 0 && typeof s === 'string') {
    if (s.charAt(0) === '-') {
      return <li>{s.slice(1).trim()}</li>;
    }
    if (s.charAt(0) === '#') {
      return <h3>{s.slice(1).trim()}</h3>;
    }
  }
  return s;
};
