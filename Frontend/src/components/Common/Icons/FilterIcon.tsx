import React from 'react';

export const FilterIcon = (props: { className?: string }) => {
  return (
    <svg
      fill="none"
      version="1.1"
      viewBox="0 0 23 23"
      xmlns="http://www.w3.org/2000/svg"
      className={props.className}>
      <path d="m19 7h-14" stroke="white" />
      <rect x=".5" y=".5" width="22" height="22" stroke="white" />
      <circle
        id="circleLeft"
        cx="8.5"
        cy="7"
        r="2"
        fill="orangered"
        stroke="white"
      />
      <path d="m19 15h-14" stroke="white" />
      <circle
        id="cirleRight"
        cx="15.426"
        cy="15"
        r="2"
        fill="orangered"
        stroke="white"
      />
    </svg>
  );
};
