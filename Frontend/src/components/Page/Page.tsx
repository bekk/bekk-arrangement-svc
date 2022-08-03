import React from 'react';
import style from './Page.module.scss';

type ReactChild = JSX.Element | JSX.Element[] | false | null | undefined;

interface Props {
  children: ReactChild | ReactChild[];
}

export const Page = ({ children }: Props) => (
  <div role="main" className={style.content}>{children}</div>
);
