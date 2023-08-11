import React, { useRef, useState } from 'react';
import {
  IEditEvent,
  isMaxParticipantsLimited,
  maxParticipantsLimit,
  urlFromShortname,
} from 'src/types/event';
import {
  parseTitle,
  parseDescription,
  parseHost,
  parseMaxAttendees,
  parseLocation,
  parseQuestions,
  parseShortname,
  parseProgram,
} from 'src/types';
import { ValidatedTextInput } from 'src/components/Common/ValidatedTextInput/ValidatedTextInput';
import { DateTimeInputWithTimezone } from 'src/components/Common/DateTimeInput/DateTimeInputWithTimezone';
import { parseEditEmail } from 'src/types/email';
import {
  EditDateTime,
  parseEditDateTime,
  isInOrder,
} from 'src/types/date-time';
import { isValid } from 'src/types/validation';
import { ValidatedTextArea } from 'src/components/Common/ValidatedTextArea/ValidatedTextArea';
import { Button } from 'src/components/Common/Button/Button';
import style from './EventForm.module.scss';
import { TimeInput } from 'src/components/Common/TimeInput/TimeInput';
import { DateInput } from 'src/components/Common/DateInput/DateInput';
import { ValidationResult } from 'src/components/Common/ValidationResult/ValidationResult';
import { datesInOrder, EditDate, parseEditDate } from 'src/types/date';
import { EditTime, parseEditTime, toEditTime } from 'src/types/time';
import { InfoBox } from 'src/components/Common/InfoBox/InfoBox';
import { CheckBox } from 'src/components/Common/Checkbox/CheckBox';

interface IProps {
  eventResult: IEditEvent;
  updateEvent: (event: IEditEvent) => void;
}

