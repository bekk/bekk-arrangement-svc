import React from 'react';
import { EditTime } from 'src/types/time';
import style from './TimeInput.module.scss';
import { useUpdateableInitialValue } from 'src/hooks/utils';

interface IProps {
  value: EditTime;
  onChange: (value: EditTime) => void;
  label?: string;
}

export const TimeInput = ({
  value: [initialHour, initialMinute],
  onChange,
  label,
}: IProps) => {
  const [hour, setHour] = useUpdateableInitialValue(initialHour);
  const [minute, setMinute] = useUpdateableInitialValue(initialMinute);

  const updateTime = () => {
    onChange([hour, minute]);
  };

  const selectText = (e: React.FocusEvent<HTMLInputElement>) => {
    e.target.select();
  };

  return (
    <>
      <label className={style.textLabel} htmlFor={label}>
        {label}
      </label>
      <div className={style.timeInputContainer}>
        <input
          className={style.timeInput}
          type="number"
          inputMode="numeric"
          value={hour}
          onChange={(v) => setHour(v.target.value)}
          onBlur={updateTime}
          onFocus={selectText}
        />
        <div className={style.text}>:</div>
        <input
          className={style.timeInput}
          type="number"
          inputMode="numeric"
          onBlur={updateTime}
          value={minute}
          onChange={(v) => setMinute(v.target.value)}
          onFocus={selectText}
        />
      </div>
    </>
  );
};
