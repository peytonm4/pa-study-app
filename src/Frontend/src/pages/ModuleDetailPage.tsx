import { useRef } from 'react';
import { Link, useParams } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { modules } from '@/api/modules';
import { documents, type DocumentStatus } from '@/api/documents';
import { figures, type FigureDto } from '@/api/figures';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';

function statusVariant(status: DocumentStatus['status']): 'default' | 'secondary' | 'destructive' {
  if (status === 'Ready') return 'default';
  if (status === 'Failed') return 'destructive';
  return 'secondary';
}

function isTerminal(status: DocumentStatus['status']) {
  return status === 'Ready' || status === 'Failed';
}

function DocumentRow({ doc, moduleId }: { doc: DocumentStatus; moduleId: string }) {
  const queryClient = useQueryClient();

  const { data: polled } = useQuery({
    queryKey: ['doc-status', doc.id],
    queryFn: () => documents.getStatus(doc.id),
    initialData: doc,
    refetchInterval: (query) => {
      const s = query.state.data?.status;
      return s === 'Ready' || s === 'Failed' ? false : 3000;
    },
  });

  const deleteMutation = useMutation({
    mutationFn: () => documents.delete(doc.id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['module', moduleId] });
    },
  });

  const status = polled?.status ?? doc.status;

  return (
    <li className="flex items-center justify-between px-4 py-3 hover:bg-muted/40 transition-colors">
      <div className="flex items-center gap-3 flex-1 min-w-0">
        <span className="text-sm truncate">{polled?.fileName ?? doc.fileName}</span>
        <Badge variant={statusVariant(status)} className="shrink-0">
          {status}
          {!isTerminal(status) && (
            <span className="ml-1 inline-block h-2 w-2 rounded-full bg-current animate-pulse" />
          )}
        </Badge>
        {status === 'Failed' && (
          <span className="text-xs text-destructive">Processing failed</span>
        )}
      </div>
      <div className="flex items-center gap-4 ml-4 shrink-0">
        <span className="text-sm text-muted-foreground">
          {new Date(doc.createdAt).toLocaleDateString()}
        </span>
        <Button
          variant="ghost"
          size="sm"
          onClick={() => deleteMutation.mutate()}
          disabled={deleteMutation.isPending}
          className="text-destructive hover:text-destructive"
        >
          {deleteMutation.isPending ? 'Deleting...' : 'Delete'}
        </Button>
      </div>
    </li>
  );
}

type ExtractionStatus = 'NotStarted' | 'Queued' | 'Processing' | 'Ready' | 'Failed';

function extractionStatusVariant(status: ExtractionStatus): 'default' | 'secondary' | 'destructive' {
  if (status === 'Ready') return 'default';
  if (status === 'Failed') return 'destructive';
  return 'secondary';
}

function FigureCard({ fig, onToggle, isPending }: { fig: FigureDto; onToggle: (keep: boolean) => void; isPending: boolean }) {
  return (
    <div className="border rounded-md overflow-hidden bg-card">
      <img
        src={fig.s3ThumbnailUrl}
        alt={`Page ${fig.pageNumber} figure`}
        className="w-full h-40 object-cover bg-muted"
      />
      <div className="p-3 space-y-2">
        <div className="flex items-center gap-2 flex-wrap">
          <Badge variant="secondary">Page {fig.pageNumber}</Badge>
          {fig.labelType && (
            <Badge variant="outline">{fig.labelType}</Badge>
          )}
        </div>
        {fig.caption && (
          <p className="text-xs text-muted-foreground line-clamp-2">{fig.caption}</p>
        )}
        <Button
          variant={fig.keep ? 'default' : 'ghost'}
          size="sm"
          className="w-full"
          onClick={() => onToggle(!fig.keep)}
          disabled={isPending}
        >
          {fig.keep ? 'Keep' : 'Ignore'}
        </Button>
      </div>
    </div>
  );
}

