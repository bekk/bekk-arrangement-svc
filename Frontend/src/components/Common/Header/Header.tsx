import React from 'react';
import logo from 'src/images/logo.svg';
import style from './Header.module.scss';
import { eventsRoute, useIsCreateRoute, useIsEditingRoute } from 'src/routing';
import { Link } from 'react-router-dom';

export const Header = () => {
  const shouldHideHeader = useShouldHideHeader();

  if (shouldHideHeader) return null;

  return (
    <div className={style.header}>
      <Link to={eventsRoute}>
        <img className={style.logo} src={logo} alt="logo" />
      </Link>
    </div>
  );
};

export const useShouldHideHeader = () => {
  const isEditingRoute = useIsEditingRoute();
  const isCreateRoute = useIsCreateRoute();
  return isEditingRoute || isCreateRoute;
};
