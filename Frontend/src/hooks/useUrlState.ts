import { useHistory } from 'react-router';
import { useEffect, useState } from 'react';

/**
 * Stores T as state in URL.
 * Functions as Reacts SetState.
 *
 * @param key - The key used to store the value in the url
 * @param initialValue - The initial value of type T to store
 * @param serialize - A function that serializes T to a string
 * @param deserialize - A function that deserializes string to T
 * @returns Returns a stateful value, and a function to update it.
 */
export const useUrlState = <T>(
  key: string,
  initialValue: T,
  serialize: (value: T) => string,
  deserialize: (value: string) => T
) => {
  const history = useHistory();
  const search = new URLSearchParams(history.location.search);

  const urlValue = search.get(key);
  const [state, setCurrentState] = useState(
    urlValue ? urlValue : serialize(initialValue)
  );

  const setState = (state: T) => {
    const serializedState = serialize(state);
    setCurrentState(serializedState);
    const searchParameters = new URLSearchParams(history.location.search);
    searchParameters.set(key, serializedState);
    const pathname = history.location.pathname;
    history.push({ pathname, search: searchParameters.toString() });
  };

  useEffect(() => {
    if (urlValue && urlValue !== state) setState(deserialize(urlValue));
    if (!urlValue && initialValue) setState(initialValue);
  }, [urlValue, initialValue, state, serialize, deserialize]);

  return [deserialize(state), setState] as const;
};
