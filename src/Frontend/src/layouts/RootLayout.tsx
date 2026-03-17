import { Outlet } from 'react-router-dom';

export default function RootLayout() {
  return (
    <div className="flex min-h-screen">
      <nav className="w-56 border-r bg-muted/40 p-4">
        <p className="text-sm text-muted-foreground">Sidebar placeholder</p>
      </nav>
      <main className="flex-1 p-6">
        <Outlet />
      </main>
    </div>
  );
}
