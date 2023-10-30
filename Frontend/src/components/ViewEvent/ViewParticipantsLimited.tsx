import React from 'react';
import style from './ViewParticipants.module.scss';
import { useParticipants } from 'src/hooks/cache';
import { hasLoaded, isBad, isUnauthorized } from 'src/remote-data';
import { IParticipant } from 'src/types/participant';
import { Spinner } from 'src/components/Common/Spinner/spinner';
import { getAuth0Url } from "src/auth";

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
    return isUnauthorized(remoteParticipants)
      ? <div className={style.badRemoteData}>Du må være autentisert for å se påmeldte deltakere. <a href={getAuth0Url()}>Trykk her</a> for å logge på.</div>
      : <div>Det har skjedd en feil i bakomliggende systemer. Ta kontakt med #basen på Slack.</div>;
  }

  if (!hasLoaded(remoteParticipants)) {
    return <Spinner />;
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