export const EventForm = ({ eventResult: event, updateEvent }: IProps) => {
  const [isMultiDayEvent, setMultiDay] = useState(
    event.start.date !== event.end.date
  );

  const [hasShortname, _setHasShortname] = useState(
    event.shortname !== undefined
  );
  const setHasShortname = (hasShortname: boolean) => {
    _setHasShortname(hasShortname);
    if (!hasShortname) {
      updateEvent({ ...event, shortname: undefined });
    }
  };

  const [hasCustomColor, _setHasCustomColor] = useState(
    event.customHexColor !== undefined
  );
  const setHasCustomColor = (hasCustomColor: boolean) => {
    _setHasCustomColor(hasCustomColor);
    if (!hasCustomColor) {
      updateEvent({ ...event, customHexColor: undefined });
    }
  };

  const [hasProgram, _setHasProgram] = useState(event.program !== undefined);
  const setHasProgram = (hasProgram: boolean) => {
    _setHasProgram(hasProgram);
    if (!hasProgram) {
      updateEvent({ ...event, program: undefined });
    }
  };

  const hasUnlimitedSpots = !isMaxParticipantsLimited(event.maxParticipants);

  const validatedStarTime = parseEditDateTime(event.start);
  const validateEndTime = parseEditDateTime(event.end);

  const debounce = useDebounce();

  return (
    <div className={style.container}>
      <div className={style.column}>
        <div>
          <ValidatedTextInput
            label={labels.title}
            placeholder={placeholders.title}
            value={event.title}
            validation={parseTitle}
            onLightBackground
            onChange={(title) =>
              updateEvent({
                ...event,
                title,
              })
            }
          />
        </div>
        <div>
          <ValidatedTextInput
            label={labels.location}
            placeholder={placeholders.location}
            value={event.location}
            validation={parseLocation}
            onLightBackground
            onChange={(location) =>
              updateEvent({
                ...event,
                location,
              })
            }
          />
        </div>
        <div className={style.office}>
          <CheckBox
            onChange={() =>
              updateEvent({
                ...event,
                offices: {
                  Oslo: !event.offices?.Oslo,
                  Trondheim: event.offices?.Trondheim || false,
                },
              })
            }
            isChecked={event.offices?.Oslo || false}
            label="Oslo"
          />
          <CheckBox
            onChange={() =>
              updateEvent({
                ...event,
                offices: {
                  Oslo: event.offices?.Oslo || false,
                  Trondheim: !event.offices?.Trondheim,
                },
              })
            }
            isChecked={event.offices?.Trondheim || false}
            label="Trondheim"
          />
        </div>
        <div>
          <div
            className={
              isMultiDayEvent
                ? style.startEndDateContainer
                : style.startDateContainer
            }>
            <div className={style.startDate}>
              <DateInput
                value={event.start.date}
                label={labels.startDate}
                onChange={(date) =>
                  updateEvent({
                    ...event,
                    ...setStartEndDates(
                      event,
                      isMultiDayEvent
                        ? ['set-start-date', date]
                        : ['set-same-date', date]
                    ),
                  })
                }
              />
            </div>
            <div className={style.startTime}>
              <TimeInput
                value={event.start.time}
                label={
                  isMultiDayEvent ? labels.timeWithEndDate : labels.startTime
                }
                onChange={(time) =>
                  updateEvent({
                    ...event,
                    ...setStartEndDates(event, ['set-start-time', time]),
                  })
                }
              />
            </div>
            <div className={style.endTime}>
              <TimeInput
                value={event.end.time}
                label={
                  isMultiDayEvent ? labels.timeWithEndDate : labels.endTime
                }
                onChange={(time) =>
                  updateEvent({
                    ...event,
                    ...setStartEndDates(event, ['set-end-time', time]),
                  })
                }
              />
            </div>
            <div className={style.dateError}>
              {!isValid(validatedStarTime) && (
                <ValidationResult
                  onLightBackground
                  validationResult={validatedStarTime}
                />
              )}
              {!isValid(validateEndTime) && (
                <ValidationResult
                  onLightBackground
                  validationResult={validateEndTime}
                />
              )}
            </div>
            {isMultiDayEvent && (
              <div className={style.endDate}>
                <DateInput
                  value={event.end.date}
                  label={labels.endDate}
                  onChange={(date) =>
                    updateEvent({
                      ...event,
                      ...setStartEndDates(event, ['set-end-date', date]),
                    })
                  }
                />
                {!isValid(validateEndTime) && (
                  <ValidationResult
                    onLightBackground
                    validationResult={validateEndTime}
                  />
                )}
              </div>
            )}
          </div>
          <Button
            onClick={() => {
              if (isMultiDayEvent) {
                updateEvent({
                  ...event,
                  ...setStartEndDates(event, [
                    'set-same-date',
                    event.start.date,
                  ]),
                });
              }
              setMultiDay(!isMultiDayEvent);
            }}
            displayAsLink
            onLightBackground>
            {isMultiDayEvent ? buttonText.removeEndDate : buttonText.addEndDate}
          </Button>
        </div>
        <div>
          <ValidatedTextArea
            className={style.textAreaContainer}
            label={labels.description}
            placeholder={placeholders.description}
            value={event.description}
            validation={parseDescription}
            onLightBackground
            minRow={8}
            onChange={(description) =>
              updateEvent({
                ...event,
                description,
              })
            }
          />
          <FormattingHelper />
        </div>
        {!hasProgram && (
          <Button
            color="Secondary"
            displayAsLink
            onLightBackground
            className={style.participantQuestion}
            onClick={() => setHasProgram(true)}>
            {buttonText.addProgram}
          </Button>
        )}
        {hasProgram && (
          <div>
            <ValidatedTextArea
              className={style.textAreaContainer}
              label={labels.program}
              placeholder={placeholders.program}
              value={event.program ?? ''}
              validation={parseProgram}
              onLightBackground
              minRow={8}
              onChange={(program) =>
                updateEvent({
                  ...event,
                  program,
                })
              }
            />
            <FormattingHelper />
            <Button
              color="Secondary"
              displayAsLink
              onLightBackground
              className={style.programButton}
              onClick={() => setHasProgram(false)}>
              {buttonText.removeProgram}
            </Button>
          </div>
        )}
      </div>

      <div className={style.column}>
        <div>
          <ValidatedTextInput
            label={labels.organizerName}
            placeholder={placeholders.organizerName}
            value={event.organizerName}
            validation={parseHost}
            onLightBackground
            onChange={(organizerName) =>
              updateEvent({
                ...event,
                organizerName,
              })
            }
          />
        </div>
        <div>
          <ValidatedTextInput
            label={labels.organizerEmail}
            placeholder={placeholders.organizerEmail}
            value={event.organizerEmail}
            validation={parseEditEmail}
            onLightBackground
            onChange={(organizerEmail) =>
              updateEvent({
                ...event,
                organizerEmail,
              })
            }
          />
        </div>
        <DateTimeInputWithTimezone
          labelDate={labels.registrationStartDate}
          labelTime={labels.registrationTime}
          value={event.openForRegistrationTime}
          onChange={(openForRegistrationTime) =>
            updateEvent({
              ...event,
              openForRegistrationTime,
            })
          }
        />
        <div>
          {event.closeRegistrationTime && (
            <DateTimeInputWithTimezone
              labelDate={labels.registrationEndDate}
              labelTime={labels.registrationTime}
              value={event.closeRegistrationTime}
              onChange={(closeRegistrationTime) =>
                updateEvent({
                  ...event,
                  closeRegistrationTime,
                })
              }
            />
          )}
          <Button
            className={style.removeQButton}
            displayAsLink
            onLightBackground
            onClick={() => {
              if (event.closeRegistrationTime) {
                updateEvent({ ...event, closeRegistrationTime: undefined });
              } else {
                updateEvent({
                  ...event,
                  closeRegistrationTime: event.openForRegistrationTime,
                });
              }
            }}>
            {event.closeRegistrationTime
              ? buttonText.removeRegistrationEndDate
              : buttonText.addRegistrationEndDate}
          </Button>
        </div>

        <div className={style.unlimitedSpots}>
          <CheckBox
            label={labels.unlimitedSpots}
            isChecked={hasUnlimitedSpots}
            onChange={(limited) => {
              if (limited) {
                updateEvent({
                  ...event,
                  maxParticipants: ['unlimited'],
                  hasWaitingList: false,
                });
              } else {
                updateEvent({
                  ...event,
                  maxParticipants: ['limited', ''],
                  hasWaitingList: false,
                });
              }
            }}
          />
        </div>
        <div>
          {!hasUnlimitedSpots &&
            isMaxParticipantsLimited(event.maxParticipants) && (
              <div className={style.limitSpots}>
                <div>
                  <ValidatedTextInput
                    label={labels.limitSpots}
                    placeholder={placeholders.limitSpots}
                    value={maxParticipantsLimit(event.maxParticipants)}
                    isNumber={true}
                    validation={(max) => parseMaxAttendees(['limited', max])}
                    onLightBackground
                    onChange={(maxParticipants) =>
                      updateEvent({
                        ...event,
                        maxParticipants: ['limited', maxParticipants],
                      })
                    }
                  />
                </div>
                <div className={style.waitListCheckBox}>
                  <CheckBox
                    label={labels.waitingList}
                    onChange={(hasWaitingList) =>
                      updateEvent({ ...event, hasWaitingList })
                    }
                    isChecked={event.hasWaitingList}
                  />
                </div>
              </div>
            )}
        </div>
        <div>
          <CheckBox
            label={labels.externalEvent}
            onChange={(isExternal) => updateEvent({ ...event, isExternal })}
            isChecked={event.isExternal}
          />
          <p className={style.helpTextCheckBox}>{helpText.externalEvent}</p>
          <p className={style.helpTextCheckBoxRed}>
            {helpText.externalEventRed}
          </p>
        </div>
        <div>
          <CheckBox
            label={labels.hiddenEvent}
            onChange={(isHidden) => updateEvent({ ...event, isHidden })}
            isChecked={event.isHidden}
          />
          <p className={style.helpTextCheckBox}>{helpText.hiddenEvent}</p>
        </div>

        <div className={style.shortName}>
          <CheckBox
            label={'Tilpass URL'}
            isChecked={hasShortname}
            onChange={setHasShortname}
          />
          {hasShortname && (
            <div className={style.flex}>
              <div className={style.origin}>{urlFromShortname('')}</div>
              <div className={style.pathBox}>
                <ValidatedTextInput
                  label={''}
                  value={event.shortname || ''}
                  onChange={(shortname) => updateEvent({ ...event, shortname })}
                  validation={parseShortname}
                  onLightBackground
                />
              </div>
            </div>
          )}
        </div>

        <div className={style.customColor}>
          <CheckBox
            label={'Tilpass farge'}
            isChecked={hasCustomColor}
            onChange={setHasCustomColor}
          />
          {hasCustomColor && (
            <div className={style.flex}>
              <div className={style.chooseColor}>Velg farge på toppbanner:</div>
              <input
                type="color"
                value={`#${event.customHexColor}`}
                onChange={(htmlEvent) => {
                  const customHexColor = htmlEvent.target.value.slice(1);
                  debounce(() => updateEvent({ ...event, customHexColor }));
                }}
              />
            </div>
          )}
        </div>

        <div>
          {event.participantQuestions.map((q, i) => (
            <ValidatedTextArea
              key={q}
              label={labels.participantQuestion}
              placeholder={placeholders.participantQuestion}
              value={q}
              validation={(s) => parseQuestions([s])}
              onLightBackground
              minRow={4}
              onChange={(participantQuestion) =>
                updateEvent({
                  ...event,
                  participantQuestions: event.participantQuestions.map(
                    (oldQ, oldI) => {
                      if (i === oldI) {
                        return participantQuestion;
                      }
                      return oldQ;
                    }
                  ),
                })
              }
            />
          ))}
          {event.participantQuestions.length > 0 && (
            <Button
              className={style.removeQButton}
              displayAsLink
              onLightBackground
              onClick={() =>
                updateEvent({
                  ...event,
                  participantQuestions: event.participantQuestions
                    .reverse()
                    .slice(1)
                    .reverse(),
                })
              }>
              {buttonText.removeParticipantQuestion}
            </Button>
          )}
        </div>
        <Button
          color="Secondary"
          displayAsLink
          onLightBackground
          className={style.participantQuestion}
          onClick={() =>
            updateEvent({
              ...event,
              participantQuestions: event.participantQuestions.concat(['']),
            })
          }>
          {buttonText.addParticipantQuestion}
        </Button>
        {event.participantQuestions.length > 0 && (
          <InfoBox title="Formateringshjelp">
            <p>Spørsmål:</p>
            <p>
              &#47;&#47; Alternativer: alternativ 1; alternativ 2; alternativ 3
            </p>
          </InfoBox>
        )}
      </div>
    </div>
  );
};

