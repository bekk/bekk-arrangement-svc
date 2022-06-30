import { useLayoutEffect, useEffect } from 'react';
import React from 'react';
import { IEditEvent, toEditEvent, parseEditEvent } from 'src/types/event';
import { deleteEvent } from 'src/api/arrangementSvc';
import { useHistory } from 'react-router';
import { EditEvent } from './EditEvent/EditEvent';
import style from './EditEventContainer.module.scss';
import { eventsRoute, editTokenKey, previewEventRoute } from 'src/routing';
import { hasLoaded } from 'src/remote-data';
import {
  useQuery,
  useParam,
  usePersistentHistoryState,
} from 'src/utils/browser-state';
import { useNotification } from 'src/components/NotificationHandler/NotificationHandler';
import { Page } from 'src/components/Page/Page';
import { Button } from 'src/components/Common/Button/Button';
import { BlockLink } from 'src/components/Common/BlockLink/BlockLink';
import { isValid } from 'src/types/validation';
import { eventIdKey } from 'src/routing';
import { ButtonWithPromptModal } from 'src/components/Common/ButtonWithConfirmModal/ButtonWithPromptModal';
import { useEvent } from 'src/hooks/cache';
import { useGotoEventPreview } from 'src/hooks/history';
import { useEditToken, useSavedEditableEvents } from 'src/hooks/saved-tokens';
import classnames from 'classnames';
import {useSetTitle} from "src/hooks/setTitle";

const useEditEvent = () => {
  const eventId = useParam(eventIdKey);
  const remoteEvent = useEvent(eventId);

  const [editEvent, setEditEvent] = usePersistentHistoryState<IEditEvent>();
  useLayoutEffect(() => {
    if (hasLoaded(remoteEvent) && !editEvent) {
      setEditEvent(toEditEvent(remoteEvent.data));
    }
  }, [remoteEvent, editEvent, setEditEvent]);

  const validEvent = validateEvent(editEvent);
  const errors = editEvent ? parseEditEvent(editEvent) : [];

  return { eventId, validEvent, editEvent, setEditEvent, errors };
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

export const EditEventContainer = () => {
  const { eventId, validEvent, editEvent, setEditEvent, errors } =
    useEditEvent();
  useSetTitle(`Rediger ${editEvent?.title}`)

  const { catchAndNotify } = useNotification();
  const history = useHistory();

  const gotoPreview = useGotoEventPreview(previewEventRoute(eventId));

  useSaveThisEditToken({ eventId });
  const editToken = useEditToken(eventId);

  if (!editEvent) {
    return <div>Loading</div>;
  }

  const onDeleteEvent = catchAndNotify(
    async (messageToParticipants: string) => {
      await deleteEvent(eventId, messageToParticipants, editToken);
      history.push(eventsRoute);
    }
  );

  return (
    <Page>
      <h1 className={style.header}>Rediger arrangement</h1>
      <EditEvent eventResult={editEvent} updateEvent={setEditEvent} />
      <div className={style.buttonContainer}>
        <BlockLink to={eventsRoute}>Avbryt</BlockLink>
        <div className={style.groupedButtons}>
          <ButtonWithPromptModal
            text={'Avlys arrangement'}
            onConfirm={onDeleteEvent}
            placeholder="Arrangementet er avlyst pga. ..."
            textareaLabel="Send en forklarende tekst på e-post til alle påmeldte deltakere:"
            className={classnames(style.button, style.redButton)}
          >
            <>
              <p>
                Er du sikker på at du vil avlyse arrangementet? <br />
                Alle deltakerene vil få beskjed. Dette kan ikke reverseres{' '}
                <span role="img" aria-label="grimacing-face">
                  😬
                </span>
              </p>
            </>
          </ButtonWithPromptModal>
          <Button
            onClick={() => validEvent && gotoPreview(validEvent)}
            className={style.button}
            disabled={!validEvent}
            disabledResaon={
              <ul>
                {Array.isArray(errors) &&
                  errors.map((x) => <li key={x.message}>{x.message}</li>)}
              </ul>
            }
          >
            Forhåndsvis endringer
          </Button>
        </div>
      </div>
    </Page>
  );
};

const validateEvent = (event?: IEditEvent) => {
  if (event) {
    const validEvent = parseEditEvent(event);
    if (isValid(validEvent)) {
      return validEvent;
    }
  }
};
