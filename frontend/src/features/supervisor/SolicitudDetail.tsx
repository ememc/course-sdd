import React, { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { apiClient } from '../../shared/api/axios';
import { useAuth } from '../auth/AuthContext';
import { useQuery } from '@tanstack/react-query';
import { AuditTimeline } from '../solicitudes/AuditTimeline';
import './SolicitudDetail.css';
import '../admin/TipoSolicitud.css';

interface SolicitudDetail {
  id: string;
  tipo: { id: string; nombre: string; camposDefinicion: string };
  empleado: { id: string; nombre: string; area: string };
  supervisorAsignado: { id: string; nombre: string } | null;
  estado: string;
  camposDinamicos: string; // JSON
  comentarioSupervisor: string | null;
  fechaCreacion: string;
  fechaEnvio: string | null;
  fechaResolucion: string | null;
  ultimaModificacion: string;
  rowVersion: string;
}

export const SolicitudDetail: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { user } = useAuth();
  const [solicitud, setSolicitud] = useState<SolicitudDetail | null>(null);
  const [loading, setLoading] = useState<boolean>(true);
  const [comentario, setComentario] = useState<string>('');
  const [showRejectForm, setShowRejectForm] = useState<boolean>(false);
  const [resolving, setResolving] = useState<boolean>(false);

  const { data: auditoria = [], refetch: refetchAuditoria } = useQuery({
    queryKey: ['solicitudAuditoria', id],
    queryFn: async () => {
      const res = await apiClient.get(`/solicitudes/${id}/auditoria`);
      return res.data;
    },
    enabled: !!id,
  });

  const fetchDetail = async () => {
    setLoading(true);
    try {
      const res = await apiClient.get(`/solicitudes/${id}`);
      setSolicitud(res.data);
    } catch (err) {
      console.error('Error fetching request detail', err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchDetail();
  }, [id]);

  const handleAprobar = async () => {
    if (!window.confirm('¿Está seguro de que desea APROBAR esta solicitud?')) return;
    setResolving(true);
    try {
      await apiClient.post(`/solicitudes/${id}/aprobar`);
      fetchDetail();
      refetchAuditoria();
    } catch (err: any) {
      alert(err.response?.data?.detail || 'Error al aprobar la solicitud.');
    } finally {
      setResolving(false);
    }
  };

  const handleRechazarSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (comentario.length < 10) {
      alert('El comentario debe tener al menos 10 caracteres.');
      return;
    }

    setResolving(true);
    try {
      await apiClient.post(`/solicitudes/${id}/rechazar`, { comentario });
      setShowRejectForm(false);
      fetchDetail();
      refetchAuditoria();
    } catch (err: any) {
      alert(err.response?.data?.detail || 'Error al rechazar la solicitud.');
    } finally {
      setResolving(false);
    }
  };

  if (loading) return <div className="loading">Cargando detalles...</div>;
  if (!solicitud) return <div className="loading">No se encontró la solicitud.</div>;

  const dynamicValues = JSON.parse(solicitud.camposDinamicos || '{}');

  const isAssignedSupervisor = user?.rol === 'Supervisor' && solicitud.supervisorAsignado?.id === user.id;
  const isPendingReview = solicitud.estado === 'EnRevision';

  return (
    <div className="detail-container">
      <div className="detail-card">
        <header className="detail-header">
          <h2>Detalle de Solicitud</h2>
          <span className={`status-tag badge-${solicitud.estado.toLowerCase()}`}>
            {solicitud.estado}
          </span>
        </header>

        <section className="detail-section">
          <h3>Información General</h3>
          <div className="info-grid">
            <div>
              <span className="label">Colaborador:</span>
              <span className="value">{solicitud.empleado.nombre}</span>
            </div>
            <div>
              <span className="label">Área / Departamento:</span>
              <span className="value">{solicitud.empleado.area}</span>
            </div>
            <div>
              <span className="label">Tipo de Solicitud:</span>
              <span className="value">{solicitud.tipo.nombre}</span>
            </div>
            <div>
              <span className="label">Supervisor Asignado:</span>
              <span className="value">{solicitud.supervisorAsignado?.nombre || 'Pendiente de tomar'}</span>
            </div>
            <div>
              <span className="label">Fecha de Envío:</span>
              <span className="value">{solicitud.fechaEnvio ? new Date(solicitud.fechaEnvio).toLocaleString() : 'No enviada'}</span>
            </div>
            {solicitud.fechaResolucion && (
              <div>
                <span className="label">Fecha de Resolución:</span>
                <span className="value">{new Date(solicitud.fechaResolucion).toLocaleString()}</span>
              </div>
            )}
          </div>
        </section>

        <section className="detail-section">
          <h3>Campos de la Solicitud</h3>
          <div className="dynamic-values-list">
            {Object.keys(dynamicValues).length === 0 ? (
              <p className="no-fields">No hay campos dinámicos ingresados.</p>
            ) : (
              Object.keys(dynamicValues).map(key => (
                <div key={key} className="dynamic-field-item">
                  <span className="field-name">{key}:</span>
                  <span className="field-value">{dynamicValues[key]?.toString()}</span>
                </div>
              ))
            )}
          </div>
        </section>

        {solicitud.comentarioSupervisor && (
          <section className="detail-section comment-section">
            <h3>Retroalimentación del Supervisor</h3>
            <p className="supervisor-comment">{solicitud.comentarioSupervisor}</p>
          </section>
        )}

        <AuditTimeline solicitudId={id!} />

        {isAssignedSupervisor && isPendingReview && (
          <div className="review-actions-container">
            {!showRejectForm ? (
              <div className="action-buttons-row">
                <button 
                  className="btn-approve" 
                  disabled={resolving} 
                  onClick={handleAprobar}
                >
                  Aprobar Solicitud
                </button>
                <button 
                  className="btn-reject-trigger" 
                  disabled={resolving} 
                  onClick={() => setShowRejectForm(true)}
                >
                  Rechazar Solicitud
                </button>
              </div>
            ) : (
              <form onSubmit={handleRechazarSubmit} className="reject-form">
                <div className="form-group">
                  <label htmlFor="comentario">Comentario de Rechazo (Mínimo 10 caracteres) <span className="required">*</span></label>
                  <textarea
                    id="comentario"
                    rows={4}
                    value={comentario}
                    onChange={(e) => setComentario(e.target.value)}
                    placeholder="Escriba los motivos del rechazo..."
                    required
                  />
                </div>
                <div className="reject-actions">
                  <button 
                    type="button" 
                    className="btn-cancel" 
                    onClick={() => setShowRejectForm(false)}
                  >
                    Regresar
                  </button>
                  <button 
                    type="submit" 
                    className="btn-reject-submit"
                    disabled={resolving}
                  >
                    Confirmar Rechazo
                  </button>
                </div>
              </form>
            )}
          </div>
        )}

        {/* Bitácora de Auditoría (Timeline) */}
        <section className="detail-section" style={{ marginTop: '2rem' }}>
          <h3>Bitácora de Auditoría</h3>
          {auditoria.length === 0 ? (
            <p style={{ color: '#94a3b8', fontSize: '0.85rem' }}>No hay registros de auditoría para esta solicitud.</p>
          ) : (
            <div className="timeline">
              {auditoria.map((item: any) => {
                let statusClass = '';
                if (item.estadoNuevo === 'Aprobada') statusClass = 'approved';
                else if (item.estadoNuevo === 'Rechazada') statusClass = 'rejected';
                else if (item.accion === 'Tomada') statusClass = 'taken';
                else if (item.accion === 'Creada' || item.accion === 'Enviada') statusClass = 'submitted';

                let metadataText = '';
                if (item.metadata) {
                  try {
                    const parsed = JSON.parse(item.metadata);
                    if (parsed.supervisorAnterior && parsed.supervisorNuevo) {
                      metadataText = `Reasignada. De ID: ${parsed.supervisorAnterior} a ID: ${parsed.supervisorNuevo}`;
                    } else {
                      metadataText = item.metadata;
                    }
                  } catch (e) {
                    metadataText = item.metadata;
                  }
                }

                return (
                  <div key={item.id} className={`timeline-item ${statusClass}`}>
                    <div className="timeline-content">
                      <div className="timeline-header">
                        <strong>{item.usuarioNombre} ({item.usuarioRol})</strong>
                        <span>{new Date(item.fechaHora).toLocaleString()}</span>
                      </div>
                      <div className="timeline-body">
                        {item.accion === 'Reasignada' ? (
                          <span>Reasignada a un nuevo supervisor</span>
                        ) : (
                          <span>Estado: <strong>{item.estadoNuevo}</strong> (Acción: {item.accion})</span>
                        )}
                      </div>
                      {metadataText && (
                        <div className="timeline-meta">
                          {metadataText}
                        </div>
                      )}
                    </div>
                  </div>
                );
              })}
            </div>
          )}
        </section>

        <div className="back-action">
          <button className="btn-back" onClick={() => navigate('/')}>
            Regresar a la Lista
          </button>
        </div>
      </div>
    </div>
  );
};