function useDebounce(timeout = 300) {
  const timer = useRef<number>();
  return (f: () => void) => {
    clearTimeout(timer.current);
    timer.current = window.setTimeout(f, timeout);
  };
}

type Action =
  | ['set-same-date', EditDate]
  | ['set-start-date', EditDate]
  | ['set-end-date', EditDate]
  | ['set-start-time', EditTime]
  | ['set-end-time', EditTime];

type State = {
  start: EditDateTime;
  end: EditDateTime;
};

const setStartEndDates = ({ start, end }: State, message: Action): State => {
  switch (message[0]) {
    case 'set-same-date': {
      const date = message[1];
      return {
        start: { ...start, date },
        end: { ...end, date },
      };
    }
    case 'set-start-date': {
      const date = message[1];

      const first = parseEditDate(date);
      const last = parseEditDate(end.date);

      if (isValid(first) && isValid(last)) {
        if (!datesInOrder({ first, last })) {
          return { start: { ...start, date }, end: { ...end, date } };
        }
      }

      return { start: { ...start, date }, end };
    }
    case 'set-end-date': {
      const date = message[1];

      const first = parseEditDate(start.date);
      const last = parseEditDate(date);

      if (isValid(first) && isValid(last)) {
        if (!datesInOrder({ first, last })) {
          return { start: { ...start, date }, end: { ...end, date } };
        }
      }

      return { start, end: { ...end, date } };
    }
    case 'set-start-time': {
      const time = message[1];

      const first = parseEditDateTime({ ...start, time });
      const last = parseEditDateTime(end);

      if (isValid(first) && isValid(last)) {
        if (!isInOrder({ first, last })) {
          const startTime = parseEditTime(time);
          if (isValid(startTime)) {
            return {
              start: { ...start, time },
              end: {
                ...end,
                time: toEditTime({
                  hour: startTime.hour + 1,
                  minute: startTime.minute,
                }),
              },
            };
          }
        }
      }

      return { end, start: { ...start, time } };
    }
    case 'set-end-time': {
      const time = message[1];

      const first = parseEditDateTime(start);
      const last = parseEditDateTime({ ...end, time });

      if (isValid(first) && isValid(last)) {
        if (!isInOrder({ first, last })) {
          const endTime = parseEditTime(time);
          if (isValid(endTime)) {
            return {
              start: {
                ...start,
                time: toEditTime({
                  hour: endTime.hour - 1,
                  minute: endTime.minute,
                }),
              },
              end: { ...end, time },
            };
          }
        }
      }

      return { start, end: { ...end, time } };
    }
  }
};

