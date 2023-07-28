import { IError, assertIsValid, listOfErrors } from 'src/types/validation';
import {
  parseTitle,
  parseDescription,
  parseHost,
  parseMaxAttendees,
  parseLocation,
  WithId,
  parseQuestions,
  toEditMaxAttendees,
  parseShortname,
  parseProgram,
  parseOffices,
  parsePickedOffices,
} from '.';
import {
  IDateTime,
  EditDateTime,
  toEditDateTime,
  parseEditDateTime,
} from 'src/types/date-time';
import {
  Email,
  toEditEmail,
  parseEditEmail,
  toEmailWriteModel,
} from 'src/types/email';
import {
  TimeInstanceContract,
  TimeInstanceEdit,
  TimeInstance,
  parseTimeInstanceViewModel,
  toTimeInstanceWriteModel,
  toEditTimeInstance,
  parseEditTimeInstance,
  addWeekToTimeInstance,
} from 'src/types/time-instance';
import {
  parseDateViewModel,
  dateToIDate,
  toEditDate,
  addWeek,
} from 'src/types/date';
import { parseName } from 'src/types/participant';

import {
  cancelParticipationUrlTemplate,
  createViewUrlTemplate,
  editUrlTemplate,
  viewEventShortnameRoute,
} from 'src/routing';
import { toEditTime } from 'src/types/time';

export interface INewEventViewModel {
  event: WithId<IEventViewModel>;
  editToken: string;
}

export type Office = 'Oslo' | 'Trondheim';
export type PickedOffice = {
  Oslo: boolean;
  Trondheim: boolean;
};

export interface IEventViewModel {
  title: string;
  description: string;
  location: string;
  offices?: Office[];
  startDate: IDateTime;
  endDate: IDateTime;
  openForRegistrationTime: TimeInstanceContract;
  closeRegistrationTime?: TimeInstanceContract;
  organizerName: string;
  organizerEmail: string;
  maxParticipants?: number;
  participantQuestions: string[];
  program?: string;
  hasWaitingList: boolean;
  isCancelled: boolean;
  isExternal: boolean;
  isHidden: boolean;
  numberOfParticipants: number;
  shortname?: string;
  customHexColor?: string;
}

export interface IEventWriteModel {
  title: string;
  description: string;
  location: string;
  offices?: Office[];
  startDate: IDateTime;
  endDate: IDateTime;
  openForRegistrationTime: TimeInstanceContract;
  closeRegistrationTime?: TimeInstanceContract;
  organizerName: string;
  organizerEmail: string;
  maxParticipants?: number;
  viewUrlTemplate: string;
  editUrlTemplate: string;
  cancelParticipationUrlTemplate: string;
  participantQuestions: string[];
  program?: string;
  hasWaitingList: boolean;
  isExternal: boolean;
  isHidden: boolean;
  shortname?: string;
  customHexColor?: string;
}

type UnlimitedParticipants = ['unlimited'];
type LimitedParticipants<max> = ['limited', max];
export type MaxParticipants<max> =
  | UnlimitedParticipants
  | LimitedParticipants<max>;

export const isMaxParticipantsLimited = <t>(
  max: MaxParticipants<t>
): max is LimitedParticipants<t> => max[0] === 'limited';

export const maxParticipantsLimit = <t>(prop: LimitedParticipants<t>): t =>
  prop[1];

export interface IEvent {
  title: string;
  description: string;
  location: string;
  offices?: PickedOffice;
  start: IDateTime;
  end: IDateTime;
  openForRegistrationTime: TimeInstance;
  closeRegistrationTime?: TimeInstance;
  organizerName: string;
  organizerEmail: Email;
  maxParticipants: MaxParticipants<number>;
  participantQuestions: string[];
  program?: string;
  hasWaitingList: boolean;
  isCancelled: boolean;
  isExternal: boolean;
  isHidden: boolean;
  numberOfParticipants: number;
  shortname?: string;
  customHexColor?: string;
}

