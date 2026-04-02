import { useRef, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { modules } from '@/api/modules';
import { documents, type DocumentStatus } from '@/api/documents';
import { figures } from '@/api/figures';
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

type GenerationStatus = 'NotStarted' | 'Queued' | 'Processing' | 'Ready' | 'Failed';

function generationStatusVariant(status: GenerationStatus): 'default' | 'secondary' | 'destructive' {
  if (status === 'Ready') return 'default';
  if (status === 'Failed') return 'destructive';
  return 'secondary';
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
      const es = query.state.data?.extractionStatus;
      const gs = query.state.data?.generationStatus;
      const running = (s?: string) => s === 'Queued' || s === 'Processing';
      return running(es) || running(gs) ? 3000 : false;
    },
  });

  const { data: figuresList } = useQuery({
    queryKey: ['figures', id],
    queryFn: () => figures.list(id!),
    enabled: !!id,
    refetchInterval: (query) => {
      return (query.state.data?.length ?? 0) > 0 ? false : 5000;
    },
  });

  const uploadMutation = useMutation({
    mutationFn: (file: File) => documents.upload(id!, file),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['module', id] });
    },
  });

  const [downloadError, setDownloadError] = useState<string | null>(null);
  const [extractionError, setExtractionError] = useState<string | null>(null);
  const [generationError, setGenerationError] = useState<string | null>(null);
  const runExtractionMutation = useMutation({
    mutationFn: () => figures.runExtraction(id!),
    onSuccess: () => {
      setExtractionError(null);
      queryClient.invalidateQueries({ queryKey: ['module', id] });
    },
    onError: (err: Error) => setExtractionError(err.message),
  });

  const runGenerationMutation = useMutation({
    mutationFn: () => modules.generate(id!),
    onSuccess: () => {
      setGenerationError(null);
      queryClient.invalidateQueries({ queryKey: ['module', id] });
    },
    onError: (err: Error) => setGenerationError(err.message),
  });

  function handleFileChange(e: React.ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0];
    if (!file) return;
    uploadMutation.mutate(file);
    e.target.value = '';
  }

  async function handleDownload() {
    try {
      setDownloadError(null);
      await figures.downloadDocx(id!);
    } catch (err: unknown) {
      setDownloadError(err instanceof Error ? err.message : 'Download failed');
    }
  }

  const extractionStatus = mod?.extractionStatus ?? 'NotStarted';
  const isExtractionRunning = extractionStatus === 'Queued' || extractionStatus === 'Processing';
  const generationStatus = mod?.generationStatus ?? 'NotStarted';
  const isGenerationRunning = generationStatus === 'Queued' || generationStatus === 'Processing';
  const figureCount = figuresList?.length ?? 0;
  const keptFigureCount = figuresList?.filter(f => f.keep).length ?? 0;

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

          {/* Lecture Extraction Section — only shown once documents exist */}
          {mod.documents.length > 0 && (
            <div className="mt-10">
              <h2 className="text-xl font-semibold mb-4">Lecture Extraction</h2>

              {figureCount > 0 && (
                <p className="text-sm text-muted-foreground mb-4">
                  {keptFigureCount} of {figureCount} figures included
                </p>
              )}

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
                  disabled={isExtractionRunning || runExtractionMutation.isPending}
                >
                  {runExtractionMutation.isPending ? 'Starting...' : 'Run Extraction'}
                </Button>

                {extractionStatus === 'Ready' && (
                  <Button variant="outline" onClick={handleDownload}>
                    Download Lecture (.docx)
                  </Button>
                )}
              </div>
              {extractionError && (
                <p className="text-sm text-destructive mt-2">{extractionError}</p>
              )}
              {downloadError && (
                <p className="text-sm text-destructive mt-2">Download failed: {downloadError}</p>
              )}
            </div>
          )}

          {/* Generate Study Materials — only shown once extraction is Ready */}
          {extractionStatus === 'Ready' && (
            <div className="mt-10">
              <h2 className="text-xl font-semibold mb-4">Study Materials</h2>

              <div className="flex items-center gap-4 mb-4">
                <span className="text-sm text-muted-foreground">Status:</span>
                <Badge variant={generationStatusVariant(generationStatus)}>
                  {generationStatus}
                  {isGenerationRunning && (
                    <span className="ml-1 inline-block h-2 w-2 rounded-full bg-current animate-pulse" />
                  )}
                </Badge>
                {generationStatus === 'Failed' && (
                  <span className="text-xs text-destructive">Generation failed. Try again.</span>
                )}
                {generationStatus === 'Ready' && (
                  <span className="text-xs text-muted-foreground">Study materials are ready.</span>
                )}
              </div>

              <Button
                onClick={() => runGenerationMutation.mutate()}
                disabled={isGenerationRunning || runGenerationMutation.isPending}
              >
                {runGenerationMutation.isPending ? 'Starting...' : 'Generate Study Materials'}
              </Button>

              {generationError && (
                <p className="text-sm text-destructive mt-2">{generationError}</p>
              )}
            </div>
          )}
        </>
      )}
    </div>
  );
}
