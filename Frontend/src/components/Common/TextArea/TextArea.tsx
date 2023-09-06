import React, { useState } from 'react';
import style from './TextArea.module.scss';
import classNames from 'classnames';

interface IProps {
  label?: string;
  value: string;
  onChange: (value: string) => void;
  placeholder?: string;
  isError?: boolean;
  onBlur?: () => void;
  onLightBackground?: boolean;
  minRow?: number;
  className?: string;
}

export const TextArea = ({
  label,
  placeholder = '',
  value,
  onChange,
  isError = false,
  onBlur = () => undefined,
  onLightBackground = false,
  className = '',
}: IProps): JSX.Element => {
  const [hasVisited, setVisited] = useState(false);
  const inputStyle = classNames(style.textArea, className, {
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
      {label && (
        <label className={labelStyle} htmlFor={label}>
          {label}
        </label>
      )}
      <textarea
        className={inputStyle}
        id={label}
        placeholder={placeholder}
        value={value}
        onChange={(v) => onChange(v.target.value)}
        onBlur={blur}
      />
    </>
  );
};
