import classnames from 'classnames';
import React from 'react';
import style from './DownloadIcon.module.scss';

interface IconProps {
  title: string;
  className?: string;
}
export const DownloadIcon = ({ title, className }: IconProps) => {
  let styles = classnames(style.downloadIcon, className);
  return (
    <svg
      width="39"
      height="39"
      viewBox="0 0 39 39"
      fill="none"
      xmlns="http://www.w3.org/2000/svg"
      className={styles}
    >
      <title> {title} </title>
      <rect x="0.5" y="0.5" width="38" height="38" stroke="white" />
      <g clipPath="url(#downloadIcon)">
        <path
          d="M27.5286 20.9101L26.8255 20.229L20.0048 26.8246V4.58838H18.9934V26.8246L12.1728 20.229L11.4697 20.9101L19.4991 28.6886L27.5286 20.9101Z"
          fill="white"
        />
        <path
          d="M24.7411 32.297H14.2695V33.2648H24.7411V32.297Z"
          fill="white"
        />
      </g>
      <defs>
        <clipPath id="downloadIcon">
          <rect
            width="16.0588"
            height="28.6765"
            fill="white"
            transform="translate(11.4697 4.58838)"
          />
        </clipPath>
      </defs>
    </svg>
  );
};
