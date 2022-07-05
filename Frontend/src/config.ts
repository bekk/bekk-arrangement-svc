let employeeSvcUrl: string;
let audience: string;
let issuerDomain: string;
let scopes: string;

interface IConfig {
  employeeSvcUrl: string
  audience: string
  issuerDomain: string
  scopes: string
}

export const getConfig = async (): Promise<IConfig> => {
  const response = await fetch(`/api/config`);
  const json = await response.json();
  return json as IConfig;
}

export const setConfig = (config: IConfig) => {
  employeeSvcUrl = config.employeeSvcUrl;
  audience = config.audience;
  issuerDomain = config.issuerDomain;
  scopes = config.scopes;
}

export const getEmployeeSvcUrl = ()  => employeeSvcUrl
export const getAudience = () => audience
export const getIssuerDomain = () => issuerDomain
export const getScopes = () => scopes

