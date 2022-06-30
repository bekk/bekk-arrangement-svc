import React, { useState } from 'react';
import style from './TextInput.module.scss';
import classNames from 'classnames';

interface IProps {
  label: string;
  value: string;
  placeholder?: string;
  isNumber?: boolean;
  onChange: (value: string) => void;
  isError?: boolean;
  onBlur?: () => void;
  onLightBackground?: boolean;
}

export const TextInput = ({
  label,
  value,
  onChange,
  placeholder = '',
  isNumber = false,
  isError = false,
  onBlur = () => undefined,
  onLightBackground = false,
}: IProps): JSX.Element => {
  const [hasVisited, setVisited] = useState(false);
  const inputStyle = classNames(style.textInput, {
    [style.visited]: hasVisited,
    [style.error]: hasVisited && isError,
    [style.onLightBackground]: onLightBackground,
    [style.onDarkBackground]: !onLightBackground,
  });
  const labelStyle = classNames(style.textLabel, {
    [style.textLabelLightBackground]: onLightBackground,
  });

  const blur = () => {
    onBlur();
    setVisited(true);
  };

  return (
    <>
      <label className={labelStyle} htmlFor={label}>
        {label}
      </label>
      <input
        id={label}
        type={isNumber ? 'number' : 'text'}
        className={inputStyle}
        placeholder={placeholder}
        value={value}
        onChange={(v) => onChange(v.target.value)}
        onBlur={blur}
      />
    </>
  );
};
