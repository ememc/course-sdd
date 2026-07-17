import React, { useState } from 'react';
import { Link, useNavigate, useLocation } from 'react-router-dom';
import { useAuth } from '../../features/auth/AuthContext';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { apiClient } from '../api/axios';
import './Layout.css';

interface Notificacion {
  id: string;
  tipo: 'NuevaSolicitud' | 'SolicitudAprobada' | 'SolicitudRechazada' | 'SolicitudReasignada' | string;
  contenido: string;
  leida: boolean;
  fechaGeneracion: string;
  solicitud: { id: string } | null;
}

interface NotificationsResponse {
  data: Notificacion[];
  noLeidas: number;
}

export const Layout: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const { user, logout } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const queryClient = useQueryClient();
  const [showNotifications, setShowNotifications] = useState(false);

  // Fetch notifications with TanStack Query and 30s polling
  const { data: notificationsData } = useQuery<NotificationsResponse>({
    queryKey: ['notifications'],
    queryFn: async () => {
      const res = await apiClient.get('/notificaciones');
      return res.data;
    },
    enabled: !!user,
    refetchInterval: 30000, // 30 seconds polling (SC-002)
  });

  const notifications = notificationsData?.data || [];
  const noLeidas = notificationsData?.noLeidas || 0;

  // Mark all as read mutation
  const markAllReadMutation = useMutation({
    mutationFn: async () => {
      await apiClient.post('/notificaciones/marcar-todas-leidas');
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['notifications'] });
    },
  });

  // Mark single as read mutation
  const markSingleReadMutation = useMutation({
    mutationFn: async (id: string) => {
      await apiClient.post(`/notificaciones/${id}/marcar-leida`);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['notifications'] });
    },
  });

  const handleNotificationClick = async (notif: Notificacion) => {
    if (!notif.leida) {
      await markSingleReadMutation.mutateAsync(notif.id);
    }
    setShowNotifications(false);
    if (notif.solicitud?.id) {
      // Direct navigation to detail view
      if (user?.rol === 'Supervisor') {
        navigate(`/solicitudes/${notif.solicitud.id}`);
      } else {
        // For employee, wait: employee list doesn't have detail links or does it?
        // Let's allow detail view for employees too, we will link it or create detail page!
        navigate(`/solicitudes/${notif.solicitud.id}`);
      }
    }
  };

  const getNotifIcon = (tipo: string) => {
    switch (tipo) {
      case 'NuevaSolicitud': return '➕';
      case 'SolicitudAprobada': return '✅';
      case 'SolicitudRechazada': return '❌';
      case 'SolicitudReasignada': return '🔄';
      default: return '🔔';
    }
  };

  return (
    <div className="app-layout">
      <nav className="navbar">
        <div className="nav-container">
          <div className="nav-brand" onClick={() => navigate('/')}>
            <span className="brand-logo">🌌</span>
            <span className="brand-name">AntiGravity SDD</span>
          </div>

          <div className="nav-links">
            {user?.rol === 'Empleado' && (
              <>
                <Link to="/" className={location.pathname === '/' ? 'active' : ''}>Mis Solicitudes</Link>
                <Link to="/nueva" className={location.pathname === '/nueva' ? 'active' : ''}>Nueva Solicitud</Link>
              </>
            )}

            {user?.rol === 'Supervisor' && (
              <>
                <Link to="/" className={location.pathname === '/' ? 'active' : ''}>Panel Supervisor</Link>
              </>
            )}

            {user?.rol === 'Administrador' && (
              <>
                <Link to="/" className={location.pathname === '/' ? 'active' : ''}>Todas las Solicitudes</Link>
                <Link to="/admin/usuarios" className={location.pathname.startsWith('/admin/usuarios') ? 'active' : ''}>Gestión de Usuarios</Link>
                <Link to="/admin/tipos" className={location.pathname.startsWith('/admin/tipos') ? 'active' : ''}>Tipos de Solicitud</Link>
              </>
            )}
          </div>

          <div className="nav-actions">
            {/* Notification Bell Dropdown */}
            <div className="notification-bell-container">
              <button 
                className="nav-action-btn bell-btn" 
                onClick={() => setShowNotifications(!showNotifications)}
                title="Notificaciones"
              >
                🔔
                {noLeidas > 0 && <span className="bell-badge">{noLeidas}</span>}
              </button>

              {showNotifications && (
                <div className="notifications-dropdown">
                  <div className="dropdown-header">
                    <h3>Notificaciones ({noLeidas} sin leer)</h3>
                    {noLeidas > 0 && (
                      <button 
                        className="mark-all-btn"
                        onClick={() => markAllReadMutation.mutate()}
                        disabled={markAllReadMutation.isPending}
                      >
                        Marcar todas leídas
                      </button>
                    )}
                  </div>
                  <div className="dropdown-list">
                    {notifications.length === 0 ? (
                      <div className="empty-notifications">No tienes notificaciones</div>
                    ) : (
                      notifications.map(n => (
                        <div 
                          key={n.id} 
                          className={`notification-item ${n.leida ? 'read' : 'unread'}`}
                          onClick={() => handleNotificationClick(n)}
                        >
                          <span className="notif-icon">{getNotifIcon(n.tipo)}</span>
                          <div className="notif-content">
                            <p className="notif-text">{n.contenido}</p>
                            <span className="notif-time">{new Date(n.fechaGeneracion).toLocaleString()}</span>
                          </div>
                        </div>
                      ))
                    )}
                  </div>
                </div>
              )}
            </div>

            {/* User Profile Info & Logout */}
            <div className="user-profile-badge">
              <span className="user-avatar">{user?.nombre?.charAt(0).toUpperCase()}</span>
              <div className="user-details">
                <span className="user-name">{user?.nombre}</span>
                <span className="user-role">{user?.rol}</span>
              </div>
              <button 
                className="logout-btn"
                onClick={() => { logout(); navigate('/login'); }}
                title="Cerrar sesión"
              >
                🚪
              </button>
            </div>
          </div>
        </div>
      </nav>

      <main className="main-content">
        <div className="content-container">
          {children}
        </div>
      </main>
    </div>
  );
};
