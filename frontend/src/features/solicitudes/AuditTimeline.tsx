import React from 'react';
import { useQuery } from '@tanstack/react-query';
import { apiClient } from '../../shared/api/axios';
import './AuditTimeline.css';

interface EventoAuditoria {
  id: string;
  usuario: { id: string; nombre: string };
  estadoAnterior: string | null;
  estadoNuevo: string;
  accion: string;
  fechaHora: string;
  metadata: { supervisorAnterior?: string; supervisorNuevo?: string } | any | null;
}

interface AuditTimelineProps {
  solicitudId: string;
}

export const AuditTimeline: React.FC<AuditTimelineProps> = ({ solicitudId }) => {
  const { data: response, isLoading, error } = useQuery<{ data: EventoAuditoria[] }>({
    queryKey: ['solicitud-auditoria', solicitudId],
    queryFn: async () => {
      const res = await apiClient.get(`/solicitudes/${solicitudId}/auditoria`);
      return res.data;
    },
    enabled: !!solicitudId
  });

  if (isLoading) return <div className="timeline-loading">Cargando historial de auditoría...</div>;
  if (error) return <div className="timeline-error">Error al cargar historial de auditoría.</div>;

  const eventos = response?.data || [];

  const getActionBadgeClass = (accion: string) => {
    switch (accion) {
      case 'Creada': return 'act-creada';
      case 'Enviada': return 'act-enviada';
      case 'Tomada': return 'act-tomada';
      case 'Aprobada': return 'act-aprobada';
      case 'Rechazada': return 'act-rechazada';
      case 'Cancelada': return 'act-cancelada';
      case 'Reasignada': return 'act-reasignada';
      default: return 'act-default';
    }
  };

  const getActionIcon = (accion: string) => {
    switch (accion) {
      case 'Creada': return '📝';
      case 'Enviada': return '📤';
      case 'Tomada': return '🔍';
      case 'Aprobada': return '✅';
      case 'Rechazada': return '❌';
      case 'Cancelada': return '🚫';
      case 'Reasignada': return '🔄';
      default: return '⚙️';
    }
  };

  return (
    <div className="audit-timeline-container">
      <h3>Historial de Auditoría (Inmutable)</h3>
      {eventos.length === 0 ? (
        <p className="no-events">No hay eventos de auditoría registrados.</p>
      ) : (
        <div className="timeline">
          {eventos.map((ev, index) => (
            <div key={ev.id} className="timeline-item">
              <div className="timeline-icon">
                {getActionIcon(ev.accion)}
              </div>
              <div className="timeline-content">
                <div className="timeline-header">
                  <span className={`action-badge ${getActionBadgeClass(ev.accion)}`}>
                    {ev.accion}
                  </span>
                  <span className="timeline-time">
                    {new Date(ev.fechaHora).toLocaleString()}
                  </span>
                </div>

                <p className="timeline-desc">
                  Realizado por: <strong>{ev.usuario.nombre}</strong>
                </p>

                {ev.estadoAnterior && (
                  <p className="timeline-states">
                    Estado: <span className="state-old">{ev.estadoAnterior}</span> &rarr; <span className="state-new">{ev.estadoNuevo}</span>
                  </p>
                )}

                {ev.accion === 'Reasignada' && ev.metadata && (
                  <div className="timeline-metadata">
                    <p>Reasignación de supervisor en bloque por administrador.</p>
                    {/* Wait, the metadata might contain the IDs or if we want we can display general reassign note */}
                  </div>
                )}
              </div>
              {index < eventos.length - 1 && <div className="timeline-line"></div>}
            </div>
          ))}
        </div>
      )}
    </div>
  );
};
