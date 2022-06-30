import React from 'react';
import logo from 'src/images/logo.svg';
import style from './Header.module.scss';
import {
  eventsRoute,
  useIsCreateRoute,
  useIsEditingRoute,
  useIsPreviewRoute,
} from 'src/routing';
import { Link } from 'react-router-dom';
import classNames from 'classnames';

export const Header = () => {
  const shouldHaveBlackHeader = useShouldHaveBlackHeaderBackground();
  const headerStyle = classNames(style.header, {
    [style.coloredHeader]: !shouldHaveBlackHeader,
  });

  return (
    <div className={headerStyle}>
      <Link to={eventsRoute}>
        <img className={style.logo} src={logo} alt="logo" />
      </Link>
    </div>
  );
};

export const useShouldHaveBlackHeaderBackground = () => {
  let isEditingRoute = useIsEditingRoute();
  let isPreviewRoute = useIsPreviewRoute();
  let isCreateRoute = useIsCreateRoute();
  return isEditingRoute || isPreviewRoute || isCreateRoute;
};