export default function ModuleDetailPage() {
  const { id } = useParams<{ id: string }>();
  const queryClient = useQueryClient();
  const fileInputRef = useRef<HTMLInputElement>(null);

  const { data: mod, isLoading } = useQuery({
    queryKey: ['module', id],
    queryFn: () => modules.get(id!),
    enabled: !!id,
    refetchInterval: (query) => {
      const s = query.state.data?.extractionStatus;
      return s === 'Ready' || s === 'Failed' ? false : 3000;
    },
  });

  const { data: figuresList, isLoading: figuresLoading } = useQuery({
    queryKey: ['figures', id],
    queryFn: () => figures.list(id!),
    enabled: !!id,
  });

  const uploadMutation = useMutation({
    mutationFn: (file: File) => documents.upload(id!, file),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['module', id] });
    },
  });

  const toggleMutation = useMutation({
    mutationFn: ({ figureId, keep }: { figureId: string; keep: boolean }) =>
      figures.toggle(figureId, keep),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['figures', id] }),
  });

  const runExtractionMutation = useMutation({
    mutationFn: () => figures.runExtraction(id!),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['module', id] }),
  });

  function handleFileChange(e: React.ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0];
    if (!file) return;
    uploadMutation.mutate(file);
    e.target.value = '';
  }

  async function handleDownload() {
    const data = await figures.getDocxDownloadUrl(id!);
    window.open(data.url, '_blank');
  }

  const extractionStatus = mod?.extractionStatus ?? 'NotStarted';
  const isExtractionRunning = extractionStatus === 'Queued' || extractionStatus === 'Processing';
  const hasFigures = (figuresList?.length ?? 0) > 0;
  const hasReviewedFigure = figuresList?.some(f => f.keep !== undefined) ?? false;

  return (
    <div className="max-w-4xl mx-auto py-8 px-4">
      <Link to="/modules" className="text-sm text-muted-foreground hover:underline mb-4 inline-block">
        ← All Modules
      </Link>

      {isLoading && <p className="text-muted-foreground">Loading...</p>}

      {mod && (
        <>
          <div className="flex items-center justify-between mb-6">
            <h1 className="text-2xl font-semibold">{mod.name}</h1>
            <div className="flex items-center gap-3">
              <input
                ref={fileInputRef}
                type="file"
                accept=".pptx,.pdf"
                className="hidden"
                onChange={handleFileChange}
              />
              <Button
                onClick={() => fileInputRef.current?.click()}
                disabled={uploadMutation.isPending}
              >
                {uploadMutation.isPending ? 'Uploading...' : 'Upload File'}
              </Button>
            </div>
          </div>

          {mod.documents.length === 0 && (
            <p className="text-muted-foreground">No files yet. Upload a PDF or PPTX to get started.</p>
          )}

          {mod.documents.length > 0 && (
            <ul className="divide-y divide-border rounded-md border">
              {mod.documents.map(doc => (
                <DocumentRow key={doc.id} doc={doc} moduleId={id!} />
              ))}
            </ul>
          )}

          {/* Figures Review Section */}
          <div className="mt-10">
            <h2 className="text-xl font-semibold mb-4">Figures Review</h2>

            {figuresLoading && (
              <p className="text-muted-foreground">Loading figures...</p>
            )}

            {!figuresLoading && !hasFigures && (
              <p className="text-muted-foreground">No figures extracted yet.</p>
            )}

            {!figuresLoading && hasFigures && (
              <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
                {figuresList!.map(fig => (
                  <FigureCard
                    key={fig.id}
                    fig={fig}
                    onToggle={(keep) => toggleMutation.mutate({ figureId: fig.id, keep })}
                    isPending={toggleMutation.isPending}
                  />
                ))}
              </div>
            )}
          </div>

          {/* Lecture Extraction Section */}
          <div className="mt-10">
            <h2 className="text-xl font-semibold mb-4">Lecture Extraction</h2>

            <div className="flex items-center gap-4 mb-4">
              <span className="text-sm text-muted-foreground">Status:</span>
              <Badge variant={extractionStatusVariant(extractionStatus)}>
                {extractionStatus}
                {isExtractionRunning && (
                  <span className="ml-1 inline-block h-2 w-2 rounded-full bg-current animate-pulse" />
                )}
              </Badge>
              {extractionStatus === 'Failed' && (
                <span className="text-xs text-destructive">Extraction failed. Try again.</span>
              )}
            </div>

            <div className="flex items-center gap-3">
              <Button
                onClick={() => runExtractionMutation.mutate()}
                disabled={isExtractionRunning || !hasFigures || !hasReviewedFigure || runExtractionMutation.isPending}
              >
                {runExtractionMutation.isPending ? 'Starting...' : 'Run Extraction'}
              </Button>

              {extractionStatus === 'Ready' && (
                <Button variant="outline" onClick={handleDownload}>
                  Download Lecture (.docx)
                </Button>
              )}
            </div>
          </div>
        </>
      )}
    </div>
  );
}