export interface IEditEvent {
  title: string;
  description: string;
  location: string;
  offices?: PickedOffice;
  start: EditDateTime;
  end: EditDateTime;
  openForRegistrationTime: TimeInstanceEdit;
  closeRegistrationTime?: TimeInstanceEdit;
  organizerName: string;
  organizerEmail: string;
  maxParticipants: MaxParticipants<string>;
  participantQuestions: string[];
  program?: string;
  hasWaitingList: boolean;
  isCancelled: boolean;
  isExternal: boolean;
  isHidden: boolean;
  numberOfParticipants: number;
  shortname?: string;
  customHexColor?: string;
}

export const parseEditEvent = ({
  title,
  description,
  location,
  offices,
  start,
  end,
  openForRegistrationTime,
  closeRegistrationTime,
  organizerName,
  organizerEmail,
  maxParticipants,
  participantQuestions,
  program,
  hasWaitingList,
  isCancelled,
  isExternal,
  isHidden,
  numberOfParticipants,
  shortname,
  customHexColor,
}: IEditEvent): IEvent | IError[] => {
  const event = {
    title: parseTitle(title),
    description: parseDescription(description),
    location: parseLocation(location),
    offices: offices && parsePickedOffices(offices),
    start: parseEditDateTime(start),
    end: parseEditDateTime(end),
    openForRegistrationTime: parseEditTimeInstance(openForRegistrationTime),
    closeRegistrationTime: closeRegistrationTime
      ? parseEditTimeInstance(closeRegistrationTime)
      : undefined,
    organizerName: parseName(organizerName),
    organizerEmail: parseEditEmail(organizerEmail),
    maxParticipants: parseMaxAttendees(maxParticipants),
    participantQuestions: parseQuestions(participantQuestions),
    program: program ? parseProgram(program) : undefined,
    hasWaitingList,
    isCancelled,
    isExternal,
    isHidden,
    numberOfParticipants,
    shortname: parseShortname(shortname),
    customHexColor,
  };

  try {
    assertIsValid(event);
  } catch {
    return listOfErrors(event);
  }

  return event;
};

export const toEventWriteModel = (event: IEvent): IEventWriteModel => ({
  ...event,
  maxParticipants: isMaxParticipantsLimited(event.maxParticipants)
    ? maxParticipantsLimit(event.maxParticipants)
    : undefined,
  openForRegistrationTime: toTimeInstanceWriteModel(
    event.openForRegistrationTime
  ),
  closeRegistrationTime: event.closeRegistrationTime
    ? toTimeInstanceWriteModel(event.closeRegistrationTime)
    : undefined,
  offices: pickedOfficeToOfficeList(event.offices),
  organizerEmail: toEmailWriteModel(event.organizerEmail),
  startDate: event.start,
  endDate: event.end,
  viewUrlTemplate: createViewUrlTemplate(event),
  editUrlTemplate: editUrlTemplate,
  cancelParticipationUrlTemplate: cancelParticipationUrlTemplate,
});

export const parseEventViewModel = (eventView: IEventViewModel): IEvent => {
  const title = parseTitle(eventView.title);
  const location = parseLocation(eventView.location);
  const offices = parseOffices(eventView.offices);
  const description = parseDescription(eventView.description);

  const start = parseDateViewModel(eventView.startDate);
  const end = parseDateViewModel(eventView.endDate);
  const openForRegistration = parseTimeInstanceViewModel(
    eventView.openForRegistrationTime
  );
  const closeRegistration = eventView.closeRegistrationTime
    ? parseTimeInstanceViewModel(eventView.closeRegistrationTime)
    : undefined;

  const organizerName = parseHost(eventView.organizerName);
  const organizerEmail = parseEditEmail(eventView.organizerEmail);
  const maxParticipants = parseMaxAttendees(
    eventView.maxParticipants !== undefined
      ? (['limited', eventView.maxParticipants.toString()] as [
          'limited',
          string
        ])
      : (['unlimited'] as ['unlimited'])
  );
  const program = eventView.program
    ? parseProgram(eventView.program)
    : undefined;
  const participantQuestions = parseQuestions(eventView.participantQuestions);
  const hasWaitingList = eventView.hasWaitingList;
  const isCancelled = eventView.isCancelled;
  const isExternal = eventView.isExternal;
  const isHidden = eventView.isHidden;
  const numberOfParticipants = eventView.numberOfParticipants;
  const shortname = parseShortname(eventView.shortname);
  const customHexColor = eventView.customHexColor;

  const event = {
    title,
    location,
    offices,
    description,
    start,
    end,
    openForRegistrationTime: openForRegistration,
    closeRegistrationTime: closeRegistration,
    organizerName,
    organizerEmail,
    maxParticipants,
    participantQuestions,
    program,
    hasWaitingList,
    isCancelled,
    isExternal,
    isHidden,
    numberOfParticipants,
    shortname,
    customHexColor,
  };

  assertIsValid(event);

  return event;
};

