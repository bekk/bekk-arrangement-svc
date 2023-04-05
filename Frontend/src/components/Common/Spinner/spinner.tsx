import React from 'react';
import style from './spinner.module.scss';

export const Spinner = () => {
  return (
    <div className={style.spinnerContainer}>
      <div className={style.spinner} />
    </div>
  );
};

export const UnstyledSpinner = () => {
  return <div className={style.spinner} />;
};
