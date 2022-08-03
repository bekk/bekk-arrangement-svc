import { useState, useEffect } from 'react';
import { calculateTimeLeft } from 'src/utils/timeleft';

export const useTimeLeft = (time: Date | false) => {
  const [timeLeft, setTimeLeft] = useState(
    calculateTimeLeft(time || new Date())
  );

  useEffect(() => {
    if (time) {
      setTimeLeft(calculateTimeLeft(time));
      const interval = setInterval(() => {
        setTimeLeft(calculateTimeLeft(time));
      }, 500);
      return () => clearInterval(interval);
    }
  }, [time]);

  return timeLeft;
};
