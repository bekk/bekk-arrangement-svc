import React from 'react';
import style from './ViewParticipants.module.scss';
import { useParticipants } from 'src/hooks/cache';
import { hasLoaded, isBad } from 'src/remote-data';
import { IParticipant } from 'src/types/participant';

interface IProps {
  eventId: string;
  editToken?: string;
}

/**
 * This component is used to display only the attendees names to internal users (Bekkere).
 * It differs from the viewParticipant component because it does not show personal information, and is only intended to be used as a motivation for employees to sign up.
 */
export const ViewParticipantsLimited = ({ eventId, editToken }: IProps) => {
  const remoteParticipants = useParticipants(eventId, editToken);

  if (isBad(remoteParticipants)) {
    return <div>Det er noe galt med dataen</div>;
  }

  if (!hasLoaded(remoteParticipants)) {
    return <div>Laster...</div>;
  }

  const { attendees, waitingList } = remoteParticipants.data;

  return (
    <div>
      {attendees.length > 0 ? (
        <div>
          <ParticipantTableLimited participants={attendees} />
        </div>
      ) : (
        <div>Ingen påmeldte</div>
      )}
      {waitingList && waitingList.length > 0 && (
        <>
          <h3 className={style.subSubHeader}>På venteliste</h3>
          <ParticipantTableLimited participants={waitingList} />
        </>
      )}
    </div>
  );
};

const ParticipantTableLimited = (props: { participants: IParticipant[] }) => {
  return (
    <table className={style.table}>
      <thead>
        <tr>
          <th className={style.desktopHeaderCell}>Navn</th>
        </tr>
      </thead>
      <tbody>
        {props.participants.map((attendee) => (
          <tr key={attendee.name}>
            <td className={style.desktopCell}>{attendee.name}</td>
          </tr>
        ))}
      </tbody>
    </table>
  );
};
