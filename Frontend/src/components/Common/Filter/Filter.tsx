import React, { useEffect, useState } from 'react';
import style from './Filter.module.scss';
import { FilterIcon } from '../Icons/FilterIcon';
import classNames from 'classnames';
import { CheckBox } from '../Checkbox/CheckBox';
import { useUrlBoolState } from '../../../hooks/useUrlBoolState';
import { FilterOptions } from '../../ViewEventsCards/ViewEventsCardsContainer';

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

type SetFilterState = (filterOptions: FilterOptions) => void;

export const Filter = ({
  setFilterState,
}: {
  setFilterState: SetFilterState;
}) => {
  const [showFilterOptions, setShowFilterOptions] = useState(false);

  const [oslo, setOslo] = useUrlBoolState(false, 'Oslo');
  const [trondheim, setTrondheim] = useUrlBoolState(false, 'trondheim');
  const [alle, setAlle] = useUrlBoolState(false, 'alle');

  const officeData: OfficeType = {
    oslo: [oslo, setOslo],
    trondheim: [trondheim, setTrondheim],
    alle: [alle, setAlle],
  };

  const [kommende, setKommende] = useUrlBoolState(true, 'kommende');
  const [tidligere, setTidligere] = useUrlBoolState(false, 'tidligere');
  const [mine, setMine] = useUrlBoolState(false, 'mine');
  const [apent, setApent] = useUrlBoolState(false, 'apent');
  const [lukket, setLukket] = useUrlBoolState(false, 'lukket');

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
