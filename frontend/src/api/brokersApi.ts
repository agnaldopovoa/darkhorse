import api from './client';
import type { BrokerCredentialDto } from './models';

export interface CreateBrokerDto {
  brokerName: string;
  apiKey: string;
  secret: string;
  feeRate: number;
  fundingRate: number;
  isSandbox: boolean;
}

export const brokersApi = {
  getAll: async (): Promise<BrokerCredentialDto[]> => {
    const res = await api.get<BrokerCredentialDto[]>('/brokers');
    return res.data;
  },

  create: async (dto: CreateBrokerDto): Promise<string> => {
    const res = await api.post<{ id: string }>('/brokers', dto);
    return res.data.id;
  },

  remove: async (id: string): Promise<void> => {
    await api.delete(`/brokers/${id}`);
  }
};
