import { useState } from 'react';
import React from 'react';
import {
  IEditEvent,
  initialEditEvent,
  parseEvent,
  IEvent,
} from 'src/types/event';
import { postEvent } from 'src/api/arrangementSvc';
import { isOk, Result } from 'src/types/validation';
import { useHistory } from 'react-router';
import { getViewEventRoute, eventsRoute } from 'src/routing';
import { EditEvent } from '../EditEvent/EditEvent/EditEvent';
import { Button } from '../Common/Button/Button';
import { PreviewEvent } from '../PreviewEvent/PreviewEvent';
import { useAuthentication } from 'src/auth';
import { Page } from '../Page/Page';
import style from './CreateEventContainer.module.scss';

export const CreateEventContainer = () => {
  useAuthentication();

  const [event, setEvent] = useState<Result<IEditEvent, IEvent>>(
    parseEvent(initialEditEvent)
  );
  const [previewState, setPreviewState] = useState(false);
  const history = useHistory();

  const addEvent = async () => {
    if (isOk(event)) {
      const createdEvent = await postEvent(event.validValue);
      history.push(getViewEventRoute(createdEvent.id));
    } else {
      throw Error('her kommer feil');
    }
  };

  const goToOverview = () => history.push(eventsRoute);

  const updateEvent = (editEvent: IEditEvent) =>
    setEvent(parseEvent(editEvent));

  const PreviewView = ({ validEvent }: { validEvent: IEvent }) => (
    <>
      <PreviewEvent event={validEvent} />
      <div className={style.buttonContainer}>
        <Button onClick={addEvent}>Opprett event</Button>
        <Button onClick={() => setPreviewState(false)}>Tilbake</Button>
      </div>
    </>
  );

  const CreateView = ({ isInvalid }: { isInvalid: boolean }) => (
    <>
      <h1 className={style.header}>Opprett event</h1>
      <EditEvent eventResult={event.editValue} updateEvent={updateEvent} />
      <div className={style.buttonContainer}>
        <Button onClick={() => setPreviewState(true)} disabled={isInvalid}>
          Forhåndsvisning
        </Button>
        <Button onClick={goToOverview}>Avbryt</Button>
      </div>
    </>
  );

  return (
    <Page>
      {previewState && isOk(event) ? (
        <PreviewView validEvent={event.validValue} />
      ) : (
        <CreateView isInvalid={!isOk(event)} />
      )}
    </Page>
  );
};
