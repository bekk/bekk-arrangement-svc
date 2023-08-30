import React, { useState } from 'react';
import {
  IEditParticipant,
  toEditParticipant,
  initalParticipant,
  parseEditParticipant,
  parseName,
  IQuestionAndAnswerViewModel,
  parseAnswerString,
} from 'src/types/participant';
import { ValidatedTextInput } from 'src/components/Common/ValidatedTextInput/ValidatedTextInput';
import { parseEditEmail } from 'src/types/email';
import { Button } from 'src/components/Common/Button/Button';
import { IEvent } from 'src/types/event';
import { useNotification } from 'src/components/NotificationHandler/NotificationHandler';
import { confirmParticipantRoute } from 'src/routing';
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
  department?: string;
}

export const AddParticipant = ({
  eventId,
  event,
  email,
  name,
  department,
}: Props) => {
  const { catchAndNotify } = useNotification();
  const history = useHistory();

  const [participant, setParticipant] = useState<IEditParticipant>(
    toEditParticipant(
      initalParticipant(
        event.participantQuestions.length,
        email,
        name,
        department
      )
    )
  );

  const [waitingOnParticipation, setWaitingOnParticipation] = useState(false);

  const validParticipant = validateParticipation(participant);

  const timeLeft = useTimeLeft(event.openForRegistrationTime);

  const { saveParticipation } = useSavedParticipations();
  const participate = catchAndNotify(async () => {
    if (validParticipant) {
      setWaitingOnParticipation(true);

      const response = await postParticipant(
        event,
        eventId,
        validParticipant
      ).catch((e) => {
        setWaitingOnParticipation(false);
        throw e;
      });

      saveParticipation({
        eventId,
        email: response.participant.email || '',
        cancellationToken: response.cancellationToken,
        questionAndAnswers: response.participant.questionAndAnswers,
      });

      history.push(
        confirmParticipantRoute({
          eventId,
          email: encodeURIComponent(response.participant.email || ''),
        })
      );
    }
  });

  const updateQuestion = (
    a: IQuestionAndAnswerViewModel,
    questionId: number,
    question: string,
    answer: string,
    i: number,
    oldI: number
  ) => {
    if (i === oldI) {
      const newAnswer: IQuestionAndAnswerViewModel = {
        questionId,
        eventId: eventId,
        email: email || '',
        question,
        answer,
      };
      return newAnswer;
    }
    return a;
  };

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
              participantAnswers: participant.participantAnswers.map(
                (answer) => ({ ...answer, email: email || '' })
              ),
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
              participantAnswers: participant.participantAnswers.map(
                (answer) => ({ ...answer, email })
              ),
            })
          }
        />
      </div>
      {event.participantQuestions.map((q, i) => {
        if (q.id === undefined)
          return <>Det skjedde en feil under lasting av spørsmål</>;

        const questionId = q.id;

        const { isMultipleChoiceQuestion, alternatives, actualQuestion } =
          multipleChoiceAlternatives(q.question);
        const foundAnswer = participant.participantAnswers.find(
          (answer) => answer.questionId === questionId
        );
        return isMultipleChoiceQuestion ? (
          <MultipleChoiceQuestion
            question={actualQuestion}
            alternatives={alternatives}
            value={foundAnswer?.answer || ''}
            onChange={(s) =>
              setParticipant({
                ...participant,
                participantAnswers: participant.participantAnswers.map(
                  (a, oldI) =>
                    updateQuestion(a, questionId, q.question, s, i, oldI)
                ),
              })
            }
          />
        ) : (
          <div key={q.question}>
            <ValidatedTextArea
              label={q.question}
              placeholder={''}
              value={foundAnswer?.answer || ''}
              validation={(answer) => parseAnswerString(answer)}
              onChange={(s) =>
                setParticipant({
                  ...participant,
                  participantAnswers: participant.participantAnswers.map(
                    (a, oldI) =>
                      updateQuestion(a, questionId, q.question, s, i, oldI)
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
        disabled={timeLeft.difference > 0 || waitingOnParticipation}>
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
