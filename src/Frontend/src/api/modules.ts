import client from './client';

export interface Module {
  id: string;
  name: string;
  status: 'Processing' | 'Ready';
  createdAt: string;
}

export const modules = {
  list: () => client.get<Module[]>('/modules').then(r => r.data),
  create: (name: string) => client.post<Module>('/modules', { name }).then(r => r.data),
  delete: (id: string) => client.delete(`/modules/${id}`),
};
