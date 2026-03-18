import { useRef } from 'react';
import { Link, useParams } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { modules } from '@/api/modules';
import { documents, type DocumentStatus } from '@/api/documents';
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

export default function ModuleDetailPage() {
  const { id } = useParams<{ id: string }>();
  const queryClient = useQueryClient();
  const fileInputRef = useRef<HTMLInputElement>(null);

  const { data: mod, isLoading } = useQuery({
    queryKey: ['module', id],
    queryFn: () => modules.get(id!),
    enabled: !!id,
  });

  const uploadMutation = useMutation({
    mutationFn: (file: File) => documents.upload(id!, file),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['module', id] });
    },
  });

  function handleFileChange(e: React.ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0];
    if (!file) return;
    uploadMutation.mutate(file);
    e.target.value = '';
  }

  return (
    <div className="max-w-2xl mx-auto py-8 px-4">
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
        </>
      )}
    </div>
  );
}
