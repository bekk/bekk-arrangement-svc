import React, { useState } from 'react';
import style from './ViewParticipants.module.scss';
import { stringifyEmail } from 'src/types/email';
import { useEvent, useParticipants } from 'src/hooks/cache';
import { hasLoaded, isBad } from 'src/remote-data';
import { useMediaQuery } from 'react-responsive';
import { IParticipant } from 'src/types/participant';
import { Button } from 'src/components/Common/Button/Button';

interface IProps {
  eventId: string;
  editToken?: string;
}

export const ViewParticipants = ({ eventId, editToken }: IProps) => {
  const remoteParticipants = useParticipants(eventId, editToken);
  const screenIsMobileSize = useMediaQuery({
    query: `(max-width: ${540}px)`,
  });

  if (isBad(remoteParticipants)) {
    return <div>Det er noe galt med dataen</div>;
  }

  if (!hasLoaded(remoteParticipants)) {
    return <div>Laster...</div>;
  }

  const { attendees, waitingList } = remoteParticipants.data;

  return (
    <div>
      {remoteParticipants.data.attendees.length > 0 ? (
        screenIsMobileSize ? (
          <ParticipantTableMobile eventId={eventId} participants={attendees} />
        ) : (
          <ParticipantTableDesktop eventId={eventId} participants={attendees} />
        )
      ) : (
        <div>Ingen påmeldte</div>
      )}
      {waitingList && waitingList.length > 0 && (
        <>
          <h3 className={style.subSubHeader}>På venteliste</h3>
          {screenIsMobileSize ? (
            <ParticipantTableMobile
              eventId={eventId}
              participants={waitingList}
            />
          ) : (
            <ParticipantTableDesktop
              eventId={eventId}
              participants={waitingList}
            />
          )}
        </>
      )}
    </div>
  );
};

const ParticipantTableMobile = (props: {
  eventId: string;
  participants: IParticipant[];
}) => {
  const event = useEvent(props.eventId);
  const questions = (hasLoaded(event) && event.data.participantQuestions) || [];
  return (
    <table className={style.table}>
      <tbody>
        {props.participants.map((attendee) => (
          <React.Fragment key={attendee.name + attendee.email.email}>
            <tr>
              <td className={style.mobileNameCell}>
                {attendee.name}{' '}
                <span className={style.mobileEmailCell}>
                  ({stringifyEmail(attendee.email)})
                </span>
              </td>
            </tr>
            <tr>
              <td colSpan={2} className={style.mobileCommentCell}>
                {questions.map(
                  (q, i) =>
                    attendee.participantAnswers[i] && (
                      <div>
                        <div className={style.question}>{q}</div>
                        <div className={style.answer}>
                          {attendee.participantAnswers[i]}
                        </div>
                      </div>
                    )
                )}
              </td>
            </tr>
          </React.Fragment>
        ))}
      </tbody>
    </table>
  );
};

const ParticipantTableDesktop = (props: {
  eventId: string;
  participants: IParticipant[];
}) => {
  const event = useEvent(props.eventId);
  const hasComments = hasLoaded(event)
    ? event.data.participantQuestions.length > 0
    : true;
  const questions = (hasLoaded(event) && event.data.participantQuestions) || [];
  const [wasCopied, setWasCopied] = useState(false);
  const copyAttendees = async () => {
    await navigator.clipboard.writeText(
      props.participants.map((p) => p.name).join(', ')
    );
    setWasCopied(true);
    setTimeout(() => {
      setWasCopied(false);
    }, 3000);
  };
  const copyEmails = async () => {
    await navigator.clipboard.writeText(
      props.participants.map((p) => stringifyEmail(p.email)).join(', ')
    );
    setWasCopied(true);
    setTimeout(() => {
      setWasCopied(false);
    }, 3000);
  };
  return (
    <>
      <Button onClick={copyAttendees}>
        Kopier deltakernavn til utklippstavle
      </Button>{' '}
      <Button onClick={copyEmails}>Kopier eposter til utklippstavle</Button>{' '}
      {wasCopied && 'Kopiert!'}
      <table className={style.table}>
        <thead>
          <tr>
            <th className={style.desktopHeaderCell}>Navn</th>
            <th className={style.desktopHeaderCell}>E-post</th>
            {hasComments && (
              <th className={style.desktopHeaderCell}>Kommentar</th>
            )}
          </tr>
        </thead>
        <tbody>
          {props.participants.map((attendee) => (
            <tr key={attendee.name + attendee.email.email}>
              <td className={style.desktopCell}>{attendee.name}</td>
              <td className={style.desktopCell}>
                {stringifyEmail(attendee.email)}
              </td>
              {hasComments && (
                <td className={style.desktopCell}>
                  {questions.map(
                    (q, i) =>
                      attendee.participantAnswers[i] && (
                        <div>
                          <div className={style.question}>{q}</div>
                          <div className={style.answer}>
                            {attendee.participantAnswers[i]}
                          </div>
                        </div>
                      )
                  )}
                </td>
              )}
            </tr>
          ))}
        </tbody>
      </table>
    </>
  );
};
