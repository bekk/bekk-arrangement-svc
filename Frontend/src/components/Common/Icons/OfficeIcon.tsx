import React from 'react';

interface IconProps {
  color: 'white' | 'black';
  className?: string;
}

export const OfficeIcon = ({ color, className }: IconProps) => {
  return (
    <svg
      width="21"
      height="29"
      viewBox="0 0 21 29"
      xmlns="http://www.w3.org/2000/svg"
      className={className}
      shapeRendering="crispEdges">
      <title>Kontor</title>
      <path fill="none" d="M19.5 2H1V28H19.5V2Z" stroke={color} />
      <path fill="none" d="M9 6H5V10H9V6Z" stroke={color} />
      <path fill="none" d="M9 13H5V17H9V13Z" stroke={color} />
      <path fill="none" d="M16 6H12V10H16V6Z" stroke={color} />
      <path fill="none" d="M16 13H12V17H16V13Z" stroke={color} />
      <path fill="none" d="M13 20H8V28H13V20Z" stroke={color} />
    </svg>
  );
};
