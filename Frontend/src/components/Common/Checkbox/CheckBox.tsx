import React, { ChangeEvent } from 'react';
import style from './CheckBox.module.scss';
import classNames from 'classnames';

interface IProps {
  onChange: (isChecked: boolean) => void;
  onDarkBackground?: boolean;
  isChecked: boolean;
  isDisabled?: boolean;
  label: string;
}

export const CheckBox = ({
  onChange,
  onDarkBackground,
  isChecked,
  isDisabled = false,
  label,
}: IProps) => {
  const labelStyle = classNames(style.checkboxLabel, {
    [style.checkboxLabelOnDarkBG]: onDarkBackground,
    [style.checkBoxDisabled]: isDisabled,
  });
  const checkboxStyle = classNames(style.checkbox, {
    [style.checkBoxOnDarkBG]: onDarkBackground,
    [style.checkBoxDisabled]: isDisabled,
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
        disabled={isDisabled}
      />
      {label}
    </label>
  );
};
