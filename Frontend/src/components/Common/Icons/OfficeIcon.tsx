import React from 'react';

interface IconProps {
  className?: string;
}

export const OfficeIcon = ({ className }: IconProps) => {
  return (
    <svg
      className={className}
      width="14"
      height="19"
      viewBox="0 0 14 19"
      fill="none"
      xmlns="http://www.w3.org/2000/svg">
      <path
        d="M11.7805 11.3687V1H1V16.7561L10.6823 16.7561"
        stroke="currentColor"
        strokeWidth="0.5"
      />
      <path
        d="M9.70794 3.48779H7.63477V5.56096H9.70794V3.48779Z"
        fill="currentColor"
        stroke="currentColor"
        strokeWidth="0.5"
      />
      <path
        d="M5.14544 3.48779H3.07227V5.56096H5.14544V3.48779Z"
        fill="currentColor"
        stroke="currentColor"
        strokeWidth="0.5"
      />
      <path
        d="M5.14544 7.63428H3.07227V9.70745H5.14544V7.63428Z"
        fill="currentColor"
        stroke="currentColor"
        strokeWidth="0.5"
      />
      <path
        d="M9.70794 7.63428H7.63477V9.70745H9.70794V7.63428Z"
        fill="currentColor"
        stroke="currentColor"
        strokeWidth="0.5"
      />
      <path
        d="M7.63429 11.7805H5.14648V16.7561H7.63429V11.7805Z"
        stroke="currentColor"
        strokeWidth="0.5"
      />
      <path
        d="M11.5037 11.3657C10.2828 11.3657 9.29297 12.3536 9.29297 13.5704C9.29297 14.8943 11.5037 17.9999 11.5037 17.9999C11.5037 17.9999 13.7157 14.8608 13.7157 13.5704C13.7157 12.3536 12.7259 11.3657 11.5037 11.3657ZM11.5037 14.1473C11.047 14.1473 10.6763 13.7765 10.6763 13.3213C10.6763 12.8662 11.047 12.4954 11.5037 12.4954C11.9617 12.4954 12.3324 12.8662 12.3324 13.3213C12.3324 13.7765 11.9617 14.1473 11.5037 14.1473Z"
        stroke="currentColor"
        strokeWidth="0.25"
        strokeMiterlimit="10"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
      <circle cx="11.5" cy="13.35" r="0.75" fill="white" />
    </svg>
  );
};
