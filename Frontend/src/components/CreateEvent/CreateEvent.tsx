import React from 'react';
import { IEditEvent, initialEditEvent } from 'src/types/event';
import { eventsRoute } from 'src/routing';
import { useAuthentication } from 'src/auth';
import { Page } from 'src/components/Page/Page';
import { EventForm } from '../EventForm/EventForm';
import { BlockLink } from 'src/components/Common/BlockLink/BlockLink';
import style from './CreateEvent.module.scss';
import { useDuplicateEvent } from 'src/hooks/history';
import { useEmailNameAndDepartment } from 'src/hooks/cache';
import { hasLoaded } from 'src/remote-data';
import { useSetTitle } from 'src/hooks/setTitle';
import { appTitle } from 'src/Constants';
import { useSessionState } from 'src/hooks/sessionState';
import { Link } from 'react-router-dom';
import { PreviewEventButton } from '../EventForm/PreviewEventButton';

export const CreateEvent = () => {
  useAuthentication();
  useSetTitle(appTitle);

  const duplicateEvent = useDuplicateEvent();

  const emailAndName = useEmailNameAndDepartment();
  const { email, name } = (hasLoaded(emailAndName) && emailAndName.data) || {};

  const [event, setEvent] = useSessionState<IEditEvent>(
    duplicateEvent ?? initialEditEvent(email, name),
    'createEvent'
  );

  return (
    <Page>
      <p className={style.linkContainer}>
        ← <Link to={eventsRoute}>Tilbake til oversikten</Link>
      </p>
      <h1 className={style.header}>Opprett arrangement</h1>
      <EventForm eventResult={event} updateEvent={setEvent} />
      <div className={style.buttonContainer}>
        <PreviewEventButton event={event}>Forhåndsvisning</PreviewEventButton>
        <BlockLink to={eventsRoute} onLightBackground>
          Avbryt
        </BlockLink>
      </div>
    </Page>
  );
};
