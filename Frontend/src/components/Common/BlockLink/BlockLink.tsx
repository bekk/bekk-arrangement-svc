import React from 'react';
import { Link } from 'react-router-dom';
import style from './BlockLink.module.scss';
import { ReactChild } from 'src/types';
import classNames from 'classnames';

interface IProps {
  to: string;
  children: ReactChild | ReactChild[];
  onLightBackground?: boolean;
}

export const BlockLink = ({ to, onLightBackground, children }: IProps) => {
  const linkStyle = classNames(style.link, {
    [style.darkText]: onLightBackground,
  });

  return (
    <Link to={to} className={linkStyle}>
      {children}
    </Link>
  );
};