const FormattingHelper = () => {
  return (
    <InfoBox title="Formateringshjelp">
      <ul className={style.listStyle}>
        <li>Bullet points med bindestrek (-)</li>
        <li>Overskrift med skigard (#)</li>
        <li>Lenker kan limes inn direkte</li>
        <li>Bold med dobbel asterisk (**) rundt teksten</li>
        <li>Italics med én asterisk (*) rundt teksten</li>
      </ul>
    </InfoBox>
  );
};

const labels = {
  title: 'Tittel*',
  startDate: 'Arrangementet starter*',
  startTime: 'Fra*',
  endTime: 'Til*',
  endDate: 'Arrangementet slutter*',
  timeWithEndDate: 'Kl*',
  location: 'Lokasjon*',
  description: 'Beskrivelse*',
  organizerName: 'Navn på arrangør*',
  organizerEmail: 'Arrangørens e-post*',
  registrationStartDate: 'Påmelding åpner*',
  registrationEndDate: 'Påmelding stenger*',
  registrationTime: 'Kl*',
  unlimitedSpots: 'Ubegrenset antall deltakere',
  limitSpots: 'Maks antall*',
  waitingList: 'Venteliste',
  externalEvent: 'Eksternt arrangement',
  hiddenEvent: 'Skjul arrangementet fra oversikten',
  participantQuestion: 'Spørsmål til deltakerne*',
  shortname: 'Lag en penere URL for arrangementet',
  program: 'Program',
};

const placeholders = {
  title: 'Navn på arrangementet ditt',
  location: 'Eventyrland',
  description:
    '# Overskrift\n\nVi støtter litt pseudomarkdown!\n\n- bullet points med bindestrek (-)\n- overskrifter med skigard (#)\n- du kan også paste inn linker direkte',
  organizerName: 'Kari Nordmann*',
  organizerEmail: 'kari.nordmann@bekk.no',
  participantQuestion: 'Allergier, preferanser eller noe annet på hjertet?',
  limitSpots: 'F.eks. 10',
  program:
    'Legg inn program for eventen. Vi støtter samme pseudomarkdown som for beskrivelsen.',
};

const helpText = {
  externalEvent:
    'Eksterne arrangement er tilgjengelig for personer utenfor Bekk.',
  externalEventRed:
    'Eksterne kan ikke se de påmeldtes e-postadresser eller navn.',
  hiddenEvent:
    'Arrangementet vil ikke dukke opp i oversikten over arrangementer eller på forsiden.',
};

const buttonText = {
  addEndDate: '+ Legg til sluttdato',
  removeEndDate: '- Fjern sluttdato',
  addRegistrationEndDate: '+ Legg til påmeldingsfrist',
  removeRegistrationEndDate: '- Fjern påmeldingsfrist',
  addParticipantQuestion: '+ Legg til spørsmål til deltakere',
  removeParticipantQuestion: '- Fjern spørsmål',
  addProgram: '+ Legg til program',
  removeProgram: '- Fjern program',
};