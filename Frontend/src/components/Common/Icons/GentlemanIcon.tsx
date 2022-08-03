import React from 'react';

interface IconProps {
  color: 'white' | 'black';
  className?: string;
}
export const GentlemanIcon = ({ color, className }: IconProps) => {
  return (
    <svg
      width="14"
      height="32"
      viewBox="0 0 14 32"
      fill="none"
      xmlns="http://www.w3.org/2000/svg"
      className={className}
    >
      <title> Strekmann-ikon</title>
      <path
        d="M6.99985 10.1492C7.97945 10.1492 8.93706 9.85872 9.75157 9.31448C10.5661 8.77024 11.2009 7.99669 11.5758 7.09165C11.9507 6.18661 12.0488 5.19074 11.8577 4.22995C11.6665 3.26917 11.1948 2.38664 10.5021 1.69395C9.80944 1.00126 8.92691 0.529539 7.96613 0.338428C7.00534 0.147316 6.00947 0.245402 5.10443 0.62028C4.19939 0.995159 3.42584 1.62999 2.8816 2.44451C2.33736 3.25902 2.04687 4.21663 2.04688 5.19623C2.04845 6.50936 2.57078 7.76825 3.49931 8.69677C4.42783 9.62529 5.68672 10.1476 6.99985 10.1492V10.1492ZM6.99985 0.968663C7.83598 0.968663 8.65334 1.21661 9.34856 1.68114C10.0438 2.14567 10.5856 2.80592 10.9056 3.57841C11.2256 4.3509 11.3093 5.20092 11.1462 6.02099C10.9831 6.84106 10.5804 7.59434 9.98919 8.18557C9.39795 8.77681 8.64467 9.17945 7.82461 9.34257C7.00454 9.50569 6.15452 9.42197 5.38203 9.10199C4.60954 8.78202 3.94929 8.24016 3.48475 7.54494C3.02022 6.84972 2.77228 6.03236 2.77228 5.19623C2.77385 4.07549 3.21976 3.00111 4.01224 2.20863C4.80473 1.41614 5.87911 0.970236 6.99985 0.968663V0.968663Z"
        fill={color}
      />
      <path
        d="M0.506836 11.6535V18.6043H1.22035V12.367H12.7793V18.6103H13.4928V11.6476C13.4928 11.6476 0.506836 11.6119 0.506836 11.6535Z"
        fill={color}
      />
      <path d="M5.88237 14.0022H5.1748V31.7568H5.88237V14.0022Z" fill={color} />
      <path
        d="M8.82573 14.0022H8.11816V31.7568H8.82573V14.0022Z"
        fill={color}
      />
    </svg>
  );
};
