import { useLayoutEffect, useEffect } from 'react';
import React from 'react';
import { IEditEvent, toEditEvent } from 'src/types/event';
import { deleteEvent } from 'src/api/arrangementSvc';
import { useHistory } from 'react-router';
import style from './EditEvent.module.scss';
import { eventsRoute, editTokenKey, viewEventRoute } from 'src/routing';
import { hasLoaded } from 'src/remote-data';
import { useQuery, useParam } from 'src/utils/browser-state';
import { useNotification } from 'src/components/NotificationHandler/NotificationHandler';
import { Page } from 'src/components/Page/Page';
import { BlockLink } from 'src/components/Common/BlockLink/BlockLink';
import { eventIdKey } from 'src/routing';
import { ButtonWithPromptModal } from 'src/components/Common/ButtonWithConfirmModal/ButtonWithPromptModal';
import { useEvent } from 'src/hooks/cache';
import { useEditToken, useSavedEditableEvents } from 'src/hooks/saved-tokens';
import classnames from 'classnames';
import { useSetTitle } from 'src/hooks/setTitle';
import { Spinner } from 'src/components/Common/Spinner/spinner';
import { useSessionState } from 'src/hooks/sessionState';
import { EventForm } from '../EventForm/EventForm';
import { PreviewEventButton } from '../EventForm/PreviewEventButton';
import { BackLink } from '../EventForm/BackLink';

const useEditEvent = () => {
  const eventId = useParam(eventIdKey);
  const remoteEvent = useEvent(eventId);

  const [editEvent, setEditEvent] = useSessionState<IEditEvent | undefined>(
    undefined,
    eventId
  );
  useLayoutEffect(() => {
    if (hasLoaded(remoteEvent) && !editEvent) {
      setEditEvent(toEditEvent(remoteEvent.data));
    }
  }, [remoteEvent, editEvent, setEditEvent]);

  return { eventId, editEvent, setEditEvent };
};

const useSaveThisEditToken = ({ eventId }: { eventId: string }) => {
  const editToken = useQuery(editTokenKey);
  const { saveEditableEvent } = useSavedEditableEvents();
  useEffect(() => {
    if (editToken) {
      saveEditableEvent({ eventId, editToken });
    }
  }, [eventId, editToken, saveEditableEvent]);
};

export const EditEvent = () => {
  const { eventId, editEvent, setEditEvent } = useEditEvent();
  useSetTitle(`Rediger ${editEvent?.title}`);

  const { catchAndNotify } = useNotification();
  const history = useHistory();

  useSaveThisEditToken({ eventId });
  const editToken = useEditToken(eventId);

  if (!editEvent) {
    return <Spinner />;
  }

  const onDeleteEvent = catchAndNotify(
    async (messageToParticipants: string) => {
      await deleteEvent(eventId, messageToParticipants, editToken);
      history.push(eventsRoute);
    }
  );

  return (
    <Page>
      <BackLink to={viewEventRoute(eventId)}>
        Tilbake til arrangementet
      </BackLink>
      <h1 className={style.header}>Rediger arrangement</h1>
      <EventForm eventResult={editEvent} updateEvent={setEditEvent} />
      <div className={style.buttonContainer}>
        <BlockLink to={eventsRoute} onLightBackground>
          Avbryt
        </BlockLink>
        <div className={style.groupedButtons}>
          <ButtonWithPromptModal
            text={'Avlys arrangement'}
            onConfirm={onDeleteEvent}
            placeholder="Arrangementet er avlyst pga. ..."
            textareaLabel="Send en forklarende tekst på e-post til alle påmeldte deltakere:"
            className={classnames(style.button, style.redButton)}>
            <p>
              Er du sikker på at du vil avlyse arrangementet? <br />
              Alle deltakerene vil få beskjed. Dette kan ikke reverseres{' '}
              <span role="img" aria-label="grimacing-face">
                😬
              </span>
            </p>
            <p className={style.italic}>
              OBS: Når et arrangement blir avlyst vises det på forsiden i et
              døgn, <br />
              markert som avlyst.
            </p>
          </ButtonWithPromptModal>
          <PreviewEventButton event={editEvent} className={style.button}>
            Forhåndsvis endringer
          </PreviewEventButton>
        </div>
      </div>
    </Page>
  );
};
