import React, { MouseEventHandler } from 'react';
import style from './Button.module.scss';
import classNames from 'classnames';
import { ReactChild } from 'src/types';

interface IProps {
  onClick: MouseEventHandler<HTMLButtonElement>;
  disabled?: boolean;
  color?: 'Primary' | 'Secondary';
  displayAsLink?: boolean;
  children?: ReactChild;
  disabledResaon?: string | JSX.Element;
  className?: string;
  onLightBackground?: boolean;
}
export const Button = ({
  onClick,
  color = 'Primary',
  disabled = false,
  displayAsLink = false,
  children,
  disabledResaon,
  className,
  onLightBackground,
}: IProps) => {
  const buttonStyle = classNames(style.tooltipHover, className, {
    [style.button]: !displayAsLink,
    [style.secondaryButton]: color === 'Secondary' && !displayAsLink,
    [style.primaryButton]: color === 'Primary' && !displayAsLink,
    [style.link]: displayAsLink,
    [style.darkLink]: displayAsLink && onLightBackground,
  });
  return (
    <button className={buttonStyle} onClick={onClick} disabled={disabled}>
      {disabled && disabledResaon && (
        <div className={style.tooltip}>{disabledResaon}</div>
      )}
      {children}
    </button>
  );
};
