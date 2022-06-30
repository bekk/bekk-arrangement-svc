export const identityFunction = <T>(x: T) => x;

export const concatLists = <T>(...list: (T[] | unknown)[]): T[] =>
  list.flatMap(materialize as any);

const materialize = <T>(list: T[] | undefined): T[] => {
  return Array.isArray(list) ? list : [];
};

export const plural = (n: number, singular: string, plural: string) => {
  if (n === 1) {
    return `${n} ${singular}`;
  }
  return `${n} ${plural}`;
};
