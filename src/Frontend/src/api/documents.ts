import client from './client';

export interface DocumentStatus {
  id: string;
  fileName: string;
  status: 'Uploading' | 'Queued' | 'Processing' | 'Ready' | 'Failed';
  createdAt: string;
}

export const documents = {
  upload: (moduleId: string, file: File) => {
    const form = new FormData();
    form.append('file', file);
    return client.post<DocumentStatus>(`/modules/${moduleId}/documents`, form, {
      headers: { 'Content-Type': 'multipart/form-data' },
    }).then(r => r.data);
  },
  getStatus: (id: string) =>
    client.get<DocumentStatus>(`/documents/${id}/status`).then(r => r.data),
  delete: (id: string) => client.delete(`/documents/${id}`),
};
