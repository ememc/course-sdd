import React, { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';
import { apiClient } from '../../shared/api/axios';
import './SupervisorDashboard.css';

interface Solicitud {
  id: string;
  tipo: { id: string; nombre: string };
  empleado: { id: string; nombre: string };
  supervisorAsignado: { id: string; nombre: string } | null;
  estado: 'Borrador' | 'Enviada' | 'EnRevision' | 'Aprobada' | 'Rechazada' | 'Cancelada';
  fechaCreacion: string;
  fechaEnvio: string | null;
  rowVersion: string;
}

export const SupervisorDashboard: React.FC = () => {
  const { user } = useAuth();
  const navigate = useNavigate();
  const [pendientes, setPendientes] = useState<Solicitud[]>([]);
  const [misRevisiones, setMisRevisiones] = useState<Solicitud[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [actionLoadingId, setActionLoadingId] = useState<string | null>(null);

  const fetchSolicitudes = async () => {
    setLoading(true);
    try {
      // Fetch requests in "Enviada" state
      const resEnviadas = await apiClient.get('/solicitudes?estado=Enviada');
      setPendientes(resEnviadas.data.data || []);

      // Fetch requests in "EnRevision" state
      const resRevision = await apiClient.get('/solicitudes?estado=EnRevision');
      setMisRevisiones(resRevision.data.data || []);
    } catch (err) {
      console.error('Error fetching supervisor dashboard data', err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchSolicitudes();
  }, []);

  const handleTomar = async (sol: Solicitud) => {
    setActionLoadingId(sol.id);
    try {
      await apiClient.post(
        `/solicitudes/${sol.id}/tomar`,
        {},
        { headers: { 'If-Match': sol.rowVersion } }
      );
      fetchSolicitudes();
    } catch (err: any) {
      if (err.response?.status === 409) {
        alert('Conflict: La solicitud ya fue tomada por otro supervisor.');
      } else {
        alert(err.response?.data?.detail || 'Error al tomar la solicitud.');
      }
      fetchSolicitudes();
    } finally {
      setActionLoadingId(null);
    }
  };

  return (
    <div className="supervisor-dashboard">
      <header className="dashboard-header">
        <div className="user-info">
          <h1>Panel de Supervisor</h1>
          <p>Área Organizacional: <strong>{user?.areaNombre}</strong></p>
        </div>
      </header>

      {loading ? (
        <div className="loading-list">Cargando panel...</div>
      ) : (
        <div className="dashboard-sections">
          {/* section 1: Pendientes de Tomar */}
          <section className="dashboard-section">
            <h2>Pendientes por Tomar (Área)</h2>
            {pendientes.length === 0 ? (
              <p className="no-items">No hay solicitudes pendientes en tu área.</p>
            ) : (
              <div className="table-responsive">
                <table className="solicitudes-table">
                  <thead>
                    <tr>
                      <th>Empleado</th>
                      <th>Tipo</th>
                      <th>Fecha de Envío</th>
                      <th>Acción</th>
                    </tr>
                  </thead>
                  <tbody>
                    {pendientes.map(sol => (
                      <tr key={sol.id}>
                        <td>{sol.empleado.nombre}</td>
                        <td>{sol.tipo.nombre}</td>
                        <td>{sol.fechaEnvio ? new Date(sol.fechaEnvio).toLocaleDateString() : 'N/A'}</td>
                        <td>
                          <button
                            className="take-btn"
                            disabled={actionLoadingId === sol.id}
                            onClick={() => handleTomar(sol)}
                          >
                            {actionLoadingId === sol.id ? 'Tomando...' : 'Tomar Solicitud'}
                          </button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </section>

          {/* section 2: Mis Revisiones */}
          <section className="dashboard-section">
            <h2>Mis Revisiones (En Curso)</h2>
            {misRevisiones.length === 0 ? (
              <p className="no-items">No tienes revisiones en curso.</p>
            ) : (
              <div className="table-responsive">
                <table className="solicitudes-table">
                  <thead>
                    <tr>
                      <th>Empleado</th>
                      <th>Tipo</th>
                      <th>Fecha de Envío</th>
                      <th>Acción</th>
                    </tr>
                  </thead>
                  <tbody>
                    {misRevisiones.map(sol => (
                      <tr key={sol.id}>
                        <td>{sol.empleado.nombre}</td>
                        <td>{sol.tipo.nombre}</td>
                        <td>{sol.fechaEnvio ? new Date(sol.fechaEnvio).toLocaleDateString() : 'N/A'}</td>
                        <td>
                          <button
                            className="review-btn"
                            onClick={() => navigate(`/solicitudes/${sol.id}`)}
                          >
                            Revisar Detalles
                          </button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </section>
        </div>
      )}
    </div>
  );
};
