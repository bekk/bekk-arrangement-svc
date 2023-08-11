import React from 'react';

interface IconProps {
  className?: string;
}

export const LocationIconBig = ({ className }: IconProps) => {
  return (
    <svg
      className={className}
      width="15"
      height="20"
      viewBox="0 0 15 20"
      fill="none"
      xmlns="http://www.w3.org/2000/svg">
      <path
        d="M7.67149 0.769043C4.27393 0.769043 1.51953 3.51802 1.51953 6.90427C1.51953 10.5885 7.67149 19.2306 7.67149 19.2306C7.67149 19.2306 13.8272 10.4951 13.8272 6.90427C13.8272 3.51802 11.0728 0.769043 7.67149 0.769043ZM7.67149 8.50965C6.4007 8.50965 5.3691 7.47772 5.3691 6.21116C5.3691 4.9446 6.4007 3.91268 7.67149 3.91268C8.94605 3.91268 9.97766 4.9446 9.97766 6.21116C9.97766 7.47772 8.94605 8.50965 7.67149 8.50965Z"
        stroke="currentColor"
        strokeMiterlimit="10"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
    </svg>
  );
};
