import React, { useEffect, useRef } from 'react';
import { myp5 } from 'src/components/Common/FunEffects/Christmas/makeChristmas';

interface Props {
  noSpawnClick: React.RefObject<HTMLDivElement>;
}

export const Christmas = ({ noSpawnClick }: Props) => {
  const ref = useRef<HTMLDivElement>(null);

  useEffect(() => {
    myp5(ref.current, noSpawnClick.current);
  }, [ref, noSpawnClick]);

  return (
    <div
      ref={ref}
      style={{ position: 'absolute', pointerEvents: 'none' }}
    ></div>
  );
};
