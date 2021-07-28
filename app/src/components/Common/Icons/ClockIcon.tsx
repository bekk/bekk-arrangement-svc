import React from 'react';

interface IconProps {
  color: 'white' | 'black';
  className?: string;
}
export const ClockIcon = ({ color, className }: IconProps) => {
  return (
    <svg
      width="32"
      height="32"
      viewBox="0 0 32 32"
      fill="none"
      xmlns="http://www.w3.org/2000/svg"
      className={className}
    >
      <title> Klokkeikon </title>
      <g>
        <path
          d="M16 32C19.1645 32 22.2579 31.0616 24.8891 29.3035C27.5203 27.5454 29.5711 25.0466 30.7821 22.1229C31.9931 19.1993 32.3099 15.9823 31.6926 12.8786C31.0752 9.77487 29.5513 6.92394 27.3137 4.6863C25.0761 2.44866 22.2251 0.924806 19.1214 0.307443C16.0177 -0.309921 12.8007 0.00693258 9.87706 1.21793C6.95345 2.42894 4.45459 4.4797 2.69649 7.11088C0.938384 9.74207 0 12.8355 0 16C0 20.2435 1.68571 24.3131 4.68629 27.3137C7.68687 30.3143 11.7565 32 16 32ZM16 1.12001C18.943 1.12001 21.8199 1.9927 24.2669 3.62774C26.7139 5.26277 28.6211 7.58671 29.7473 10.3057C30.8736 13.0246 31.1682 16.0165 30.5941 18.903C30.0199 21.7894 28.6028 24.4408 26.5217 26.5218C24.4407 28.6028 21.7894 30.0199 18.9029 30.5941C16.0165 31.1682 13.0246 30.8736 10.3057 29.7473C7.58671 28.6211 5.26277 26.7139 3.62773 24.2669C1.9927 21.8199 1.12 18.943 1.12 16C1.12705 12.0558 2.69702 8.27506 5.48604 5.48604C8.27505 2.69703 12.0557 1.12706 16 1.12001Z"
          fill={color}
        />
        <path
          d="M13.5468 23.6667L15.9202 17.7067H16.0002C16.4115 17.7176 16.8129 17.5797 17.1305 17.3181C17.4481 17.0566 17.6606 16.6891 17.7288 16.2834C17.7969 15.8776 17.7162 15.4609 17.5015 15.1099C17.2868 14.7589 16.9525 14.4974 16.5602 14.3733V4.49333H15.4402V14.3867C15.1446 14.4765 14.8792 14.6455 14.6728 14.8754C14.4664 15.1052 14.3268 15.3871 14.2691 15.6907C14.2114 15.9942 14.2379 16.3077 14.3456 16.5972C14.4533 16.8868 14.6382 17.1413 14.8802 17.3333L12.5068 23.2533L13.5468 23.6667Z"
          fill={color}
        />
      </g>
    </svg>
  );
};
