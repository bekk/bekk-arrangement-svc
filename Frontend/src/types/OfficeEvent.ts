import {
  array,
  date,
  decodeType,
  record,
  string,
} from 'typescript-json-decoder';

export type OfficeEvent = decodeType<typeof OfficeEventDecoder>;
export const OfficeEventDecoder = record({
  contactPerson: string,
  createdAt: date,
  description: string,
  endTime: date,
  id: string,
  location: string,
  modifiedAt: date,
  startTime: date,
  themes: array(string),
  title: string,
  types: array(string),
});
