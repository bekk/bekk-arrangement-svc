import React, { ChangeEvent } from 'react';
import style from './CheckBox.module.scss';
import classNames from 'classnames';

interface IProps {
  onChange: (isChecked: boolean) => void;
  onDarkBackground?: boolean;
  isChecked: boolean;
  label: string;
}

export const CheckBox = ({
  onChange,
  onDarkBackground,
  isChecked,
  label,
}: IProps) => {
  const labelStyle = classNames(style.checkboxLabel, {
    [style.checkboxLabelOnDarkBG]: onDarkBackground,
  });
  const checkboxStyle = classNames(style.checkbox, {
    [style.checkBoxOnDarkBG]: onDarkBackground,
  });
  return (
    <label className={labelStyle}>
      <input
        className={checkboxStyle}
        type="checkbox"
        onChange={(e: ChangeEvent<HTMLInputElement>) =>
          onChange(e.target.checked)
        }
        checked={isChecked}
      />
      {label}
    </label>
  );
};
