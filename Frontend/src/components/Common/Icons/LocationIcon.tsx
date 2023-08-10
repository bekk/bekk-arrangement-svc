import React from 'react';

interface IconProps {
  className?: string;
}

export const LocationIcon = ({ className }: IconProps) => {
  return (
    <svg
      className={className}
      width="12"
      height="18"
      viewBox="0 0 12 18"
      fill="none"
      xmlns="http://www.w3.org/2000/svg">
      <g>
        <path
          d="M11.3008 6.79031C11.3008 10.3704 5.79845 17.3 5.79845 17.3C5.79845 17.3 0.300781 10.3704 0.300781 6.79031C0.300781 3.20469 2.76332 0.299988 5.79845 0.299988C8.83824 0.299988 11.3008 3.20469 11.3008 6.79031Z"
          stroke="currentColor"
          strokeWidth="0.5"
          strokeMiterlimit="10"
          strokeLinecap="round"
          strokeLinejoin="round"
        />
        <path
          d="M7.80961 6.10589C7.80961 7.26751 6.86713 8.21 5.70372 8.21C4.54209 8.21 3.59961 7.26751 3.59961 6.10589C3.59961 4.94248 4.54209 4 5.70372 4C6.86713 4 7.80961 4.94248 7.80961 6.10589Z"
          stroke="currentColor"
          strokeWidth="0.5"
          strokeMiterlimit="10"
          strokeLinecap="round"
          strokeLinejoin="round"
        />
      </g>
      <defs>
        <rect width="11.75" height="18" fill="white" />
      </defs>
    </svg>
  );
};
