import React from 'react';
interface IconProps {
  className?: string;
}
export const ExternalIcon = ({ className }: IconProps) => {
  return (
    <svg
      className={className}
      width="19"
      height="19"
      viewBox="0 0 19 19"
      fill="none"
      xmlns="http://www.w3.org/2000/svg">
      <path
        d="M1 1L9.63945 3.55V16.1111M1 1V16.1111L9.63945 18V16.1111M1 1H14.4211V5.25M9.63945 16.1111H14.4211V12.8056"
        stroke="currentColor"
        strokeWidth="0.5"
      />
      <path
        d="M11.7363 9.07507H17.9995M17.9995 9.07507L15.5906 6.95007M17.9995 9.07507L15.5906 11.2001"
        stroke="currentColor"
        strokeWidth="0.5"
      />
    </svg>
  );
};
