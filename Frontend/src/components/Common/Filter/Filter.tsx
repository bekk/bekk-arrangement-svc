import React, { useEffect, useState } from 'react';
import style from './Filter.module.scss';
import { FilterIcon } from '../Icons/FilterIcon';
import classNames from 'classnames';
import { CheckBox } from '../Checkbox/CheckBox';
import { IEvent } from '../../../types/event';
import { isInTheFuture, isInThePast } from '../../../types/date-time';
import { FilterOptions } from '../../ViewEventsCards/ViewEventsCardsContainer';
import { EditEventToken, Participation } from '../../../hooks/saved-tokens';
import { useUrlState } from '../../../hooks/useUrlState';

type UpdateBoolFunction = (state: boolean) => void;
type SetFilterState = (filterOptions: FilterOptions) => void;

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

const serializeBool = (bool: boolean) => (bool ? '1' : '0');
const deserializeBool = (string: string) => string === '1';

export const Filter = ({
  setFilterState,
}: {
  setFilterState: SetFilterState;
}) => {
  const [showFilterOptions, setShowFilterOptions] = useState(false);

  const [oslo, setOslo] = useUrlState<boolean>(
    false,
    'Oslo',
    serializeBool,
    deserializeBool
  );
  const [trondheim, setTrondheim] = useUrlState<boolean>(
    false,
    'trondheim',
    serializeBool,
    deserializeBool
  );
  const [alle, setAlle] = useUrlState<boolean>(
    false,
    'alle',
    serializeBool,
    deserializeBool
  );

  const officeData: OfficeType = {
    oslo: [oslo, setOslo],
    trondheim: [trondheim, setTrondheim],
    alle: [alle, setAlle],
  };

  const [kommende, setKommende] = useUrlState<boolean>(
    true,
    'kommende',
    serializeBool,
    deserializeBool
  );
  const [tidligere, setTidligere] = useUrlState<boolean>(
    false,
    'tidligere',
    serializeBool,
    deserializeBool
  );
  const [mine, setMine] = useUrlState<boolean>(
    false,
    'mine',
    serializeBool,
    deserializeBool
  );
  const [apent, setApent] = useUrlState<boolean>(
    false,
    'apent',
    serializeBool,
    deserializeBool
  );
  const [lukket, setLukket] = useUrlState<boolean>(
    false,
    'lukket',
    serializeBool,
    deserializeBool
  );

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

  const filterStyles = classNames(style.filter, {
    [style.filterOpen]: showFilterOptions,
  });

  return (
    <div className={style.container}>
      <button
        className={style.buttonReset}
        onClick={() => setShowFilterOptions(!showFilterOptions)}>
        <FilterIcon className={filterStyles} />
      </button>
      {showFilterOptions && (
        <div className={style.filters}>
          <Type typeData={typeData} />
          <Office kontorData={officeData} />
        </div>
      )}
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
