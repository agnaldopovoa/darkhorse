import api from './client';
import type { DashboardStats, PortfolioValue } from './models';

export const dashboardApi = {
  getStats: async (): Promise<DashboardStats> => {
    const res = await api.get<DashboardStats>('/dashboard/stats');
    return res.data;
  },

  getPortfolio: async (): Promise<PortfolioValue> => {
    const res = await api.get<PortfolioValue>('/portfolio/value');
    return res.data;
  }
};
