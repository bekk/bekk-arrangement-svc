import React from 'react';
import style from './BackLink.module.scss';
import { Link, type LinkProps } from 'react-router-dom';
import classNames from 'classnames';

export const BackLink = ({ className, children, ...props }: LinkProps) => {
  return (
    <Link className={classNames(style.link, className)} {...props}>
      ← <span className={style.text}>{children}</span>
    </Link>
  );
};
