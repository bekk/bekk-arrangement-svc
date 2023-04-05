import queryString from 'query-string';
import { useHistory, useParams } from 'react-router';
import { Optional } from 'src/types';

export const queryStringStringify = (
  queries: Record<string, string | number | undefined>
): string => {
  const query = queryString.stringify(queries);
  if (query) {
    return `?${query}`;
  }
  return '';
};

export const useQuery = (key: string) => {
  const {
    location: { search },
  } = useHistory();
  const params = queryString.parse(search);
  if (key in params) {
    const value = params[key];
    if (typeof value === 'string') {
      return value;
    }
  }
};

export const useParam = (parameter: string) => {
  const params: Record<string, Optional<string>> = useParams();
  return params[parameter] || '';
};
