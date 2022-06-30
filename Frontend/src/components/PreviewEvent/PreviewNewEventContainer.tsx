import React from 'react';
import style from './PreviewEventContainer.module.scss';
import { Page } from 'src/components/Page/Page';
import { useNotification } from 'src/components/NotificationHandler/NotificationHandler';
import { useHistory } from 'react-router';
import {
  viewEventRoute,
  editEventRoute,
  viewEventShortnameRoute,
} from 'src/routing';
import { postEvent } from 'src/api/arrangementSvc';
import { ViewEvent } from 'src/components/ViewEvent/ViewEvent';
import { Button } from 'src/components/Common/Button/Button';
import { useSavedEditableEvents } from 'src/hooks/saved-tokens';
import { usePreviewEvent } from 'src/hooks/history';
import {
  isMaxParticipantsLimited,
  maxParticipantsLimit,
} from 'src/types/event';
import {useSetTitle} from "src/hooks/setTitle";

export const PreviewNewEventContainer = () => {
  const { catchAndNotify } = useNotification();
  const history = useHistory();

  const { saveEditableEvent } = useSavedEditableEvents();

  const event = usePreviewEvent();
  useSetTitle(`Forhåndsvisning ${event?.title}`)
  if (!event) {
    return <div>Det finnes ingen event å forhåndsvise</div>;
  }

  const returnToCreate = () => {
    history.goBack();
  };

  const participantsText = `${
    isMaxParticipantsLimited(event.maxParticipants)
      ? maxParticipantsLimit(event.maxParticipants)
      : 'ubegrensa'
  } plasser`;

  const postNewEvent = catchAndNotify(async () => {
    const editUrlTemplate =
      document.location.origin + editEventRoute('{eventId}', '{editToken}');
    const {
      event: { id, shortname },
      editToken,
    } = await postEvent(event, editUrlTemplate);
    saveEditableEvent({ eventId: id, editToken });
    history.push(
      shortname ? viewEventShortnameRoute(shortname) : viewEventRoute(id)
    );
  });

  return (
    <Page>
      <h1 className={style.header}>Forhåndsvisning</h1>
      <div className={style.previewContainer}>
        <ViewEvent
          eventId={undefined}
          event={event}
          participantsText={participantsText}
          userCanEdit={false}
          isPreview
        />
      </div>
      <div className={style.buttonContainer}>
        <Button color={'Secondary'} onClick={returnToCreate}>
          Tilbake til redigering
        </Button>
        <Button onClick={postNewEvent}>Opprett arrangement</Button>
      </div>
    </Page>
  );
};
