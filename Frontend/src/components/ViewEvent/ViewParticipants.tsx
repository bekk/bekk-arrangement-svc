import React, { useState } from 'react';
import style from './ViewParticipants.module.scss';
import { stringifyEmail } from 'src/types/email';
import { useEvent, useParticipants } from 'src/hooks/cache';
import { hasLoaded, isBad } from 'src/remote-data';
import { useMediaQuery } from 'react-responsive';
import { IParticipant } from 'src/types/participant';
import { Button } from 'src/components/Common/Button/Button';
import { Spinner } from 'src/components/Common/Spinner/spinner';
import { Modal } from '../Common/Modal/Modal';
import { useEditToken, useSavedParticipations } from '../../hooks/saved-tokens';
import { useParam } from '../../utils/browser-state';
import { eventIdKey } from '../../routing';
import { deleteParticipant } from '../../api/arrangementSvc';
import { useNotification } from '../NotificationHandler/NotificationHandler';

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
    return <Spinner />;
  }

  return (
    <div>
      {remoteParticipants.data.attendees.length > 0 ? (
        screenIsMobileSize ? (
          <ParticipantTableMobile
            eventId={eventId}
            participants={remoteParticipants.data.attendees}
          />
        ) : (
          <ParticipantTableDesktop
            eventId={eventId}
            participants={remoteParticipants.data.attendees}
          />
        )
      ) : (
        <div>Ingen påmeldte</div>
      )}
      {remoteParticipants.data.waitingList &&
        remoteParticipants.data.waitingList.length > 0 && (
          <>
            <h3 className={style.subSubHeader}>På venteliste</h3>
            {screenIsMobileSize ? (
              <ParticipantTableMobile
                eventId={eventId}
                participants={remoteParticipants.data.waitingList}
              />
            ) : (
              <ParticipantTableDesktop
                eventId={eventId}
                participants={remoteParticipants.data.waitingList}
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
  const [showModal, setShowModal] = useState<IParticipant | null>(null);
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
      props.participants.map((p) => stringifyEmail(p.email)).join('; ')
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
      </Button>
      <Button onClick={copyEmails}>Kopier eposter til utklippstavle</Button>
      {wasCopied && 'Kopiert!'}
      <table className={style.table}>
        <thead>
          <tr>
            <th className={style.desktopHeaderCell}>Navn</th>
            <th className={style.desktopHeaderCell}>E-post</th>
            {hasComments && (
              <th className={style.desktopHeaderCell}>Kommentar</th>
            )}
            <th className={style.desktopHeaderCell}></th>
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
                        <div key={`${q}:${i}`}>
                          <div className={style.question}>{q}</div>
                          <div className={style.answer}>
                            {attendee.participantAnswers[i]}
                          </div>
                        </div>
                      )
                  )}
                </td>
              )}
              <td className={style.desktopCell}>
                <Button onClick={() => setShowModal(attendee)}>Meld av</Button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
      {showModal !== null && (
        <AvmeldModal
          eventName={hasLoaded(event) ? event.data.title : 'Arrangement'}
          participant={showModal}
          closeModal={() => setShowModal(null)}
        />
      )}
    </>
  );
};

const AvmeldModal = ({
  eventName,
  participant,
  closeModal,
}: {
  eventName: string;
  participant: IParticipant;
  closeModal: () => void;
}) => {
  const eventId = useParam(eventIdKey);
  const editToken = useEditToken(eventId);
  const [isDeleting, setIsDeleting] = useState(false);
  const { catchAndNotify } = useNotification();
  const { removeSavedParticipant } = useSavedParticipations();

  const cancelParticipant = catchAndNotify(async (participantEmail: string) => {
    await deleteParticipant({
      eventId,
      participantEmail,
      editToken,
    });
    removeSavedParticipant({ eventId, email: participantEmail });
  });

  return (
    <Modal header="Avmelding" closeModal={closeModal}>
      <p>
        Er du sikker på at du vil melde av {participant.name} fra {eventName}?
      </p>
      <p>Dersom vedkommende angrer seg må vedkommende melde seg på igjen.</p>
      <div style={{ marginTop: 50, display: 'flex', justifyContent: 'center' }}>
        <Button
          disabled={isDeleting}
          onClick={async () => {
            setIsDeleting(true);
            await cancelParticipant(participant.email.email);
            closeModal();
          }}>
          Meld av
        </Button>
        <Button disabled={isDeleting} onClick={closeModal}>
          Avbryt
        </Button>
      </div>
    </Modal>
  );
};
