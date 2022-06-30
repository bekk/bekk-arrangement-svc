import { UserNotification } from 'src/components/NotificationHandler/NotificationHandler';

export type ErrorType = 'Error' | 'Warning';
const isErrorType = (s: string): s is ErrorType =>
  s === 'Error' || s === 'Warning';

export interface IError {
  message: string;
  type: ErrorType;
}
const isIError = (error: any): error is IError =>
  typeof error === 'object' &&
  error !== null &&
  'message' in error &&
  typeof error.message === 'string' &&
  'type' in error &&
  isErrorType(error.type);

export const isIErrorList = (errors: any): errors is IError[] =>
  Array.isArray(errors) && errors.every(isIError) && errors.length > 0;

export const isValid = <T>(notErrors: T): notErrors is Exclude<T, IError[]> =>
  !isIErrorList(notErrors);

export function assertIsValid<T>(
  validValue: { [K in keyof T]: T[K] | IError[] }
): asserts validValue is T {
  if (Object.values(validValue).some(isIErrorList)) {
    throw new UserNotification(
      'Kunne ikke parses av følgende grunner: ' +
        Object.values(validValue)
          .filter(isIErrorList)
          .flat()
          .map((x) => x.message)
          .join(', ')
    );
  }
}

export const listOfErrors = (obj: Record<string, unknown>): IError[] =>
  Object.values(obj).filter(isIErrorList).flat();

export const error = (message: string): IError => ({
  type: 'Error',
  message,
});

export const warning = (message: string): IError => ({
  type: 'Warning',
  message,
});

export const validate = <To>(validations: Record<string, boolean> = {}) => {
  const errorMessages = Object.entries(validations)
    .filter(([errorMessage, isError]) => isError)
    .map(([errorMessage]) => errorMessage);
  return {
    resolve: (validatedValue: To) => {
      if (errorMessages.length > 0) {
        return errorMessages.map(error);
      }
      return validatedValue;
    },
    reject: (errorMessage: string) => error(errorMessage),
  };
};
