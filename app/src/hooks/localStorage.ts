type Option<T> = T | undefined;

export const useLocalStorage = ({
  localStorageKey,
}: {
  localStorageKey: string;
}): [() => Option<string>, (str: string) => void] => {
  const readLocalStorage = (): Option<string> => {
    const inStorage = localStorage.getItem(localStorageKey);
    if (inStorage !== null) {
      return inStorage;
    }
  };

  const setLocalStorage = (value: string) => {
    localStorage.setItem(localStorageKey, value);
    return readLocalStorage();
  };

  return [
    readLocalStorage,
    (valueToStore: string) => {
      setLocalStorage(valueToStore);
    },
  ];
};
