import React, { useEffect, useRef } from 'react';
import { myp5 } from 'src/components/Common/FunEffects/Kittens/makeKittens';

export const Kittens = () => {
  const ref = useRef<HTMLDivElement>(null);

  useEffect(() => {
    myp5(ref.current);
  }, [ref]);

  return (
    <div
      ref={ref}
      style={{ position: 'absolute', pointerEvents: 'none' }}
    ></div>
  );
};
