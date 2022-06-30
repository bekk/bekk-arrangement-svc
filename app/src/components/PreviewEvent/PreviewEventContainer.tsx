import React from 'react';
import style from './PreviewEventContainer.module.scss';
import { Page } from 'src/components/Page/Page';
import { useNotification } from 'src/components/NotificationHandler/NotificationHandler';
import { useHistory } from 'react-router';
import { useParam } from 'src/utils/browser-state';
import {
  eventIdKey,
  viewEventRoute,
  viewEventShortnameRoute,
} from 'src/routing';
import { putEvent } from 'src/api/arrangementSvc';
import { ViewEvent } from 'src/components/ViewEvent/ViewEvent';
import { Button } from 'src/components/Common/Button/Button';
import { usePreviewEvent } from 'src/hooks/history';
import { useEditToken } from 'src/hooks/saved-tokens';
import {
  isMaxParticipantsLimited,
  maxParticipantsLimit,
} from 'src/types/event';
import {useSetTitle} from "src/hooks/setTitle";
import {appTitle} from "src/Constants";

export const PreviewEventContainer = () => {
  const { catchAndNotify } = useNotification();
  const history = useHistory();
  useSetTitle(appTitle)

  const eventId = useParam(eventIdKey);
  const editToken = useEditToken(eventId);
  const event = usePreviewEvent();
  useSetTitle(`Forhåndsvisning ${event?.title}`)
  if (!event) {
    return <div>Det finnes ingen event å forhåndsvise</div>;
  }

  const participantsText =
    (!isMaxParticipantsLimited(event.maxParticipants)
      ? '∞'
      : maxParticipantsLimit(event.maxParticipants).toString()) + ' plasser';

  const returnToEdit = () => {
    history.goBack();
  };

  const putEditedEvent = catchAndNotify(async () => {
    await putEvent(eventId, event, editToken);
    history.push(
      event.shortname
        ? viewEventShortnameRoute(event.shortname)
        : viewEventRoute(eventId)
    );
  });

  return (
    <Page>
      <h1 className={style.header}>Forhåndsvisning</h1>
      <div className={style.previewContainer}>
        <ViewEvent
          eventId={eventId}
          event={event}
          participantsText={participantsText}
          userCanEdit={false}
          isPreview
        />
      </div>
      <div className={style.buttonContainer}>
        <Button color={'Secondary'} onClick={returnToEdit}>
          Tilbake til redigering
        </Button>
        <Button onClick={putEditedEvent}>Lagre</Button>
      </div>
    </Page>
  );
};
