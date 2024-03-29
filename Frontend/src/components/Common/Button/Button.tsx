import React, { MouseEventHandler } from 'react';
import style from './Button.module.scss';
import classNames from 'classnames';
import { ReactChild } from 'src/types';

interface IProps {
  onClick: MouseEventHandler<HTMLButtonElement>;
  disabled?: boolean;
  color?: 'Primary' | 'Secondary';
  displayAsLink?: boolean;
  children?: ReactChild | ReactChild[];
  disabledReason?: string | JSX.Element;
  className?: string;
  onLightBackground?: boolean;
}
export const Button = ({
  onClick,
  color = 'Primary',
  disabled = false,
  displayAsLink = false,
  children,
  disabledReason,
  className,
  onLightBackground,
}: IProps) => {
  const buttonStyle = classNames(
    style.tooltipHover,
    {
      [style.button]: !displayAsLink,
      [style.secondaryButton]: color === 'Secondary' && !displayAsLink,
      [style.primaryButton]: color === 'Primary' && !displayAsLink,
      [style.link]: displayAsLink,
      [style.darkLink]: displayAsLink && onLightBackground,
    },
    className
  );
  return (
    <button className={buttonStyle} onClick={onClick} disabled={disabled}>
      {disabled && disabledReason && (
        <div className={style.tooltip}>{disabledReason}</div>
      )}
      {children}
    </button>
  );
};
