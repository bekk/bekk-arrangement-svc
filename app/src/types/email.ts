import { validate, IError } from './validation';

type EmailViewModel = string;
type EmailWriteModel = string;

export interface Email {
  email: string;
}

type EditEmail = string;

export const parseEditEmail = (email: string): Email | IError[] => {
  const validator = validate<Email>({
    'E-post må inneholde minst tre tegn': email.length < 3,
    'E-post må inneholde et @-tegn': !email.includes('@'),
  });
  return validator.resolve({ email });
};

export const toEditEmail = ({ email }: Email): EditEmail => email;

export const parseEmailViewModel = (email: EmailViewModel): Email => ({
  email,
});
export const toEmailWriteModel = ({ email }: Email): EmailWriteModel => email;

// Utils functions

export const stringifyEmail = ({ email }: Email) => email;
