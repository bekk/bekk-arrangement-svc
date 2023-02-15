import React, {useState} from 'react';
import style from './ViewEventsCards.module.scss';
import {createRoute} from 'src/routing';
import {hasLoaded, RemoteData} from 'src/remote-data';
import {Page} from 'src/components/Page/Page';
import {useFilteredEvents} from 'src/hooks/cache';
import {EventCardElement} from 'src/components/ViewEventsCards/EventCardElement';
import {Button} from 'src/components/Common/Button/Button';
import {useHistory} from 'react-router';
import {authenticateUser, isAuthenticated} from 'src/auth';
import {WavySubHeader} from 'src/components/Common/Header/WavySubHeader';
import {IEvent} from 'src/types/event';
import {isInOrder, isInTheFuture, isInThePast} from 'src/types/date-time';

import {useSetTitle} from "src/hooks/setTitle";
import {appTitle} from "src/Constants";
import {Filter} from "../Common/Filter/Filter";
import {dateToIDate} from "../../types/date";
import {useSavedEditableEvents, useSavedParticipations} from "../../hooks/saved-tokens";

export type FilterOptions = {
    oslo: boolean
    trondheim: boolean
    alle: boolean
    kommende: boolean
    tidligere: boolean
    mine: boolean
    apent: boolean
    lukket: boolean
}

const initialFilterOptions = {
    oslo: false,
    trondheim: false,
    alle: false,
    kommende: false,
    tidligere: false,
    mine: false,
    apent: false,
    lukket: false,
}

export const ViewEventsCardsContainer = () => {
    const [filterOptions, setFilterOptions] = useState(initialFilterOptions);
    useSetTitle(appTitle)
    const savedEditableEvents = useSavedEditableEvents();
    const savedParticipations = useSavedParticipations();

    const filteredEvents = sortEvents(useFilteredEvents(filterOptions)).filter(event =>
        (
            // ( (!filterOptions.tidligere && !filterOptions.kommende && !filterOptions.mine) || (filterOptions.tidligere && filterTidligere(event[1])) || (filterOptions.kommende && filterKommende(event[1])) || (filterOptions.mine && filterMine(event[0], savedEditableEvents, savedParticipations)) ) &&
            filterType(filterOptions, event, savedEditableEvents, savedEditableEvents) &&
                filterAccess(filterOptions, event) &&
                filterOffice(filterOptions, event)
            //( (!filterOptions.apent && !filterOptions.lukket) || (filterOptions.apent && filterApent(event[1])) || (filterOptions.lukket && filterLukket(event[1])) ) &&
            // ( (!filterOptions.oslo && !filterOptions.trondheim && !filterOptions.alle) || (filterOptions.oslo && filterOslo(event[1])) || (filterOptions.trondheim && filterTrondheim(event[1])) || (filterOptions.alle && filterAlle(event[1])))
        )
    )

    return (
        <>
            <WavySubHeader eventId={'all-events'}>
                <div role="heading" aria-level={3} className={style.header}>
                    <h1 className={style.headerText}>Hva skjer i Bekk?</h1>
                    <AddEventButton/>
                </div>
            </WavySubHeader>
            <Page>
                <div className={style.headerContainer}>
                    <Filter setFilterState={setFilterOptions}/>
                </div>
                <div className={style.grid}>
                    {filteredEvents.map(([id, event]) => (
                        <EventCardElement key={id} eventId={id} event={event}/>
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

const sortEvents = (events: Map<string, RemoteData<IEvent>>) => {
    const eventList: [string, IEvent][] = [...events].flatMap(([id, event]) =>
        hasLoaded(event) ? [[id, event.data]] : []
    );
    return [...eventList].sort(([idA, a], [idB, b]) =>
        isInOrder({first: a.start, last: b.start}) ? -1 : 1
    );
};

const filterType = (filterOptions: FilterOptions, event: [string, IEvent], savedEditableEvents:any, savedParticipations: any) =>
    ( (!filterOptions.tidligere && !filterOptions.kommende && !filterOptions.mine) || (filterOptions.tidligere && filterTidligere(event[1])) || (filterOptions.kommende && filterKommende(event[1])) || (filterOptions.mine && filterMine(event[0], savedEditableEvents, savedParticipations)) ) ;

const filterAccess = (filterOptions: FilterOptions, event: [string, IEvent]) =>
    ( (!filterOptions.apent && !filterOptions.lukket) || (filterOptions.apent && filterApent(event[1])) || (filterOptions.lukket && filterLukket(event[1])) )

const filterOffice = (filterOptions: FilterOptions, event: [string, IEvent]) =>
    ( (!filterOptions.oslo && !filterOptions.trondheim && !filterOptions.alle) || (filterOptions.oslo && filterOslo(event[1])) || (filterOptions.trondheim && filterTrondheim(event[1])) || (filterOptions.alle && filterAlle(event[1])))

const filterOslo = (event: IEvent) => event.office === "Oslo"
const filterTrondheim = (event: IEvent) => event.office === "Trondheim"
const filterAlle = (event: IEvent) => event.office === "Alle"
const filterKommende = (event: IEvent) => {
    return isInTheFuture(event.start)
}
const filterTidligere = (event: IEvent) => {
    return isInThePast(event.start)
}
const filterMine = (id: string, savedEditableEvents: any, savedParticipations: any) => {
    return savedEditableEvents.savedEvents.map((x: any) => x.eventId).includes(id) || savedParticipations.savedParticipations.map((x: any) => x.eventId).includes(id)
}
const filterApent = (event: IEvent) => event.isExternal
const filterLukket = (event: IEvent) => !event.isExternal