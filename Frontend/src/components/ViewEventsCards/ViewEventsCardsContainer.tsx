import React, { useEffect, useState } from 'react';
import style from './ViewEventsCards.module.scss';
import { createRoute } from 'src/routing';
import { hasLoaded, RemoteData } from 'src/remote-data';
import { Page } from 'src/components/Page/Page';
import { useFilteredEvents } from 'src/hooks/cache';
import { EventCardElement } from 'src/components/ViewEventsCards/EventCardElement';
import { Button } from 'src/components/Common/Button/Button';
import { useHistory } from 'react-router';
import { authenticateUser, isAuthenticated } from 'src/auth';
import { WavySubHeader } from 'src/components/Common/Header/WavySubHeader';
import { IEvent } from 'src/types/event';
import { isInOrder } from 'src/types/date-time';

import { useSetTitle } from 'src/hooks/setTitle';
import { appTitle } from 'src/Constants';
import {
  Filter,
  filterAccess,
  filterOffice,
  FilterOptions,
  filterType,
} from '../Common/Filter/Filter';
import {
  useSavedEditableEvents,
  useSavedParticipations,
} from '../../hooks/saved-tokens';

const initialFilterOptions: FilterOptions = {
  oslo: true,
  trondheim: true,
  alle: true,
  kommende: true,
  tidligere: false,
  mine: false,
  apent: false,
  lukket: false,
};

export const ViewEventsCardsContainer = () => {
  const [filterOptions, setFilterOptions] = useState(initialFilterOptions);
  const [filteredEvents, setFilteredEvents] = useState<[string, IEvent][]>([]);
  useSetTitle(appTitle);
  const savedEditableEvents = useSavedEditableEvents();
  const savedParticipations = useSavedParticipations();

  const fetchedEvents = eventMapToList(useFilteredEvents(filterOptions)).filter(
    (event) =>
      filterType(
        filterOptions,
        event,
        savedEditableEvents.savedEvents,
        savedParticipations.savedParticipations
      ) &&
      filterAccess(filterOptions, event) &&
      filterOffice(filterOptions, event)
  );

  useEffect(
    () => {
      setFilteredEvents(sortEvents(fetchedEvents));
    },
    // https://github.com/facebook/react/issues/14476 Dan Abramov says this is OK
    [JSON.stringify(fetchedEvents)]
  );

  return (
    <>
      <WavySubHeader eventId={'all-events'}>
        <div role="heading" aria-level={3} className={style.header}>
          <h1 className={style.headerText}>Hva skjer i Bekk?</h1>
          <AddEventButton />
        </div>
      </WavySubHeader>
      <Page>
        <div className={style.headerContainer}>
          <Filter
            filterState={initialFilterOptions}
            setFilterState={setFilterOptions}
          />
        </div>
        <div className={style.grid}>
          {filteredEvents.map(([id, event]) => (
            <EventCardElement key={id} eventId={id} event={event} />
          ))}
        </div>
      </Page>
    </>
  );
};

const AddEventButton = () => {
  const history = useHistory();
  if (isAuthenticated()) {
    return (
      <Button color={'Secondary'} onClick={() => history.push(createRoute)}>
        Opprett et arrangement
      </Button>
    );
  }
  return <Button onClick={authenticateUser}>Logg inn</Button>;
};

const eventMapToList = (
  events: Map<string, RemoteData<IEvent>>
): [string, IEvent][] =>
  [...events].flatMap(([id, event]) =>
    hasLoaded(event) ? [[id, event.data]] : []
  );

const sortEvents = (events: [string, IEvent][]) => {
  return events.sort(([idA, a], [idB, b]) =>
    isInOrder({ first: a.start, last: b.start }) ? 1 : -1
  );
};
