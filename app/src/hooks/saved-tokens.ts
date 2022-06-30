//**  Event  **//

import { useEffect, useState } from 'react';
import { getEventsAndParticipationsForEmployee } from 'src/api/arrangementSvc';
import { getEmployeeId, isAuthenticated } from 'src/auth';
import { useLocalStorage } from 'src/hooks/localStorage';

export type EditEventToken = {
  eventId: string;
  editToken: string;
};
const isEditEventTokenType = (x: any): x is EditEventToken =>
  'eventId' in x &&
  typeof x.eventId === 'string' &&
  'editToken' in x &&
  typeof x.editToken === 'string';

export const useSavedEditableEvents = (): {
  savedEvents: EditEventToken[];
  saveEditableEvent: (event: EditEventToken) => void;
} => {
  const [readStorage, setStorage] = useLocalStorage({
    localStorageKey: 'editable-events',
  });

  const readValidatedStorage = () => {
    const storageContent = readStorage();
    const parsedStorage: unknown[] =
      storageContent !== undefined ? JSON.parse(storageContent) : [];
    return Array.isArray(parsedStorage)
      ? parsedStorage.filter(isEditEventTokenType)
      : [];
  };

  const updateStorage = (event: EditEventToken) =>
    JSON.stringify([
      ...readValidatedStorage().filter((x) => x.eventId !== event.eventId),
      event,
    ]);

  return {
    savedEvents: readValidatedStorage(),
    saveEditableEvent: (event: EditEventToken) =>
      setStorage(updateStorage(event)),
  };
};

export const useEditToken = (eventId: string) => {
  const { savedEvents: createdEvents } = useSavedEditableEvents();
  const event = createdEvents.find((x) => x.eventId === eventId);

  return event?.editToken;
};

//**  Participant  **//

export type Participation = {
  eventId: string;
  email: string;
  cancellationToken: string;
};
const isParticipationAndHasCancellationToken = (x: any): x is Participation =>
  'eventId' in x &&
  typeof x.eventId === 'string' &&
  'email' in x &&
  typeof x.email === 'string' &&
  'cancellationToken' in x &&
  typeof x.cancellationToken === 'string';

export const useSavedParticipations = () => {
  const [readStorage, setStorage] = useLocalStorage({
    localStorageKey: 'participations',
  });

  const readValidatedStorage = () => {
    const storageContent = readStorage();
    const parsedStorage: unknown[] =
      storageContent !== undefined ? JSON.parse(storageContent) : [];
    return Array.isArray(parsedStorage)
      ? parsedStorage.filter(isParticipationAndHasCancellationToken)
      : [];
  };

  const updateStorage = (participant: Participation) =>
    JSON.stringify([
      ...readValidatedStorage().filter(
        (x) =>
          !(participant.eventId === x.eventId && participant.email === x.email)
      ),
      participant,
    ]);

  const removeFromStorage = (p: { eventId: string; email: string }) =>
    JSON.stringify(
      readValidatedStorage().filter(
        (x) => !(x.eventId === p.eventId && x.email === p.email)
      )
    );

  return {
    savedParticipations: readValidatedStorage(),
    saveParticipation: (participant: Participation) => {
      setStorage(updateStorage(participant));
    },
    removeSavedParticipant: (participant: { eventId: string; email: string }) =>
      setStorage(removeFromStorage(participant)),
  };
};

//** On page load **//

export const usePopulateTokensInLocalStorage = () => {
  const { savedEvents, saveEditableEvent } = useSavedEditableEvents();
  const { savedParticipations, saveParticipation, removeSavedParticipant } =
    useSavedParticipations();

  useEffectOnce(async () => {
    if (isAuthenticated()) {
      const employeeId = getEmployeeId();
      const { editableEvents, participations } =
        await getEventsAndParticipationsForEmployee(employeeId);

      const eventKey = ({ eventId }: EditEventToken) => eventId;
      const participationKey = ({ eventId, email }: Participation) =>
        `${eventId}:${email}`;

      const newEvents = editableEvents.filter(
        (x) => !savedEvents.map(eventKey).includes(eventKey(x))
      );
      const newParticipations = participations.filter(
        (x) =>
          !savedParticipations
            .map(participationKey)
            .includes(participationKey(x))
      );
      const removedParticipations = savedParticipations.filter(
        (x) =>
          !participations.map(participationKey).includes(participationKey(x))
      );

      newEvents.forEach(saveEditableEvent);
      newParticipations.forEach(saveParticipation);
      removedParticipations.forEach(removeSavedParticipant);
    }
  });
};

const useEffectOnce = (effect: () => void) => {
  const [hasRun, setHasRun] = useState(false);
  useEffect(() => {
    if (!hasRun) {
      effect();
      setHasRun(true);
    }
  }, [hasRun, effect]);
};
