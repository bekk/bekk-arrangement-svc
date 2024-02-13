import React, { useState } from 'react';
import {
  IEditParticipant,
  toEditParticipant,
  initalParticipant,
  validateParticipation,
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
import { ValidationResult } from 'src/components/Common/ValidationResult/ValidationResult';

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
        event.participantQuestions,
        email,
        name,
        department
      )
    )
  );

  const [isSubmitClicked, setIsSubmitClicked] = useState(false);
  const [waitingOnParticipation, setWaitingOnParticipation] = useState(false);

  const validatedParticipant = validateParticipation(participant);

  const timeLeft = useTimeLeft(event.openForRegistrationTime);

  const { saveParticipation } = useSavedParticipations();

  const participate = catchAndNotify(async () => {
    setIsSubmitClicked(true);

    if (isValid(validatedParticipant)) {
      setWaitingOnParticipation(true);

      const response = await postParticipant(
        event,
        eventId,
        validatedParticipant
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
    oldI: number,
    required: boolean
  ) => {
    if (i === oldI) {
      const newAnswer: IQuestionAndAnswerViewModel = {
        questionId,
        eventId: eventId,
        email: participant.email,
        question,
        answer: answer,
        required
      };
      return newAnswer;
    }
    return a;
  };

  const labelForQuestion = (question: string, required: boolean) =>
    required ?
      `${question} *`
      : question;

  return (
    <div className={style.addParticipantContainer}>
      <div>
        <ValidatedTextInput
          label={labelForQuestion("Navn", true)}
          placeholder="Ola Nordmann"
          value={participant.name}
          validation={parseName}
          isSubmitClicked={isSubmitClicked}
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
          label={labelForQuestion("E-post", true)}
          placeholder="ola.nordmann@bekk.no"
          value={participant.email}
          validation={parseEditEmail}
          isSubmitClicked={isSubmitClicked}
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
            key={q.id}
            question={labelForQuestion(actualQuestion, q.required)}
            alternatives={alternatives}
            value={foundAnswer?.answer || ''}
            validation={(answer) => parseAnswerString(answer, q.required)}
            isSubmitClicked={isSubmitClicked}
            onChange={(s) =>
              setParticipant({
                ...participant,
                participantAnswers: participant.participantAnswers.map(
                  (a, oldI) =>
                    updateQuestion(a, questionId, q.question, s, i, oldI, q.required)
                ),
              })
            }
          />
        ) : (
          <div key={q.id}>
            <ValidatedTextArea
              label={labelForQuestion(q.question, q.required)}
              placeholder={''}
              value={foundAnswer?.answer || ''}
              validation={(answer) => parseAnswerString(answer, q.required)}
              isSubmitClicked={isSubmitClicked}
              onChange={(s) =>
                setParticipant({
                  ...participant,
                  participantAnswers: participant.participantAnswers.map(
                    (a, oldI) =>
                      updateQuestion(a, questionId, q.question, s, i, oldI, q.required)
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
      {isSubmitClicked && !isValid(validatedParticipant) && (
        <ValidationResult validationResult={validatedParticipant} />
      )}
    </div>
  );
};
