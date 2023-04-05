import React from 'react';

interface IconProps {
  color: 'white' | 'black';
  className?: string;
}

export const LocationIcon = ({ color, className }: IconProps) => {
  return (
    <svg
      width="18"
      height="26"
      viewBox="0 0 18 26"
      fill="none"
      xmlns="http://www.w3.org/2000/svg"
      className={className}>
      <path
        d="M8.99755 1C4.58072 1 1 4.57368 1 8.9758C1 13.7653 8.99755 25 8.99755 25C8.99755 25 17 13.6439 17 8.9758C17 4.57368 13.4193 1 8.99755 1ZM8.99755 11.0628C7.34552 11.0628 6.00444 9.72128 6.00444 8.07476C6.00444 6.42823 7.34552 5.08673 8.99755 5.08673C10.6545 5.08673 11.9956 6.42823 11.9956 8.07476C11.9956 9.72128 10.6545 11.0628 8.99755 11.0628Z"
        stroke="#0E0E0E"
        strokeMiterlimit="10"
        strokeLinecap="round"
        strokeLinejoin="round"
        fill={color}
      />
    </svg>
  );
};
