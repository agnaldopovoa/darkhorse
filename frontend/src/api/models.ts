export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
}

export interface UserContext {
  id: string;
  email: string;
}

export interface BrokerCredentialDto {
  id: string;
  brokerName: string;
  feeRate: number;
  fundingRate: number;
  isSandbox: boolean;
  status: string;
  lastTestedAt: string | null;
  createdAt: string;
}

export interface StrategyDto {
  id: string;
  name: string;
  symbol: string;
  timeframe: string;
  status: string;
  mode: string;
  circuitState: string;
  circuitFailures: number;
  script: string;
  parameters: string;
  createdAt: string;
  updatedAt: string;
}

export interface RecentOrderDto {
  id: string;
  symbol: string;
  side: string;
  fillPrice: number | null;
  status: string;
  createdAt: string;
}

export interface SystemHealth {
  state: 'OK' | 'WARN' | 'CRITICAL';
  openCircuits: number;
  halfOpenCircuits: number;
  details: string;
}

export interface DashboardStats {
  activeStrategies: number;
  totalOrders: number;
  systemHealth: SystemHealth;
  recentOrders: RecentOrderDto[];
}

export interface PortfolioBreakdownItem {
  symbol: string;
  quantity: number;
  price: number;
  valueUsd: number;
}

export interface PortfolioValue {
  totalValueUsd: number;
  breakdown: PortfolioBreakdownItem[];
  cachedAt: string;
}
