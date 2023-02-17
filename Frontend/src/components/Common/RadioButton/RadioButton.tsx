import React from 'react';
import style from './RadioButton.module.scss';

interface IProps {
  onChange: (isChecked: boolean) => void;
  checked: boolean;
  label: string;
}

export const RadioButton = ({ onChange, checked, label }: IProps) => {
  return (
    <label className={style.radioButtonLabel}>
      <input
        className={style.radioButton}
        type="radio"
        onChange={(e: any) => onChange(e.target.checked)}
        checked={checked}
      />
      {label}
    </label>
  );
};
