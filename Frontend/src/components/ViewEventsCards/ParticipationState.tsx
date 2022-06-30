import React from 'react';
import { capitalize } from 'src/components/ViewEvent/ViewEvent';
import { editEventRoute, viewEventRoute } from 'src/routing';
import { IEvent } from 'src/types/event';
import { dateToITime, stringifyTime } from 'src/types/time';
import { stringifyTimeInstanceWithDayName } from 'src/types/time-instance';
import { plural } from 'src/utils';
import style from './EventCardElement.module.scss';
import { SmileIcon } from 'src/components/Common/Icons/SmileIcon';
import { FrownyFaceIcon } from 'src/components/Common/Icons/FrownyFaceIcon';
import { useHistory } from 'react-router';
import { Button } from 'src/components/Common/Button/Button';

export enum EventState  {
  Rediger = 'Rediger',
  IkkeApnet = 'Ikke åpnet',
  PameldingHarStengt = 'Påmeldingen har stengt',
  Avsluttet = 'Avsluttet',
  Pameldt = 'Påmeldt',
  PaVenteliste = 'På venteliste',
  Plass = 'Plass',
  PlassPaVenteliste = 'Plass på venteliste',
  Fullt = 'Fullt',
  Laster = 'Laster',
  Avlyst = 'Avlyst',
  IkkePameldt = "ikke-påmeldt"
}

interface IProps {
  eventId: string;
  eventState: EventState;
  event: IEvent;
  numberOfAvailableSpots?: number;
  waitingListSpot?: number;
}

export const ParticipationState = ({
  eventId,
  eventState,
  event,
  numberOfAvailableSpots,
  waitingListSpot,
}: IProps) => {
  const history = useHistory();
  const route = (path: string, htmlEvent: any) => {
    htmlEvent.stopPropagation();
    htmlEvent.preventDefault();
    history.push(path);
  };
  switch (eventState) {
    case EventState.Rediger:
      return (
        <Button
          onClick={(htmlEvent: any) =>
            route(editEventRoute(eventId), htmlEvent)
          }
        >
          Rediger
        </Button>
      );

    case EventState.IkkeApnet:
      const openForRegistrationTime = event.openForRegistrationTime;
      const dateTimeText = `${capitalize(
        stringifyTimeInstanceWithDayName(openForRegistrationTime)
      )}, ${stringifyTime(dateToITime(openForRegistrationTime))}`;
      return (
        <div className={style.stateText}>
          Påmelding åpner <br /> {dateTimeText}
        </div>
      );


    case EventState.PameldingHarStengt:
      return <div className={style.stateText}>Påmelding er stengt</div>;

    case EventState.Avsluttet:
      return <div className={style.stateText}>Arrangementet er avsluttet</div>;

    case EventState.Pameldt:
      return (
        <div className={style.stateIconContainer}>
          <SmileIcon className={style.stateIcon} />
          Du er påmeldt!
        </div>
      );

    case EventState.PaVenteliste:
      return (
        <div className={style.stateText}>
          Du er nr. {waitingListSpot} på venteliste
        </div>
      );

    case EventState.Plass:
      if (numberOfAvailableSpots !== undefined && numberOfAvailableSpots <= 5) {
        return (
          <div className={style.stateContainer}>
            <div className={style.stateText}>
              {plural(numberOfAvailableSpots, "plass", "plasser")} igjen!
            </div>
            <Button
              onClick={(htmlEvent: any) =>
                route(viewEventRoute(eventId), htmlEvent)
              }
            >
              Meld deg på
            </Button>
          </div>
        );
      }
      return (
        <Button
          onClick={(htmlEvent: any) =>
            route(viewEventRoute(eventId), htmlEvent)
          }
        >
          Meld deg på
        </Button>
      );

    case EventState.PlassPaVenteliste:
      return (
        <div className={style.stateContainer}>
          <div className={style.stateText}>Arrangementet er fullt.</div>
          <Button
            onClick={(htmlEvent: any) =>
              route(viewEventRoute(eventId), htmlEvent)
            }
          >
            Sett på venteliste
          </Button>
        </div>
      );

    case EventState.Fullt:
      return <div className={style.stateText}>Arrangementet er fullt</div>;

    case EventState.Avlyst:
      return (
        <div className={style.stateIconContainer}>
          <FrownyFaceIcon className={style.stateIcon} />
          Avlyst
        </div>
      );

    case EventState.Laster:
      return <div className={style.stateText}>Laster...</div>;

    case EventState.IkkePameldt:
      return null;
  }
};
