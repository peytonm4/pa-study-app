import client from './client';

export interface FigureDto {
  id: string;
  s3ThumbnailUrl: string;
  pageNumber: number;
  keep: boolean;
  labelType: string | null;
  caption: string | null;
}

export const figures = {
  list: (moduleId: string) =>
    client.get<FigureDto[]>(`/modules/${moduleId}/figures`).then(r => r.data),
  toggle: (figureId: string, keep: boolean) =>
    client.patch<FigureDto>(`/figures/${figureId}`, { keep }).then(r => r.data),
  runExtraction: (moduleId: string) =>
    client.post(`/modules/${moduleId}/extract`).then(r => r.data),
  getDocxDownloadUrl: (moduleId: string) =>
    client.get<{ url: string }>(`/modules/${moduleId}/docx`).then(r => r.data),
};
