import api from './client';
import type { StrategyDto } from './models';

export interface CreateStrategyDto {
  credentialId?: string;
  name: string;
  symbol: string;
  timeframe: string;
  script: string;
  parameters: string;
  mode: string;
  maxPositionSize?: number;
  maxDailyVolume?: number;
}

export interface UpdateStrategyDto {
  script: string;
  parameters: string;
}

export interface RunBacktestDto {
  startDate: string;
  endDate: string;
}

export const strategiesApi = {
  getAll: async (): Promise<StrategyDto[]> => {
    const res = await api.get<StrategyDto[]>('/strategies');
    return res.data;
  },

  create: async (dto: CreateStrategyDto): Promise<string> => {
    const res = await api.post<{ id: string }>('/strategies', dto);
    return res.data.id;
  },

  update: async (id: string, dto: UpdateStrategyDto): Promise<void> => {
    await api.put(`/strategies/${id}`, dto);
  },

  start: async (id: string): Promise<void> => {
    await api.post(`/strategies/${id}/start`);
  },

  backtest: async (id: string, dto: RunBacktestDto): Promise<string> => {
    const res = await api.post<{ jobId: string }>(`/strategies/${id}/backtest`, dto);
    return res.data.jobId;
  }
};
