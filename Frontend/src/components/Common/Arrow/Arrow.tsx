import React from 'react';
import classNames from 'classnames';
import style from './Arrow.module.scss';

type Direction = 'right' | 'left' | 'down' | 'up';

interface IProps {
  direction: Direction;
  noCircle?: boolean;
  className?: string;
  color?: 'white' | 'black';
}

export const Arrow = ({
  direction,
  noCircle,
  className,
  color = 'black',
}: IProps) => {
  const arrowStyle = classNames(style.arrow, className, {
    [style.left]: direction === 'left',
    [style.right]: direction === 'right',
    [style.up]: direction === 'up',
    [style.down]: direction === 'down',
    [style.whiteArrow]: color === 'white',
  });

  const points = noCircle
    ? '76 105 173.5 210 278.5 105'
    : '101 130 148.5 185 203.5 130';
  const strokeWidth = noCircle ? '15' : '5';

  return (
    <div className={arrowStyle}>
      <svg width="411px" height="420px" viewBox="0 0 411 420" version="1.1">
        <g stroke="000" strokeWidth="1" fill="none" fillRule="evenodd">
          <g transform="translate(33.000000, 33.000000)" stroke="#000">
            <polyline strokeWidth={strokeWidth} points={points} />
            {!noCircle && (
              <ellipse
                strokeWidth="4"
                cx="152.5"
                cy="157"
                rx="152.5"
                ry="157"
              />
            )}
          </g>
        </g>
      </svg>
    </div>
  );
};
