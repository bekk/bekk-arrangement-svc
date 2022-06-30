import { useHistory } from 'react-router';
import { IEditEvent, IEvent } from 'src/types/event';
import { Optional } from 'src/types';

//** Preview **//

export const useGotoEventPreview = (path: string) => {
  const history = useHistory();
  return (event: IEvent) => history.push(path, event);
};

export const usePreviewEvent = () => {
  const history = useHistory();
  return history.location.state as Optional<IEvent>;
};

//** Duplicate event **//

export const useGotoCreateDuplicate = (path: string) => {
  const history = useHistory();
  return (event: IEditEvent) => history.push(path, event);
};

export const useDuplicateEvent = () => {
  const history = useHistory();
  return history.location.state as Optional<IEditEvent>;
};
