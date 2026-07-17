import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { AuthProvider, useAuth } from './features/auth/AuthContext';
import { Login } from './features/auth/Login';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { SolicitudForm } from './features/solicitudes/SolicitudForm';
import { SolicitudList } from './features/solicitudes/SolicitudList';
import { SupervisorDashboard } from './features/supervisor/SupervisorDashboard';
import { SolicitudDetail } from './features/supervisor/SolicitudDetail';
import { Layout } from './shared/components/Layout';
import { UsuariosAdmin } from './features/admin/usuarios/UsuariosAdmin';
import { TiposSolicitudAdmin } from './features/admin/tipos-solicitud/TiposSolicitudAdmin';
import './App.css';

const queryClient = new QueryClient();

const ProtectedRoute = ({ children }: { children: React.ReactNode }) => {
  const { isAuthenticated } = useAuth();
  return isAuthenticated ? <Layout>{children}</Layout> : <Navigate to="/login" replace />;
};

const RootPage = () => {
  const { user } = useAuth();
  if (user?.rol === 'Supervisor') {
    return <SupervisorDashboard />;
  }
  return <SolicitudList />;
};

function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <AuthProvider>
        <BrowserRouter>
          <Routes>
            <Route path="/login" element={<Login />} />
            <Route
              path="/"
              element={
                <ProtectedRoute>
                  <RootPage />
                </ProtectedRoute>
              }
            />
            <Route
              path="/nueva"
              element={
                <ProtectedRoute>
                  <SolicitudForm />
                </ProtectedRoute>
              }
            />
            <Route
              path="/editar/:id"
              element={
                <ProtectedRoute>
                  <SolicitudForm />
                </ProtectedRoute>
              }
            />
            <Route
              path="/solicitudes/:id"
              element={
                <ProtectedRoute>
                  <SolicitudDetail />
                </ProtectedRoute>
              }
            />
            <Route
              path="/admin/usuarios"
              element={
                <ProtectedRoute>
                  <UsuariosAdmin />
                </ProtectedRoute>
              }
            />
            <Route
              path="/admin/tipos-solicitud"
              element={
                <ProtectedRoute>
                  <TiposSolicitudAdmin />
                </ProtectedRoute>
              }
            />
            <Route path="*" element={<Navigate to="/" replace />} />
          </Routes>
        </BrowserRouter>
      </AuthProvider>
    </QueryClientProvider>
  );
}

export default App;
