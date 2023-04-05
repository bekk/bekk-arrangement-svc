import React from 'react';

interface IconProps {
  color: 'white' | 'black';
  className?: string;
}

export const OfficeIcon = ({ color, className }: IconProps) => {
  return (
      <svg width="21" height="24" viewBox="0 0 21 24" fill="none" xmlns="http://www.w3.org/2000/svg"

            className={className}>

          <path d="M20 1H1V23H20V1Z" stroke={color} strokeWidth="0.5"/>
          <path d="M9.5 4H6.5V8H9.5V4Z" stroke={color} strokeWidth="0.5"/>
          <path d="M9.5 10H6.5V14H9.5V10Z" stroke={color} strokeWidth="0.5"/>
          <path d="M15.5 4H12.5V8H15.5V4Z" stroke={color} strokeWidth="0.5"/>
          <path d="M15.5 10H12.5V14H15.5V10Z" stroke={color} strokeWidth="0.5"/>
          <path d="M12.5 16H8.5V23H12.5V16Z" stroke={color} strokeWidth="0.5"/>
      </svg>
  );
};
