import React from 'react';

interface IconProps {
  className?: string;
}

export const OfficeIconBig = ({ className }: IconProps) => {
  return (
    <svg
      className={className}
      width="17"
      height="22"
      viewBox="0 0 17 22"
      fill="none"
      xmlns="http://www.w3.org/2000/svg">
      <path d="M12.5828 20H1V1H14V13.5" stroke="currentColor" />
      <path
        d="M11.5 4H9V6.5H11.5V4Z"
        fill="currentColor"
        stroke="currentColor"
        strokeWidth="0.5"
      />
      <path
        d="M6 4H3.5V6.5H6V4Z"
        fill="currentColor"
        stroke="currentColor"
        strokeWidth="0.5"
      />
      <path
        d="M6 9H3.5V11.5H6V9Z"
        fill="currentColor"
        stroke="currentColor"
        strokeWidth="0.5"
      />
      <path
        d="M11.5 9H9V11.5H11.5V9Z"
        fill="currentColor"
        stroke="currentColor"
        strokeWidth="0.5"
      />
      <path d="M9 14H6V20H9V14Z" stroke="currentColor" strokeWidth="0.5" />
      <path
        d="M13.6659 13.5C12.1936 13.5 11 14.6912 11 16.1586C11 17.7551 13.6659 21.5 13.6659 21.5C13.6659 21.5 16.3333 17.7146 16.3333 16.1586C16.3333 14.6912 15.1398 13.5 13.6659 13.5ZM13.6659 16.8543C13.1152 16.8543 12.6681 16.4071 12.6681 15.8583C12.6681 15.3094 13.1152 14.8622 13.6659 14.8622C14.2182 14.8622 14.6652 15.3094 14.6652 15.8583C14.6652 16.4071 14.2182 16.8543 13.6659 16.8543Z"
        stroke="currentColor"
        strokeWidth="0.5"
        strokeMiterlimit="10"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
      <circle cx="13.5" cy="16" r="1" fill="currentColor" />
    </svg>
  );
};
