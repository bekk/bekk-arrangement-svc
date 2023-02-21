import React, { useEffect, useState } from 'react';
import style from './Filter.module.scss';
import { FilterIcon } from '../Icons/FilterIcon';
import classNames from 'classnames';
import { CheckBox } from '../Checkbox/CheckBox';
import { IEvent } from '../../../types/event';
import { isInTheFuture, isInThePast } from '../../../types/date-time';
import { EditEventToken, Participation } from '../../../hooks/saved-tokens';
import { useUrlState } from '../../../hooks/useUrlState';

export type FilterOptions = {
  oslo: boolean;
  trondheim: boolean;
  alle: boolean;
  kommende: boolean;
  tidligere: boolean;
  mine: boolean;
  apent: boolean;
  lukket: boolean;
};

type UpdateBoolFunction = (state: boolean) => void;

type OfficeType = {
  oslo: [boolean, UpdateBoolFunction];
  trondheim: [boolean, UpdateBoolFunction];
  alle: [boolean, UpdateBoolFunction];
};

type TypeData = {
  kommende: [boolean, UpdateBoolFunction];
  tidligere: [boolean, UpdateBoolFunction];
  mine: [boolean, UpdateBoolFunction];
  apent: [boolean, UpdateBoolFunction];
  lukket: [boolean, UpdateBoolFunction];
};

export const Filter = ({
  filterState,
  setFilterState,
}: {
  filterState: FilterOptions;
  setFilterState: (filterOptions: FilterOptions) => void;
}) => {
  const [showFilterOptions, setShowFilterOptions] = useState(false);

  const [oslo, setOslo] = useUrlBoolState('Oslo', filterState.oslo);
  const [trondheim, setTrondheim] = useUrlBoolState(
    'trondheim',
    filterState.trondheim
  );
  const [alle, setAlle] = useUrlBoolState('alle', filterState.alle);

  const officeData: OfficeType = {
    oslo: [oslo, setOslo],
    trondheim: [trondheim, setTrondheim],
    alle: [alle, setAlle],
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
  const [apent, setApent] = useUrlBoolState('apent', filterState.apent);
  const [lukket, setLukket] = useUrlBoolState('lukket', filterState.lukket);

  useEffect(
    () =>
      setFilterState({
        oslo,
        trondheim,
        alle,
        kommende,
        tidligere,
        mine,
        apent,
        lukket,
      }),
    [oslo, trondheim, alle, kommende, tidligere, mine, apent, lukket]
  );

  const typeData: TypeData = {
    kommende: [kommende, setKommende],
    tidligere: [tidligere, setTidligere],
    mine: [mine, setMine],
    apent: [apent, setApent],
    lukket: [lukket, setLukket],
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
  const [alle, setAlle] = kontorData.alle;
  return (
    <div>
      <h3>Kontor</h3>
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
      <CheckBox
        onDarkBackground
        label="Alle"
        isChecked={alle}
        onChange={() => setAlle(!alle)}
      />
    </div>
  );
};

const Type = ({ typeData }: { typeData: TypeData }) => {
  const [kommende, setKommende] = typeData.kommende;
  const [tidligere, setTidligere] = typeData.tidligere;
  const [mine, setMine] = typeData.mine;
  const [apent, setApent] = typeData.apent;
  const [lukket, setLukket] = typeData.lukket;
  return (
    <div>
      <h3>Type</h3>
      <div className={style.typeContainer}>
        <div>
          <CheckBox
            onDarkBackground
            label="Kommende arrangementer"
            isChecked={kommende}
            onChange={() => setKommende(!kommende)}
          />
          <CheckBox
            onDarkBackground
            label="Tidligere arrangementer"
            isChecked={tidligere}
            onChange={() => setTidligere(!tidligere)}
          />
          <CheckBox
            onDarkBackground
            label="Mine arrangementer"
            isChecked={mine}
            onChange={() => setMine(!mine)}
          />
        </div>
        <div>
          <CheckBox
            onDarkBackground
            label="Åpent arrangement"
            isChecked={apent}
            onChange={() => setApent(!apent)}
          />
          <CheckBox
            onDarkBackground
            label="Lukket arrangement"
            isChecked={lukket}
            onChange={() => setLukket(!lukket)}
          />
        </div>
      </div>
    </div>
  );
};

export const filterType = (
  filterOptions: FilterOptions,
  event: [string, IEvent],
  savedEvents: EditEventToken[],
  savedParticipations: Participation[]
) =>
  // Dersom ingenting er valgt, vis alt
  (!filterOptions.tidligere &&
    !filterOptions.kommende &&
    !filterOptions.mine) ||
  // Vis det som er valgt
  (filterOptions.tidligere && filterTidligere(event[1])) ||
  (filterOptions.kommende && filterKommende(event[1])) ||
  (filterOptions.mine &&
    filterMine(event[0], savedEvents, savedParticipations));

export const filterAccess = (
  filterOptions: FilterOptions,
  event: [string, IEvent]
) =>
  // Dersom ingenting er valgt, vis alt
  (!filterOptions.apent && !filterOptions.lukket) ||
  // Vis det som er valgt
  (filterOptions.apent && filterApent(event[1])) ||
  (filterOptions.lukket && filterLukket(event[1]));

export const filterOffice = (
  filterOptions: FilterOptions,
  event: [string, IEvent]
) =>
  // Dersom ingenting er valgt, vis alt
  (!filterOptions.oslo && !filterOptions.trondheim && !filterOptions.alle) ||
  // Vis det som er valgt
  (filterOptions.oslo && filterOslo(event[1])) ||
  (filterOptions.trondheim && filterTrondheim(event[1])) ||
  (filterOptions.alle && filterAlle(event[1]));

const filterOslo = (event: IEvent) => event.office === 'Oslo';
const filterTrondheim = (event: IEvent) => event.office === 'Trondheim';
const filterAlle = (event: IEvent) => event.office === 'Alle';
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
  savedEvents.map((x: any) => x.eventId).includes(id) ||
  savedParticipations.map((x: any) => x.eventId).includes(id);

const filterApent = (event: IEvent) => event.isExternal;
const filterLukket = (event: IEvent) => !event.isExternal;
const serializeBool = (bool: boolean) => (bool ? '1' : '0');
const deserializeBool = (string: string) => string === '1';
const useUrlBoolState = (key: string, value: boolean) =>
  useUrlState(key, value, serializeBool, deserializeBool);
