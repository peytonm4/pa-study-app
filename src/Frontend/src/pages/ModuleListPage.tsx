import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { modules, type Module } from '@/api/modules';
import { Badge } from '@/components/ui/badge';
import { Input } from '@/components/ui/input';
import { Button } from '@/components/ui/button';

export default function ModuleListPage() {
  const queryClient = useQueryClient();
  const [name, setName] = useState('');

  const { data, isLoading } = useQuery({
    queryKey: ['modules'],
    queryFn: modules.list,
  });

  const createMutation = useMutation({
    mutationFn: (n: string) => modules.create(n),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['modules'] });
      setName('');
    },
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => modules.delete(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['modules'] }),
  });

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    const trimmed = name.trim();
    if (!trimmed) return;
    createMutation.mutate(trimmed);
  }

  return (
    <div className="max-w-2xl mx-auto py-8 px-4">
      <h1 className="text-2xl font-semibold mb-6">Modules</h1>

      <form onSubmit={handleSubmit} className="flex gap-2 mb-8">
        <Input
          value={name}
          onChange={e => setName(e.target.value)}
          placeholder="Module name"
          className="flex-1"
        />
        <Button type="submit" disabled={createMutation.isPending || !name.trim()}>
          {createMutation.isPending ? 'Creating...' : 'Create Module'}
        </Button>
      </form>

      {isLoading && <p className="text-muted-foreground">Loading...</p>}

      {!isLoading && data?.length === 0 && (
        <p className="text-muted-foreground">No modules yet. Create one above.</p>
      )}

      {!isLoading && data && data.length > 0 && (
        <ul className="divide-y divide-border rounded-md border">
          {data.map(mod => (
            <li key={mod.id} className="flex items-center justify-between px-4 py-3 hover:bg-muted/40 transition-colors">
              <Link to={`/modules/${mod.id}`} className="font-medium hover:underline flex-1">
                {mod.name}
              </Link>
              <div className="flex items-center gap-4 ml-4">
                <Badge variant={mod.status === 'Ready' ? 'default' : 'secondary'}>
                  {mod.status}
                </Badge>
                <span className="text-sm text-muted-foreground">
                  {new Date(mod.createdAt).toLocaleDateString()}
                </span>
                <Button
                  variant="ghost"
                  size="sm"
                  disabled={deleteMutation.isPending}
                  onClick={() => deleteMutation.mutate(mod.id)}
                >
                  Delete
                </Button>
              </div>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