export const toEditEvent = ({
  title,
  description,
  location,
  offices,
  start,
  end,
  openForRegistrationTime,
  closeRegistrationTime,
  organizerName,
  organizerEmail,
  maxParticipants,
  participantQuestions,
  program,
  hasWaitingList,
  isCancelled,
  isExternal,
  isHidden,
  numberOfParticipants,
  shortname,
  customHexColor,
}: IEvent): IEditEvent => ({
  title,
  description,
  location,
  offices,
  start: toEditDateTime(start),
  end: toEditDateTime(end),
  openForRegistrationTime: toEditTimeInstance(openForRegistrationTime),
  closeRegistrationTime: closeRegistrationTime
    ? toEditTimeInstance(closeRegistrationTime)
    : undefined,
  organizerName,
  organizerEmail: toEditEmail(organizerEmail),
  maxParticipants: toEditMaxAttendees(maxParticipants),
  participantQuestions,
  program,
  hasWaitingList,
  isCancelled,
  isExternal,
  isHidden,
  numberOfParticipants,
  shortname,
  customHexColor,
});

export const initialEditEvent = (email?: string, name?: string): IEditEvent => {
  const eventStartDate = new Date();
  const openForRegistrationTime = toEditTimeInstance(new Date());
  return {
    title: '',
    description: '',
    location: '',
    offices: { Oslo: true, Trondheim: true },
    start: {
      date: toEditDate(dateToIDate(eventStartDate)),
      time: toEditTime({ hour: 17, minute: 0 }),
    },
    end: {
      date: toEditDate(dateToIDate(eventStartDate)),
      time: toEditTime({ hour: 20, minute: 0 }),
    },
    openForRegistrationTime,
    closeRegistrationTime: undefined,
    organizerName: name ?? '',
    organizerEmail: email ?? '',
    maxParticipants: ['limited', ''],
    participantQuestions: [],
    program: undefined,
    hasWaitingList: true,
    isCancelled: false,
    isExternal: false,
    isHidden: false,
    numberOfParticipants: 0,
  };
};

export const urlFromShortname = (shortname: string) => {
  const hostAndProtocol = document.location.origin;
  return hostAndProtocol + viewEventShortnameRoute(shortname);
};

export const incrementOneWeek = (event: IEvent): IEvent => {
  return {
    ...event,
    start: { ...event.start, date: addWeek(event.start.date) },
    end: { ...event.end, date: addWeek(event.end.date) },
    openForRegistrationTime: addWeekToTimeInstance(
      event.openForRegistrationTime
    ),
    closeRegistrationTime:
      event.closeRegistrationTime &&
      addWeekToTimeInstance(event.closeRegistrationTime),
  };
};

export const pickedOfficeToOfficeList = (
  pickedOffice?: PickedOffice
): Office[] | undefined => {
  if (pickedOffice === undefined) return undefined;

  return Object.keys(pickedOffice).filter(
    (y: string) => pickedOffice[y as keyof PickedOffice]
  ) as Office[];
};
