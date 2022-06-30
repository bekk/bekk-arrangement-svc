import React, { ReactChild, useState } from 'react';
import style from './Header.module.scss';
import { SineCurve } from 'src/components/Common/SineCurve/SineCurve';
import classNames from 'classnames';
import { useEventColor } from 'src/components/ViewEventsCards/EventCardElement';

interface IProps {
  children?: ReactChild[] | ReactChild;
  eventId?: string | 'all-events';
  eventTitle?: string;
  customHexColor?: string;
}

export const WavySubHeader = ({
  children,
  eventId,
  eventTitle = '',
  customHexColor,
}: IProps) => {
  const [isHovered, setIsHovered] = useState(false);

  const { style: colorStyle, colorCode } = useEventColor(
    eventId,
    style,
    eventTitle,
    customHexColor
  );

  return (
    <div
      className={style.subHeaderContainer}
      onMouseMove={() => setIsHovered(true)}
      onMouseLeave={() => setIsHovered(false)}
    >
      <div
        style={{ backgroundColor: colorCode }}
        className={classNames(style.contentContainer, colorStyle)}
      >
        {children}
      </div>
      <div className={style.sineCurve}>
        <SineCurve
          width={window.innerWidth}
          height={100}
          frequency={25}
          amplitude={8}
          speed={2}
          animate={isHovered}
          color={colorCode}
          className={style.sineCurveContent}
        />
      </div>
    </div>
  );
};
