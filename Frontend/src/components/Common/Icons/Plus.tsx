import React from 'react';

interface IProps {
  className?: string;
  title?: string;
}

export function Plus({ className, title }: IProps) {
  return (
    <svg
      version="1.1"
      className={className}
      fill="transparent"
      stroke="white"
      viewBox="19.52 18.65 34.83 34.87">
      <title>{title || 'Legg til'}</title>
      <g fill="white">
        <polygon points="36.3 50.99 37.58 50.99 37.58 36.73 51.84 36.73 51.84 35.45 37.58 35.45 37.58 21.18 36.3 21.18 36.3 35.45 22.04 35.45 22.04 36.73 36.3 36.73 36.3 50.99"></polygon>
      </g>
    </svg>
  );
}
