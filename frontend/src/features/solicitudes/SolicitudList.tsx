import React, { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';
import { apiClient } from '../../shared/api/axios';
import './SolicitudList.css';

interface Solicitud {
  id: string;
  tipo: { id: string; nombre: string };
  empleado: { id: string; nombre: string };
  supervisorAsignado: { id: string; nombre: string } | null;
  estado: 'Borrador' | 'Enviada' | 'EnRevision' | 'Aprobada' | 'Rechazada' | 'Cancelada';
  fechaCreacion: string;
  fechaEnvio: string | null;
  fechaResolucion: string | null;
  rowVersion: string;
}

export const SolicitudList: React.FC = () => {
  const { user } = useAuth();
  const navigate = useNavigate();
  const [solicitudes, setSolicitudes] = useState<Solicitud[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [actionLoadingId, setActionLoadingId] = useState<string | null>(null);

  const fetchSolicitudes = async () => {
    setLoading(true);
    try {
      const res = await apiClient.get('/solicitudes');
      setSolicitudes(res.data.data || []);
    } catch (err) {
      console.error('Error fetching requests', err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchSolicitudes();
  }, []);

  const handleCancelar = async (id: string) => {
    if (!window.confirm('¿Está seguro de que desea cancelar esta solicitud?')) return;
    
    setActionLoadingId(id);
    try {
      await apiClient.post(`/solicitudes/${id}/cancelar`);
      fetchSolicitudes();
    } catch (err: any) {
      alert(err.response?.data?.detail || 'Error al cancelar la solicitud.');
    } finally {
      setActionLoadingId(null);
    }
  };

  const getStatusBadgeClass = (estado: string) => {
    switch (estado) {
      case 'Borrador': return 'badge-borrador';
      case 'Enviada': return 'badge-enviada';
      case 'EnRevision': return 'badge-revision';
      case 'Aprobada': return 'badge-aprobada';
      case 'Rechazada': return 'badge-rechazada';
      case 'Cancelada': return 'badge-cancelada';
      default: return 'badge-default';
    }
  };

  return (
    <div className="dashboard-container">
      <header className="dashboard-header">
        <div className="user-info">
          <h1>{user?.rol === 'Administrador' ? 'Todas las Solicitudes' : 'Mis Solicitudes'}</h1>
        </div>
        {user?.rol === 'Empleado' && (
          <div className="header-actions">
            <button className="new-btn" onClick={() => navigate('/nueva')}>
              Nueva Solicitud
            </button>
          </div>
        )}
      </header>

      {loading ? (
        <div className="loading-list">Cargando solicitudes...</div>
      ) : solicitudes.length === 0 ? (
        <div className="empty-state">
          <h3>No tienes solicitudes registradas</h3>
          <p>Comienza creando una nueva solicitud desde el botón de la parte superior.</p>
        </div>
      ) : (
        <div className="table-responsive">
          <table className="solicitudes-table">
            <thead>
              <tr>
                <th>Tipo</th>
                <th>Fecha de Creación</th>
                <th>Supervisor Asignado</th>
                <th>Estado</th>
                <th>Acciones</th>
              </tr>
            </thead>
            <tbody>
              {solicitudes.map(sol => (
                <tr key={sol.id}>
                  <td className="tipo-col">{sol.tipo.nombre}</td>
                  <td>{new Date(sol.fechaCreacion).toLocaleDateString()}</td>
                  <td>{sol.supervisorAsignado?.nombre || 'Pendiente'}</td>
                  <td>
                    <span className={`badge ${getStatusBadgeClass(sol.estado)}`}>
                      {sol.estado}
                    </span>
                  </td>
                  <td>
                    <div className="action-buttons">
                      {sol.estado === 'Borrador' && (
                        <button 
                          className="edit-action-btn"
                          onClick={() => navigate(`/editar/${sol.id}`)}
                        >
                          Editar
                        </button>
                      )}
                      {sol.estado === 'Enviada' && (
                        <button 
                          className="cancel-action-btn"
                          disabled={actionLoadingId === sol.id}
                          onClick={() => handleCancelar(sol.id)}
                        >
                          {actionLoadingId === sol.id ? 'Cancelando...' : 'Cancelar'}
                        </button>
                      )}
                      {sol.estado !== 'Borrador' && sol.estado !== 'Enviada' && (
                        <span className="no-actions">Sin acciones</span>
                      )}
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
};
