import React, { useEffect } from 'react';
import style from './Filter.module.scss';
import { FilterIcon } from '../Icons/FilterIcon';
import classNames from 'classnames';
import { CheckBox } from '../Checkbox/CheckBox';
import { IEvent } from 'src/types/event';
import { isInTheFuture, isInThePast } from 'src/types/date-time';
import { EditEventToken, Participation } from 'src/hooks/saved-tokens';
import { useUrlState } from 'src/hooks/useUrlState';

export type FilterOptions = {
  oslo: boolean;
  trondheim: boolean;
  kommende: boolean;
  tidligere: boolean;
  mine: boolean;
  eksternt: boolean;
  internt: boolean;
};

type UpdateBoolFunction = (state: boolean) => void;

type OfficeType = {
  oslo: [boolean, UpdateBoolFunction];
  trondheim: [boolean, UpdateBoolFunction];
};

type TypeData = {
  kommende: [boolean, UpdateBoolFunction];
  tidligere: [boolean, UpdateBoolFunction];
  mine: [boolean, UpdateBoolFunction];
  eksternt: [boolean, UpdateBoolFunction];
  internt: [boolean, UpdateBoolFunction];
};

export const Filter = ({
  filterState,
  setFilterState,
}: {
  filterState: FilterOptions;
  setFilterState: (filterOptions: FilterOptions) => void;
}) => {
  const [showFilterOptions, setShowFilterOptions] = useUrlBoolState(
    'filter',
    false
  );

  const [oslo, setOslo] = useUrlBoolState('Oslo', filterState.oslo);
  const [trondheim, setTrondheim] = useUrlBoolState(
    'trondheim',
    filterState.trondheim
  );

  const officeData: OfficeType = {
    oslo: [oslo, setOslo],
    trondheim: [trondheim, setTrondheim],
  };

  const [kommende, setKommende] = useUrlBoolState(
    'kommende',
    filterState.kommende
  );
  const [tidligere, setTidligere] = useUrlBoolState(
    'tidligere',
    filterState.tidligere
  );
  const [mine, setMine] = useUrlBoolState('mine', filterState.mine);
  const [eksternt, setEksternt] = useUrlBoolState(
    'eksternt',
    filterState.eksternt
  );
  const [internt, setInternt] = useUrlBoolState('internt', filterState.internt);

  useEffect(
    () =>
      setFilterState({
        oslo,
        trondheim,
        kommende,
        tidligere,
        mine,
        eksternt,
        internt,
      }),
    [oslo, trondheim, kommende, tidligere, mine, eksternt, internt]
  );

  const typeData: TypeData = {
    kommende: [kommende, setKommende],
    tidligere: [tidligere, setTidligere],
    mine: [mine, setMine],
    eksternt: [eksternt, setEksternt],
    internt: [internt, setInternt],
  };

  const filterIconStyles = classNames(style.filter, {
    [style.filterOpen]: showFilterOptions,
  });

  const filterStyles = classNames(style.filterOptions, {
    [style.filterOptionsOpen]: showFilterOptions,
  });

  return (
    <div className={style.container}>
      <button
        className={style.buttonReset}
        onClick={() => setShowFilterOptions(!showFilterOptions)}>
        <FilterIcon className={filterIconStyles} />
      </button>
      <div className={filterStyles}>
        <Type typeData={typeData} />
        <Office kontorData={officeData} />
      </div>
    </div>
  );
};

const Office = ({ kontorData }: { kontorData: OfficeType }) => {
  const [oslo, setOslo] = kontorData.oslo;
  const [trondheim, setTrondheim] = kontorData.trondheim;
  return (
    <div className={style.column}>
      <h3>Hvor</h3>
      <CheckBox
        onDarkBackground
        label="Oslo"
        isChecked={oslo}
        onChange={() => setOslo(!oslo)}
      />
      <CheckBox
        onDarkBackground
        label="Trondheim"
        isChecked={trondheim}
        onChange={() => setTrondheim(!trondheim)}
      />
    </div>
  );
};

