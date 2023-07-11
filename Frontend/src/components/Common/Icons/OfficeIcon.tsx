import React from 'react';

interface IconProps {
  color: 'white' | 'black';
  className?: string;
}

export const OfficeIcon_old = ({ color, className }: IconProps) => {
  return (
    <svg
      width="21"
      height="24"
      viewBox="0 0 21 24"
      fill="none"
      xmlns="http://www.w3.org/2000/svg"
      className={className}
      shapeRendering="crispEdges">
      <path d="M20 1H1V23H20V1Z" stroke={color} strokeWidth="0.5" />
      <path d="M9.5 4H6.5V8H9.5V4Z" stroke={color} strokeWidth="0.5" />
      <path d="M9.5 10H6.5V14H9.5V10Z" stroke={color} strokeWidth="0.5" />
      <path d="M15.5 4H12.5V8H15.5V4Z" stroke={color} strokeWidth="0.5" />
      <path d="M15.5 10H12.5V14H15.5V10Z" stroke={color} strokeWidth="0.5" />
      <path d="M12.5 16H8.5V23H12.5V16Z" stroke={color} strokeWidth="0.5" />
    </svg>
  );
};

export const OfficeIcon = ({ color, className }: IconProps) => {
    if (Math.random() > 0.5) {
        return (
            <svg
                width="18"
                height="24"
                viewBox="0 0 18 24"
                fill="none"
                xmlns="http://www.w3.org/2000/svg"
                className={className}
            >
                <path d="M14 1H1V20H14V1Z" stroke={color} strokeWidth="0.5"/>
                <path d="M12 4H9V7H12V4Z" fill="white" stroke={color} strokeWidth="0.5"/>
                <path d="M6 4H3V7H6V4Z" fill="white" stroke={color} strokeWidth="0.5"/>
                <path d="M6 9H3V12H6V9Z" fill="white" stroke={color} strokeWidth="0.5"/>
                <path d="M12 9H9V12H12V9Z" fill="white" stroke={color} strokeWidth="0.5"/>
                <path d="M9 14H6V20H9V14Z" stroke={color} strokeWidth="0.5"/>
                <path d="M13.9991 14C12.3428 14 11 15.3401 11 16.9909C11 18.787 13.9991 23 13.9991 23C13.9991 23 17 18.7415 17 16.9909C17 15.3401 15.6572 14 13.9991 14ZM13.9991 17.7735C13.3796 17.7735 12.8767 17.2705 12.8767 16.653C12.8767 16.0356 13.3796 15.5325 13.9991 15.5325C14.6204 15.5325 15.1233 16.0356 15.1233 16.653C15.1233 17.2705 14.6204 17.7735 13.9991 17.7735Z" fill="white" stroke={color} strokeWidth="0.5" strokeMiterlimit="10" strokeLinecap="round" strokeLinejoin="round"/>
                <circle cx="14" cy="16.6499" r="1" fill="#0E0E0E"/>
            </svg>
        );
    } else {
        return (
            <svg
                width="18"
                height="24"
                viewBox="0 0 18 24"
                fill="none"
                xmlns="http://www.w3.org/2000/svg"
                className={className}
            >
                <path d="M14 1H1V20H14V1Z" stroke={color} strokeWidth="0.5"/>
                <path d="M12 4H9V7H12V4Z" fill="white" stroke={color} strokeWidth="0.5"/>
                <path d="M6 4H3V7H6V4Z" fill="white" stroke={color} strokeWidth="0.5"/>
                <path d="M6 9H3V12H6V9Z" fill="white" stroke={color} strokeWidth="0.5"/>
                <path d="M12 9H9V12H12V9Z" fill="white" stroke={color} strokeWidth="0.5"/>
                <path d="M9 14H6V20H9V14Z" stroke={color} strokeWidth="0.5"/>
                <path d="M13.9991 14C12.3428 14 11 15.3401 11 16.9909C11 18.787 13.9991 23 13.9991 23C13.9991 23 17 18.7415 17 16.9909C17 15.3401 15.6572 14 13.9991 14ZM13.9991 17.7735C13.3796 17.7735 12.8767 17.2705 12.8767 16.653C12.8767 16.0356 13.3796 15.5325 13.9991 15.5325C14.6204 15.5325 15.1233 16.0356 15.1233 16.653C15.1233 17.2705 14.6204 17.7735 13.9991 17.7735Z" fill="#0E0E0E" stroke={color} strokeWidth="0.5" strokeMiterlimit="10" strokeLinecap="round" strokeLinejoin="round"/>
                <circle cx="14" cy="16.6499" r="1" fill="white"/>
            </svg>
        );
    }
}
