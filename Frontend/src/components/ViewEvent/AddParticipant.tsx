import React, { useState } from 'react';
import {
  IEditParticipant,
  toEditParticipant,
  initalParticipant,
  parseEditParticipant,
  parseName,
  parseAnswers,
} from 'src/types/participant';
import { ValidatedTextInput } from 'src/components/Common/ValidatedTextInput/ValidatedTextInput';
import { parseEditEmail } from 'src/types/email';
import { Button } from 'src/components/Common/Button/Button';
import { IEvent } from 'src/types/event';
import { useNotification } from 'src/components/NotificationHandler/NotificationHandler';
import { cancelParticipantRoute, confirmParticipantRoute } from 'src/routing';
import { postParticipant } from 'src/api/arrangementSvc';
import { isValid } from 'src/types/validation';
import { useHistory } from 'react-router';
import { useSavedParticipations } from 'src/hooks/saved-tokens';
import { useTimeLeft } from 'src/hooks/timeleftHooks';
import { ValidatedTextArea } from 'src/components/Common/ValidatedTextArea/ValidatedTextArea';
import style from './ViewEventContainer.module.scss';
import classNames from 'classnames';
import {
  multipleChoiceAlternatives,
  MultipleChoiceQuestion,
} from 'src/components/ViewEvent/MultipleChoiceQuestion';

interface Props {
  eventId: string;
  event: IEvent;
  email?: string;
  name?: string;
}

export const AddParticipant = ({ eventId, event, email, name }: Props) => {
  const { catchAndNotify } = useNotification();
  const history = useHistory();

  const [participant, setParticipant] = useState<IEditParticipant>(
    toEditParticipant(
      initalParticipant(event.participantQuestions.length, email, name)
    )
  );

  const [waitingOnParticipation, setWaitingOnParticipation] = useState(false);

  const validParticipant = validateParticipation(participant);

  const timeLeft = useTimeLeft(event.openForRegistrationTime);

  const { saveParticipation } = useSavedParticipations();
  const participate = catchAndNotify(async () => {
    if (validParticipant) {
      const cancelUrlTemplate =
        document.location.origin +
        cancelParticipantRoute({
          eventId: '{eventId}',
          email: '{email}',
          cancellationToken: '{cancellationToken}',
        });

      setWaitingOnParticipation(true);

      const {
        participant: { email = '' },
        cancellationToken,
      } = await postParticipant(
        eventId,
        validParticipant,
        cancelUrlTemplate
      ).catch((e) => {
        setWaitingOnParticipation(false);
        throw e;
      });

      saveParticipation({ eventId, email, cancellationToken });

      history.push(
        confirmParticipantRoute({
          eventId,
          email: encodeURIComponent(email),
        })
      );
    }
  });

  return (
    <div className={style.addParticipantContainer}>
      <div>
        <ValidatedTextInput
          label={'Navn'}
          placeholder={'Ola Nordmann'}
          value={participant.name}
          validation={parseName}
          onChange={(name: string) =>
            setParticipant({
              ...participant,
              name,
            })
          }
        />
      </div>
      <div>
        <ValidatedTextInput
          label={'E-post'}
          placeholder={'ola.nordmann@bekk.no'}
          value={participant.email}
          validation={parseEditEmail}
          onChange={(email: string) =>
            setParticipant({
              ...participant,
              email,
            })
          }
        />
      </div>
      {event.participantQuestions.map((q, i) => {
        const { isMultipleChoiceQuestion, alternatives, actualQuestion } =
          multipleChoiceAlternatives(q);
        return isMultipleChoiceQuestion ? (
          <MultipleChoiceQuestion
            question={actualQuestion}
            alternatives={alternatives}
            value={participant.participantAnswers[i]}
            onChange={(s) =>
              setParticipant({
                ...participant,
                participantAnswers: participant.participantAnswers.map(
                  (a, oldI) => {
                    if (i === oldI) {
                      return s;
                    }
                    return a;
                  }
                ),
              })
            }
          />
        ) : (
          <div key={q}>
            <ValidatedTextArea
              label={q}
              placeholder={''}
              value={participant.participantAnswers[i] ?? ''}
              validation={(answer) => parseAnswers([answer])}
              minRow={3}
              onChange={(answer: string) =>
                setParticipant({
                  ...participant,
                  participantAnswers: participant.participantAnswers.map(
                    (a, oldI) => {
                      if (i === oldI) {
                        return answer;
                      }
                      return a;
                    }
                  ),
                })
              }
            />
          </div>
        );
      })}
      <Button
        onClick={participate}
        className={classNames({
          [style.loadingSpinner]: waitingOnParticipation,
        })}
        disabled={timeLeft.difference > 0 || waitingOnParticipation}
      >
        {!waitingOnParticipation ? 'Meld meg på' : 'Melder på...'}
      </Button>
    </div>
  );
};

const validateParticipation = (participant: IEditParticipant) => {
  const vParticipant = parseEditParticipant(participant);
  if (isValid(vParticipant)) {
    return vParticipant;
  }
};
