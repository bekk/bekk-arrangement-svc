import React, { useRef, useState, useEffect } from 'react';

/**
 *  Hentet fra Bekk-forside-app
 *  https://github.com/bekk/bekk-forside-app/blob/8c3451f88adca6c1b8b0f90b0c1786dea5f72cbb/app/src/components/KeyfigureView/SineCurve/SineCurve.tsx
 *  Credit går til oleast og selbekk
 */

interface IProps {
  /**
   * Width and height define the size of the canvas, not the size in the DOM.
   * They are used to configure a resolution of the animation.
   */
  width: number;
  height: number;

  frequency: number;
  amplitude: number;
  /* The speed in pixels per animated frame */
  speed?: number;
  animate?: boolean;
  className?: string;
  color: string;
}

export function SineCurve({
  className,
  width,
  height,
  frequency,
  amplitude,
  speed = 1,
  animate = false,
  color,
}: IProps) {
  const animationHandler = useRef(0);
  const [animationOffset, setAnimationOffset] = useState(0);
  const sinePath = createSineCurvePath({
    width,
    height,
    frequency,
    amplitude,
    offset: animationOffset,
  });

  useEffect(() => {
    function draw() {
      if (animate) {
        animationHandler.current = window.requestAnimationFrame(draw);
        setAnimationOffset((offset) => offset + speed);
      }
    }
    draw();
    return () => window.cancelAnimationFrame(animationHandler.current);
  }, [animate, speed]);

  return (
    <svg viewBox={`0 0 ${width} ${height + 1}`} className={className}>
      <title> Lang bølge </title>
      <path strokeWidth="2.6" d={sinePath} fill={color} />
    </svg>
  );
}

interface ISineCurvePathParams {
  width: number;
  height: number;
  amplitude: number;
  frequency: number;
  offset?: number;
}

function createSineCurvePath({
  width,
  height,
  amplitude,
  frequency,
  offset = 0,
}: ISineCurvePathParams): string {
  const path: string[] = [];
  for (let x = 0; x <= width; x++) {
    const y = height / 2 + amplitude * Math.sin((x + offset) / frequency);
    const pathCommand = x === 0 ? 'M' : 'L';
    path.push(`${pathCommand}${x},${y}`);
  }
  path.push(`L${width},-${height + 1}`, `L0,-${height + 1}`, `Z`);
  return path.join(' ');
}
