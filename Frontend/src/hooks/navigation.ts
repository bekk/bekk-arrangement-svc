import { useRef, useEffect } from 'react';

export function useOnClickOutside(
  onClickOutside: () => void
): React.RefObject<HTMLDivElement> {
  const ref = useRef<HTMLDivElement>(null);
  useEffect(() => {
    const handleClickOutside = (event: any) => {
      if (ref && ref.current && ref.current.contains(event.target)) {
        return;
      }
      onClickOutside();
    };

    document.addEventListener('mouseup', handleClickOutside);
    return () => document.removeEventListener('mouseup', handleClickOutside);
  }, [onClickOutside]);

  return ref;
}

function useKeyDown(key: string, onKeyDown: () => void) {
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === key && !e.getModifierState('Shift')) {
        onKeyDown();
      }
    };
    document.addEventListener('keydown', handleKeyDown, false);
    return () => document.removeEventListener('keydown', handleKeyDown, false);
  }, [key, onKeyDown]);
}

export function useOnEscape(onEscape: () => void) {
  useKeyDown('Escape', onEscape);
}
