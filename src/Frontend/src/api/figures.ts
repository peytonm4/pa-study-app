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
  downloadDocx: async (moduleId: string): Promise<void> => {
    const response = await client.get(`/modules/${moduleId}/docx/download`, { responseType: 'blob' });
    const href = URL.createObjectURL(response.data);
    const a = document.createElement('a');
    a.href = href;
    a.download = 'lecture.docx';
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    setTimeout(() => URL.revokeObjectURL(href), 100);
  },
};
