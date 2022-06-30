import classNames from 'classnames';
import React, { useCallback, useState } from 'react';
import { Arrow } from 'src/components/Common/Arrow/Arrow';
import { useOnClickOutside } from 'src/hooks/eventListener';
import style from './Dropdown.module.scss';

export interface IDropdownItem<T> {
  name: string;
  id: T;
}

interface IProps<T> {
  items: IDropdownItem<T>[];
  onChange(id: T): void;
  selectedId: T;
}

export const Dropdown = <T extends number | string>({
  items,
  onChange,
  selectedId,
}: IProps<T>) => {
  const [dropdownOpen, setDropdownOpen] = useState(false);
  const selectedItem = items.find((x) => x.id === selectedId);

  const closeWhenClickOutsideRef = useOnClickOutside<HTMLDivElement>(
    useCallback(() => {
      setDropdownOpen(false);
    }, [])
  );

  const handleSelect = (id: T) => {
    setDropdownOpen(false);
    onChange(id);
  };

  const selectedElementStyle = classNames(style.selectedElement, {
    [style.selectedElementOpen]: dropdownOpen,
  });

  const arrowStyle = classNames(style.arrow, {
    [style.upSideDown]: dropdownOpen,
  });

  const dropdownElementStyle = (element: IDropdownItem<T>) =>
    classNames(style.dropdownElement, {
      [style.dropdownElementSelected]: selectedItem === element,
      [style.lastElement]: element === items[items.length - 1],
    });

  return (
    <div
      className={style.container}
      ref={closeWhenClickOutsideRef}
      onClick={() => setDropdownOpen(!dropdownOpen)}
    >
      <div className={selectedElementStyle}>
        <p className={style.text}>{selectedItem?.name}</p>
        <Arrow className={arrowStyle} direction="down" color="white" noCircle />
      </div>
      {dropdownOpen && (
        <div className={style.elementsWrapper}>
          {items.map((x) => (
            <div
              className={dropdownElementStyle(x)}
              key={x.id}
              onClick={() => handleSelect(x.id)}
            >
              <p className={style.elementText}>{x.name}</p>
            </div>
          ))}
        </div>
      )}
    </div>
  );
};
