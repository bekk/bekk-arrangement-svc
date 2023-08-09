import React from 'react';
import { Button } from '../Common/Button/Button';
import { IEditEvent, parseEditEvent } from 'src/types/event';
import { isValid } from 'src/types/validation';
import { useGotoEventPreview } from 'src/hooks/history';
import { previewNewEventRoute } from 'src/routing';

interface IProps {
  children: string;
  event: IEditEvent;
}

export const PreviewEventButton = ({ event, children }: IProps) => {
  const parsedEvent = parseEditEvent(event);
  const eventIsValid = isValid(parsedEvent);
  const gotoPreview = useGotoEventPreview(previewNewEventRoute);

  const redirectToPreview = () => {
    if (eventIsValid) gotoPreview(parsedEvent);
  };

  const errors = isValid(parsedEvent) ? undefined : (
    <ul>
      {parsedEvent.map((x) => (
        <li key={x.message}>{x.message}</li>
      ))}
    </ul>
  );

  return (
    <Button
      onClick={redirectToPreview}
      disabled={!eventIsValid}
      disabledReason={errors}>
      {children}
    </Button>
  );
};
