import React from 'react';
import { Button } from '../Common/Button/Button';
import { IEditEvent, parseEditEvent } from 'src/types/event';
import { isValid } from 'src/types/validation';
import { useGotoEventPreview } from 'src/hooks/history';

interface IProps {
  event: IEditEvent;
  path: string;
  children: string;
  className?: string;
}

export const PreviewEventButton = ({
  event,
  path,
  children,
  ...props
}: IProps) => {
  const parsedEvent = parseEditEvent(event);
  const eventIsValid = isValid(parsedEvent);
  const gotoPreview = useGotoEventPreview(path);

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
      disabledReason={errors}
      {...props}>
      {children}
    </Button>
  );
};
