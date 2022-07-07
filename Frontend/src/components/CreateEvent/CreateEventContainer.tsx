import React, {useState} from 'react';
import { IEditEvent, initialEditEvent, parseEditEvent } from 'src/types/event';
import { eventsRoute, previewNewEventRoute } from 'src/routing';
import { useAuthentication } from 'src/auth';
import { Page } from 'src/components/Page/Page';
import { Button } from 'src/components/Common/Button/Button';
import { EditEvent } from 'src/components/EditEvent/EditEvent/EditEvent';
import { BlockLink } from 'src/components/Common/BlockLink/BlockLink';
import style from './CreateEventContainer.module.scss';
import { isValid } from 'src/types/validation';
import { useDuplicateEvent, useGotoEventPreview } from 'src/hooks/history';
import { useEmailAndName } from 'src/hooks/cache';
import { hasLoaded } from 'src/remote-data';
import {useSetTitle} from "src/hooks/setTitle";
import {appTitle} from "src/Constants";

export const CreateEventContainer = () => {
  useAuthentication();
  useSetTitle(appTitle)

  const duplicateEvent = useDuplicateEvent();

  const emailAndName = useEmailAndName();
  const { email, name } = (hasLoaded(emailAndName) && emailAndName.data) || {};
  const storedEvent = window.sessionStorage.getItem("eventData")
  const eventData =
    storedEvent !== null ?
      JSON.parse(storedEvent) as IEditEvent:
      duplicateEvent ?? initialEditEvent(email, name)

  const [event, setEvent] = useState<IEditEvent>(eventData);
  const updateEvent = (event: IEditEvent) => {
    window.sessionStorage.setItem("eventData", JSON.stringify(event))
    setEvent(event)
  }
  const validEvent = validateEvent(event);
  const errors = parseEditEvent(event);

  const gotoPreview = useGotoEventPreview(previewNewEventRoute);

  const redirectToPreview =
    validEvent &&
    (() => {
      gotoPreview(validEvent);
    });

  return (
    <Page>
      <h1 className={style.header}>Opprett arrangement</h1>
      <EditEvent eventResult={event} updateEvent={updateEvent} />
      <div className={style.buttonContainer}>
        <Button
          onClick={redirectToPreview || (() => {})}
          disabled={!redirectToPreview}
          disabledResaon={
            <ul>
              {Array.isArray(errors) &&
                errors.map((x) => <li key={x.message}>{x.message}</li>)}
            </ul>
          }
        >
          Forhåndsvisning
        </Button>
        <BlockLink to={eventsRoute} onLightBackground>
          Avbryt
        </BlockLink>
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