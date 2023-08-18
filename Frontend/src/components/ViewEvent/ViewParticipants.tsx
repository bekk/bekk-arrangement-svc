import React, { useState } from 'react';
import style from './ViewParticipants.module.scss';
import { stringifyEmail } from 'src/types/email';
import { useEvent, useParticipants } from 'src/hooks/cache';
import { hasLoaded, isBad } from 'src/remote-data';
import { IParticipant } from 'src/types/participant';
import { Button } from 'src/components/Common/Button/Button';
import { Spinner } from 'src/components/Common/Spinner/spinner';
import { Modal } from '../Common/Modal/Modal';
import { useEditToken, useSavedParticipations } from 'src/hooks/saved-tokens';
import { deleteParticipant } from 'src/api/arrangementSvc';
import { useNotification } from '../NotificationHandler/NotificationHandler';
import { useHistory } from 'react-router';
import { Plus } from '../Common/Icons/Plus';

interface IProps {
  eventId: string;
  editToken?: string;
}

export const ViewParticipants = ({ eventId, editToken }: IProps) => {
  const remoteParticipants = useParticipants(eventId, editToken);

  if (isBad(remoteParticipants)) {
    return <div>Det er noe galt med dataen</div>;
  }

  if (!hasLoaded(remoteParticipants)) {
    return <Spinner />;
  }

  const { attendees, waitingList } = remoteParticipants.data;

  return (
    <div>
      {attendees.length > 0 ? (
        <>
          <ParticipantTableMobile eventId={eventId} participants={attendees} />
          <ParticipantTableDesktop eventId={eventId} participants={attendees} />
        </>
      ) : (
        <div>Ingen påmeldte</div>
      )}
      {waitingList && waitingList.length > 0 && (
        <>
          <h3 className={style.subSubHeader}>På venteliste</h3>
          <ParticipantTableMobile
            eventId={eventId}
            participants={waitingList}
          />
          <ParticipantTableDesktop
            eventId={eventId}
            participants={waitingList}
          />
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
  const [showModal, setShowModal] = useState<IParticipant | null>(null);
  if (!hasLoaded(event)) return <></>;
  return (
    <div className={style.hideOnDesktop}>
      <table className={style.table}>
        <tbody>
          {props.participants.map((attendee) => (
            <React.Fragment key={attendee.name + attendee.email.email}>
              <tr>
                <td className={style.mobileNameCell}>
                  {attendee.name}
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
                        <div key={q}>
                          <div className={style.question}>{q}</div>
                          <div className={style.answer}>
                            {attendee.participantAnswers[i]}
                          </div>
                        </div>
                      )
                  )}
                  <Button
                    className={style.deleteParticipantButtonMobile}
                    onClick={() => setShowModal(attendee)}>
                    Meld av
                  </Button>
                </td>
              </tr>
            </React.Fragment>
          ))}
        </tbody>
      </table>
      {showModal !== null && (
        <DeleteParticipantModal
          eventId={props.eventId}
          eventName={event.data.title}
          participant={showModal}
          closeModal={() => setShowModal(null)}
        />
      )}
    </div>
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

  if (!hasLoaded(event)) return <></>;

  return (
    <div className={style.hideOnMobile}>
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
                <button
                  className={style.deleteParticipantButton}
                  onClick={() => setShowModal(attendee)}>
                  <Plus title="Meld av" />
                </button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
      {showModal !== null && (
        <DeleteParticipantModal
          eventId={props.eventId}
          eventName={event.data.title}
          participant={showModal}
          closeModal={() => setShowModal(null)}
        />
      )}
    </div>
  );
};

const DeleteParticipantModal = ({
  eventId,
  eventName,
  participant,
  closeModal,
}: {
  eventId: string;
  eventName: string;
  participant: IParticipant;
  closeModal: () => void;
}) => {
  const history = useHistory();
  const editToken = useEditToken(eventId);
  const [isDeleting, setIsDeleting] = useState(false);
  const { catchAndNotify } = useNotification();
  const { removeSavedParticipant } = useSavedParticipations();

  const cancelParticipant = catchAndNotify(async (participantEmail: string) => {
    if (eventId && participantEmail) {
      await deleteParticipant({
        eventId,
        participantEmail,
        editToken,
      });
      removeSavedParticipant({ eventId, email: participantEmail });
    }
  });

  return (
    <Modal header={participant.name} closeModal={closeModal}>
      <div className={style.modalContent}>
        <p>Er du sikker på at du vil avmelde deltakeren fra {eventName}?</p>
        <p>Ved å avmelde deltakeren vil hen ikke lengre være påmeldt.</p>
      </div>
      <div className={style.deleteParticipantModalContainer}>
        <Button
          color={'Secondary'}
          className={style.modalButton}
          disabled={isDeleting}
          onClick={closeModal}>
          Avbryt
        </Button>
        <Button
          className={style.modalButton}
          disabled={isDeleting}
          onClick={async () => {
            setIsDeleting(true);
            await cancelParticipant(participant.email.email);
            history.go(0);
            closeModal();
          }}>
          Meld av
        </Button>
      </div>
    </Modal>
  );
};
