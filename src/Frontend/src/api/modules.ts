import client from './client';

export interface Module {
  id: string;
  name: string;
  status: 'Processing' | 'Ready';
  createdAt: string;
}

export interface ModuleDetail extends Module {
  documents: import('./documents').DocumentStatus[];
  extractionStatus: 'NotStarted' | 'Queued' | 'Processing' | 'Ready' | 'Failed';
  generationStatus: 'NotStarted' | 'Queued' | 'Processing' | 'Ready' | 'Failed';
  docxS3Key: string | null;
}

export const modules = {
  list: () => client.get<Module[]>('/modules').then(r => r.data),
  get: (id: string) => client.get<ModuleDetail>(`/modules/${id}`).then(r => r.data),
  create: (name: string) => client.post<Module>('/modules', { name }).then(r => r.data),
  delete: (id: string) => client.delete(`/modules/${id}`),
  generate: (id: string) => client.post(`/modules/${id}/generate`).then(r => r.data),
};
