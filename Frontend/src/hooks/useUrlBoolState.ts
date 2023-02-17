import { useHistory } from 'react-router';
import { useEffect, useState } from 'react';

export const useUrlBoolState = (initialValue: boolean, key: string) => {
  const serializeBool = (bool: boolean) => (bool ? '1' : '0');
  const deserializeBool = (string: string) => string === '1';
  const history = useHistory();
  const search = new URLSearchParams(history.location.search);

  const urlValue = search.get(key);
  const [state, setCurrentState] = useState(
    urlValue ? urlValue : serializeBool(initialValue)
  );

  const setState = (state: boolean) => {
    setCurrentState(serializeBool(state));
    const searchParameters = new URLSearchParams(history.location.search);
    searchParameters.set(key, serializeBool(state));
    const pathname = history.location.pathname;
    history.push({ pathname, search: searchParameters.toString() });
  };

  useEffect(() => {
    if (urlValue && urlValue !== state) setState(deserializeBool(urlValue));
    if (!urlValue && initialValue) setState(initialValue);
  }, [urlValue]);

  return [deserializeBool(state), setState] as const;
};
