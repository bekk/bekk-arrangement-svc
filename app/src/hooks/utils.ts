import { useState, useLayoutEffect, useEffect } from 'react';

export const useUpdateableInitialValue = <T extends string | number>(
  initialValue: T
): [T, (value: T) => void] => {
  const [value, setValue] = useState(initialValue);
  useLayoutEffect(() => {
    setValue(initialValue);
  }, [initialValue]);

  return [value, setValue];
};

export const useEffectOnce = (effect: () => void) => {
  const [hasRun, setHasRun] = useState(false);
  useEffect(() => {
    if (!hasRun) {
      effect();
      setHasRun(true);
    }
  }, [effect, hasRun]);
};
