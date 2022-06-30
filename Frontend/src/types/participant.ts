import { validate, IError, assertIsValid, listOfErrors } from './validation';
import {
  Email,
  parseEmailViewModel,
  toEditEmail,
  parseEditEmail,
} from './email';

export interface IParticipantWriteModel {
  name: string;
  participantAnswers: string[];
  cancelUrlTemplate: string;
}

export interface IParticipantViewModel {
  name: string;
  email?: string;
  eventId: string;
  registrationTime: number;
  participantAnswers: string[];
}

export interface IParticipantsWithWaitingList {
  attendees: IParticipant[];
  waitingList?: IParticipant[];
}

export interface IParticipantViewModelsWithWaitingList {
  attendees: IParticipantViewModel[];
  waitingList?: IParticipantViewModel[];
}

export interface INewParticipantViewModel {
  participant: IParticipantViewModel;
  cancellationToken: string;
}

export interface IParticipant {
  name: string;
  email: Email;
  participantAnswers: string[];
}

export interface IEditParticipant {
  name: string;
  email: string;
  participantAnswers: string[];
}

export const toParticipantWriteModel = (
  participant: IParticipant,
  cancelUrlTemplate: string = ''
): IParticipantWriteModel => {
  return {
    ...participant,
    cancelUrlTemplate,
  };
};

export const parseParticipantViewModel = (
  participantView: IParticipantViewModel
): IParticipant => {
  const email = participantView.email
    ? parseEmailViewModel(participantView.email)
    : { email: '' };
  const name = parseName(participantView.name);
  const answers = parseAnswers(participantView.participantAnswers);

  const participant = {
    ...participantView,
    email,
    name,
    answers,
  };

  assertIsValid(participant);

  return participant;
};

export const parseEditParticipant = ({
  name,
  email,
  participantAnswers: answers,
}: IEditParticipant): IParticipant | IError[] => {
  const participant = {
    name: parseName(name),
    email: parseEditEmail(email),
    participantAnswers: parseAnswers(answers),
  };

  try {
    assertIsValid(participant);
  } catch {
    return listOfErrors(participant);
  }

  return participant;
};

export const toEditParticipant = ({
  name,
  email,
  participantAnswers: answers,
}: IParticipant): IEditParticipant => ({
  name,
  email: toEditEmail(email),
  participantAnswers: answers,
});

export const parseName = (value: string): string | IError[] => {
  const validator = validate<string>({
    'Navn må ha minst tre tegn': value.length < 3,
    'Navn kan ha maks 60 tegn': value.length > 60,
  });
  return validator.resolve(value);
};

export const parseAnswers = (value: string[]): string[] | IError[] => {
  if (value.length === 0) {
    return value;
  }
  const validator = validate<string[]>({
    'Svar kan ha maks 500 tegn': value.some((s) => s.length > 500),
  });
  return validator.resolve(value);
};

export function initalParticipant(
  numberOfParticipantQuestions: number,
  email?: string,
  name?: string
): IParticipant {
  return {
    email: { email: email ?? '' },
    name: name ?? '',
    participantAnswers: Array(numberOfParticipantQuestions).fill(''),
  };
}
