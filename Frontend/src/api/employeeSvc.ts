import { getEmployeeSvcUrl } from 'src/config';
import { get } from './crud';

export const getEmailAndName = (
  employeeId: number
): Promise<{ name: string; email: string }> =>
  get({
    host: getEmployeeSvcUrl(),
    path: `/v2/employees/${employeeId}?IncludeNotStarted=false&IncludeResigned=false&IncludeStillingsgrad=false&IncludeRoles=false&IncludePersonellResponsible=false`,
  });
