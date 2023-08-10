import React from 'react';
interface IconProps {
  className?: string;
}
export const ExternalIcon = ({ className }: IconProps) => {
  return (
    <svg
      className={className}
      width="22"
      height="22"
      viewBox="0 0 22 22"
      fill="none"
      xmlns="http://www.w3.org/2000/svg">
      <path
        d="M1 1L11.1641 4V18.7778M1 1V18.7778L11.1641 21V18.7778M1 1H16.7895V6M11.1641 18.7778H16.7895V14.8889"
        stroke="currentColor"
      />
      <path
        d="M13.6309 10.5001H20.9993M20.9993 10.5001L18.1653 8.00012M20.9993 10.5001L18.1653 13.0001"
        stroke="currentColor"
      />
    </svg>
  );
};