const Type = ({ typeData }: { typeData: TypeData }) => {
  const [kommende, setKommende] = typeData.kommende;
  const [tidligere, setTidligere] = typeData.tidligere;
  const [mine, setMine] = typeData.mine;
  const [eksternt, setEksternt] = typeData.eksternt;
  const [internt, setInternt] = typeData.internt;
  return (
    <>
      <div className={style.column}>
        <h3>Type arrangementer</h3>
        <CheckBox
          onDarkBackground
          label="Kommende"
          isChecked={kommende}
          onChange={() => setKommende(!kommende)}
        />
        <CheckBox
          onDarkBackground
          label="Tidligere"
          isChecked={tidligere}
          onChange={() => setTidligere(!tidligere)}
        />
        <CheckBox
          onDarkBackground
          label="Kun mine"
          isChecked={mine}
          onChange={() => setMine(!mine)}
        />
      </div>
      <div className={style.column}>
        <h3>For hvem</h3>
        <CheckBox
          onDarkBackground
          label="Eksterne"
          isChecked={eksternt}
          onChange={() => setEksternt(!eksternt)}
        />
        <CheckBox
          onDarkBackground
          label="Interne"
          isChecked={internt}
          onChange={() => setInternt(!internt)}
        />
      </div>
    </>
  );
};

export const filterType = (
  filterOptions: FilterOptions,
  event: [string, IEvent]
) =>
  // Dersom ingenting er valgt, vis alt
  (!filterOptions.tidligere && !filterOptions.kommende) ||
  // Vis det som er valgt
  (filterOptions.tidligere && filterTidligere(event[1])) ||
  (filterOptions.kommende && filterKommende(event[1]));

export const filterKunMine = (
  filterOptions: FilterOptions,
  event: [string, IEvent],
  savedEvents: EditEventToken[],
  savedParticipations: Participation[]
) =>
  !filterOptions.mine ||
  (filterOptions.mine &&
    filterMine(event[0], savedEvents, savedParticipations));

export const filterAccess = (
  filterOptions: FilterOptions,
  event: [string, IEvent]
) =>
  // Dersom ingenting er valgt, vis alt
  (!filterOptions.eksternt && !filterOptions.internt) ||
  // Vis det som er valgt
  (filterOptions.eksternt && filterEksternt(event[1])) ||
  (filterOptions.internt && filterInternt(event[1]));

export const filterOffice = (
  filterOptions: FilterOptions,
  event: [string, IEvent]
) =>
  // Dersom ingenting er valgt, vis alt
  (!filterOptions.oslo && !filterOptions.trondheim) ||
  // Vis det som er valgt
  (filterOptions.oslo && filterOslo(event[1])) ||
  (filterOptions.trondheim && filterTrondheim(event[1])) ||
  event[1].offices === undefined;

const filterOslo = (event: IEvent) => event.offices?.Oslo;
const filterTrondheim = (event: IEvent) => event.offices?.Trondheim;
const filterKommende = (event: IEvent) => {
  return isInTheFuture(event.start);
};
const filterTidligere = (event: IEvent) => {
  return isInThePast(event.start);
};
const filterMine = (
  id: string,
  savedEvents: EditEventToken[],
  savedParticipations: Participation[]
) =>
  savedEvents.map((x: EditEventToken) => x.eventId).includes(id) ||
  savedParticipations.map((x: Participation) => x.eventId).includes(id);

const filterEksternt = (event: IEvent) => event.isExternal;
const filterInternt = (event: IEvent) => !event.isExternal;
const serializeBool = (bool: boolean) => (bool ? '1' : '0');
const deserializeBool = (string: string) => string === '1';
const useUrlBoolState = (key: string, value: boolean) =>
  useUrlState(key, value, serializeBool, deserializeBool);
