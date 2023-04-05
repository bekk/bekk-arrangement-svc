import { getEmployeeSvcUrl } from 'src/config';
import { get } from './crud';

export const getEmailNameAndDepartment = (
  employeeId: number
): Promise<{ name: string; email: string; department: string }> =>
  get({
    host: getEmployeeSvcUrl(),
    path: `/v2/employees/${employeeId}?IncludeNotStarted=false&IncludeResigned=false&IncludeStillingsgrad=false&IncludeRoles=false&IncludePersonellResponsible=false`,
  });
