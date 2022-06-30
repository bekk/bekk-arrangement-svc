let bekkApiUrl: string;
let issuerDomain: string;
let audience: string;
let scopes: string;
let arrangementSvcUrl: string;
let employeeSvcUrl: string;

interface IConfig {
  bekkApiUrl: string;
  audience: string;
  issuerDomain: string;
  scopes: string;
  arrangementSvcUrl: string;
  employeeSvcUrl: string;
}

export function setConfig(config: IConfig): void {
  bekkApiUrl = config.bekkApiUrl;
  issuerDomain = config.issuerDomain;
  audience = config.audience;
  scopes = config.scopes;
  arrangementSvcUrl = config.arrangementSvcUrl;
  employeeSvcUrl = config.employeeSvcUrl;
}

export async function getConfig(): Promise<IConfig> {
  const response = await fetch('/config');
  return response.json() as Promise<IConfig>;
}

export function getBekkApiUrl(): string {
  return bekkApiUrl;
}

export function getIssuerDomain(): string {
  return issuerDomain;
}

export function getAudience(): string {
  return audience;
}

export function getScopes(): string {
  return scopes;
}

export function getArrangementSvcUrl(): string {
  return arrangementSvcUrl;
}

export function getEmployeeSvcUrl(): string {
  return employeeSvcUrl;
}
