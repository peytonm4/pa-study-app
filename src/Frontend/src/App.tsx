import { createBrowserRouter, RouterProvider, Navigate } from 'react-router-dom';
import RootLayout from '@/layouts/RootLayout';
import ModuleListPage from '@/pages/ModuleListPage';
import ModuleDetailPage from '@/pages/ModuleDetailPage';

const router = createBrowserRouter([
  {
    path: '/',
    element: <RootLayout />,
    children: [
      { index: true, element: <Navigate to="/modules" replace /> },
      { path: 'modules', element: <ModuleListPage /> },
      { path: 'modules/:id', element: <ModuleDetailPage /> },
    ],
  },
]);

export default function App() {
  return <RouterProvider router={router} />;
}
