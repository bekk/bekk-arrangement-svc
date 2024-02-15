import { validate, IError, assertIsValid, listOfErrors } from './validation';
import {
  Email,
  parseEmailViewModel,
  toEditEmail,
  parseEditEmail,
} from './email';
import {
  cancelParticipationUrlTemplate,
  createViewUrlTemplate,
} from '../routing';
import { IEvent, IQuestion } from './event';

export interface IQuestionAndAnswerWriteModel {
  questionId: number;
  eventId: string;
  email: string;
  answer: string;
}

export interface IQuestionAndAnswerViewModel {
  questionId: number;
  eventId: string;
  email: string;
  question: string;
  answer: string;
  required: boolean;
}

export interface IParticipantWriteModel {
  name: string;
  department: string;
  participantAnswers: IQuestionAndAnswerWriteModel[];
  viewUrlTemplate: string;
  cancelUrlTemplate: string;
}

export interface IParticipantViewModel {
  name: string;
  email?: string;
  department: string;
  eventId: string;
  registrationTime: number;
  questionAndAnswers: IQuestionAndAnswerViewModel[];
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
  department: string;
  participantAnswers: IQuestionAndAnswerViewModel[];
}

export interface IEditParticipant {
  name: string;
  email: string;
  department: string;
  participantAnswers: IQuestionAndAnswerViewModel[];
}

export const toParticipantWriteModel = (
  participant: IParticipant,
  event: IEvent
): IParticipantWriteModel => {
  return {
    name: participant.name,
    department: participant.department,
    participantAnswers: participant.participantAnswers
      .filter((a) => a.questionId !== 0)
      .map((a) => ({
        questionId: a.questionId,
        eventId: a.eventId,
        email: a.email,
        answer: a.answer.trim(),
      })),
    viewUrlTemplate: createViewUrlTemplate(event),
    cancelUrlTemplate: cancelParticipationUrlTemplate,
  };
};

export const parseParticipantViewModel = (
  participantView: IParticipantViewModel
): IParticipant => {
  const email = participantView.email
    ? parseEmailViewModel(participantView.email)
    : { email: '' };
  const name = parseName(participantView.name);
  const answers = parseAnswers(
    participantView
      .questionAndAnswers
      .filter(qa => qa.answer.length > 0)
  );
  const participant = {
    ...participantView,
    email,
    name,
    participantAnswers: answers,
  };

  assertIsValid(participant);

  return participant;
};

export const validateParticipation = ({
  name,
  email,
  department,
  participantAnswers: answers,
}: IEditParticipant): IParticipant | IError[] => {
  const participant = {
    name: parseName(name),
    email: parseEditEmail(email),
    department: department,
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
  department,
  participantAnswers: answers,
}: IParticipant): IEditParticipant => ({
  name,
  email: toEditEmail(email),
  department,
  participantAnswers: answers,
});

export const parseName = (value: string): string | IError[] => {
  const validator = validate<string>({
    'Navn må ha minst tre tegn': value.length < 3,
    'Navn kan ha maks 60 tegn': value.length > 60,
  });
  return validator.resolve(value);
};

export const parseAnswerString = (value: string, required: boolean): string | IError[] => {
  const validator = validate<string>({
    'Svar kan ha maks 500 tegn': value.length > 500,
    'Spørsmålet er obligatorisk': required && value.trim().length === 0,
  });
  return validator.resolve(value);
};

export const parseAnswers = (
  value: IQuestionAndAnswerViewModel[]
): IQuestionAndAnswerViewModel[] | IError[] => {
  const validator = validate<IQuestionAndAnswerViewModel[]>({
    'Svar kan ha maks 500 tegn': value.some((s) => s && s.answer.length > 500),
    'Du har ikke besvart alle obligatoriske spørsmål': value.some((s) => s.required && s.answer.trim().length === 0),
  });
  return validator.resolve(value);
};

export function initalParticipant(
  questions: IQuestion[],
  email?: string,
  name?: string,
  department?: string
): IParticipant {
  return {
    email: { email: email ?? '' },
    name: name ?? '',
    department: department ?? '',
    participantAnswers: questions.map(question => ({
      questionId: 0,
      eventId: '',
      email: '',
      question: '',
      answer: '',
      required: question.required,
    })),
  };
}
