import React from 'react';
interface IconProps {
  color: 'white' | 'black';
  className?: string;
}
export const ExternalIcon = ({ color, className }: IconProps) => {
  return (
    <>
      <svg
        width="15"
        height="18"
        viewBox="0 0 15 18"
        fill="none"
        xmlns="http://www.w3.org/2000/svg"
        className={className}
      > 
        <title>Stilisert mann med pil mot høyre</title>
        <path d="M14 18V11H1V18" stroke={color} strokeWidth="0.7" />
        <path
          d="M12.5 14.5L10.25 15.799L10.25 13.201L12.5 14.5Z"
          fill={color}
        />
        <circle cx="7.5" cy="4.5" r="4.15" stroke={color} strokeWidth="0.7" />
        <path d="M2.5 14.2H10.5V14.7H2.5V14.2Z" fill={color} />
      </svg>
    </>
  );
};
