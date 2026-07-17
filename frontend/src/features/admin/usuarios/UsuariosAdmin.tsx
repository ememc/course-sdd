import React, { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { apiClient } from '../../../shared/api/axios';
import './UsuariosAdmin.css';

interface Usuario {
  id: string;
  nombre: string;
  email: string;
  rol: string;
  area: string;
  supervisorId: string | null;
  supervisorNombre: string | null;
  activo: boolean;
  fechaCreacion: string;
}

interface PaginatedUsersResponse {
  data: Usuario[];
  total: number;
  page: number;
  pageSize: number;
}

interface PendingRequest {
  id: string;
  empleadoNombre: string;
  tipoNombre: string;
  estado: string;
  fechaCreacion: string;
}

interface Supervisor {
  id: string;
  nombre: string;
}

export const UsuariosAdmin: React.FC = () => {
  const queryClient = useQueryClient();
  const [roleFilter, setRoleFilter] = useState<string>('');
  const [statusFilter, setStatusFilter] = useState<string>('');
  const [page, setPage] = useState<number>(1);
  const [pageSize] = useState<number>(15);

  // States for Reassignment / Deactivation modal
  const [showDeactivateModal, setShowDeactivateModal] = useState<boolean>(false);
  const [deactivatingUser, setDeactivatingUser] = useState<Usuario | null>(null);
  const [pendingRequests, setPendingRequests] = useState<PendingRequest[]>([]);
  const [targetSupervisorId, setTargetSupervisorId] = useState<string>('');

  // Fetch paginated users
  const { data: usersResponse, isLoading } = useQuery<PaginatedUsersResponse>({
    queryKey: ['users-admin', roleFilter, statusFilter, page],
    queryFn: async () => {
      const params: any = { page, pageSize };
      if (roleFilter) params.rol = roleFilter;
      if (statusFilter) params.activo = statusFilter === 'true';
      const res = await apiClient.get('/usuarios', { params });
      return res.data;
    }
  });

  // Fetch active supervisors for dropdown selectors
  const { data: supervisorsResponse } = useQuery<{ data: Supervisor[] }>({
    queryKey: ['supervisors-list'],
    queryFn: async () => {
      const res = await apiClient.get('/usuarios/supervisores?soloActivos=true');
      return res;
    }
  });

  const supervisors = (supervisorsResponse?.data as unknown as Supervisor[]) || [];
  const users = usersResponse?.data || [];
  const total = usersResponse?.total || 0;
  const totalPages = Math.ceil(total / pageSize);

  // Mutation to reassign default supervisor (organizational level)
  const reassignDefaultSupervisorMutation = useMutation({
    mutationFn: async ({ employeeId, supervisorId }: { employeeId: string; supervisorId: string | null }) => {
      await apiClient.patch(`/usuarios/${employeeId}/supervisor`, { nuevoSupervisorId: supervisorId });
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['users-admin'] });
      alert('Supervisor predeterminado actualizado exitosamente.');
    },
    onError: (err: any) => {
      alert(err.response?.data?.detail || 'Error al reasignar supervisor.');
    }
  });

  // Mutation to deactivate user
  const deactivateUserMutation = useMutation({
    mutationFn: async (id: string) => {
      return await apiClient.post(`/usuarios/${id}/desactivar`);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['users-admin'] });
      alert('Usuario desactivado exitosamente.');
      setShowDeactivateModal(false);
      setDeactivatingUser(null);
    },
    onError: async (err: any) => {
      if (err.response?.status === 409) {
        const data = err.response.data;
        // In RFC 7807 we might have different structure: data.solicitudesPendientes or data.errors
        setPendingRequests(data.solicitudesPendientes || []);
        setShowDeactivateModal(true);
      } else {
        alert(err.response?.data?.detail || 'Error al desactivar el usuario.');
      }
    }
  });

  // Mutation to reassign requests in bulk
  const bulkReassignMutation = useMutation({
    mutationFn: async ({ requestIds, supervisorId }: { requestIds: string[]; supervisorId: string }) => {
      await apiClient.post('/solicitudes/reasignar', {
        solicitudIds: requestIds,
        nuevoSupervisorId: supervisorId
      });
    }
  });

  const handleDeactivateClick = (u: Usuario) => {
    setDeactivatingUser(u);
    deactivateUserMutation.mutate(u.id);
  };

  const handleBulkReassignAndDeactivate = async () => {
    if (!targetSupervisorId) {
      alert('Por favor seleccione un supervisor de destino.');
      return;
    }
    if (!deactivatingUser) return;

    try {
      const requestIds = pendingRequests.map(r => r.id);
      await bulkReassignMutation.mutateAsync({
        requestIds,
        supervisorId: targetSupervisorId
      });

      await deactivateUserMutation.mutateAsync(deactivatingUser.id);
    } catch (err: any) {
      alert('Error en el proceso de reasignación y desactivación: ' + (err.response?.data?.detail || err.message));
    }
  };

  return (
    <div className="admin-usuarios-container">
      <h2>Gestión de Usuarios (Administrador)</h2>

      {/* Filters bar */}
      <div className="card filters-card">
        <div className="filters-grid">
          <div className="filter-group">
            <label>Filtrar por Rol</label>
            <select value={roleFilter} onChange={e => { setRoleFilter(e.target.value); setPage(1); }}>
              <option value="">Todos los Roles</option>
              <option value="Empleado">Empleado</option>
              <option value="Supervisor">Supervisor</option>
              <option value="Administrador">Administrador</option>
            </select>
          </div>

          <div className="filter-group">
            <label>Filtrar por Estado</label>
            <select value={statusFilter} onChange={e => { setStatusFilter(e.target.value); setPage(1); }}>
              <option value="">Todos los Estados</option>
              <option value="true">Activo</option>
              <option value="false">Inactivo</option>
            </select>
          </div>
        </div>
      </div>

      {isLoading ? (
        <div className="loading-users">Cargando usuarios...</div>
      ) : (
        <div className="card table-card">
          <div className="table-responsive">
            <table className="usuarios-table">
              <thead>
                <tr>
                  <th>Nombre</th>
                  <th>Email</th>
                  <th>Rol</th>
                  <th>Área / Departamento</th>
                  <th>Supervisor Asignado</th>
                  <th>Estado</th>
                  <th>Acción</th>
                </tr>
              </thead>
              <tbody>
                {users.map(u => (
                  <tr key={u.id}>
                    <td><strong>{u.nombre}</strong></td>
                    <td>{u.email}</td>
                    <td><span className={`role-badge role-${u.rol.toLowerCase()}`}>{u.rol}</span></td>
                    <td>{u.area}</td>
                    <td>
                      {u.rol === 'Empleado' ? (
                        <select
                          className="supervisor-select"
                          value={u.supervisorId || ''}
                          onChange={e => reassignDefaultSupervisorMutation.mutate({
                            employeeId: u.id,
                            supervisorId: e.target.value || null
                          })}
                        >
                          <option value="">-- Sin Supervisor --</option>
                          {supervisors
                            .filter(s => s.id !== u.id)
                            .map(s => (
                              <option key={s.id} value={s.id}>{s.nombre}</option>
                            ))}
                        </select>
                      ) : (
                        <span className="na-text">—</span>
                      )}
                    </td>
                    <td>
                      <span className={`status-dot ${u.activo ? 'dot-active' : 'dot-inactive'}`}></span>
                      {u.activo ? 'Activo' : 'Inactivo'}
                    </td>
                    <td>
                      {u.activo && u.rol !== 'Administrador' ? (
                        <button
                          className="deactivate-btn"
                          onClick={() => handleDeactivateClick(u)}
                        >
                          Desactivar
                        </button>
                      ) : (
                        <span className="na-text">No modificable</span>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          {totalPages > 1 && (
            <div className="pagination">
              <button disabled={page <= 1} onClick={() => setPage(page - 1)}>Anterior</button>
              <span>Página {page} de {totalPages}</span>
              <button disabled={page >= totalPages} onClick={() => setPage(page + 1)}>Siguiente</button>
            </div>
          )}
        </div>
      )}

      {/* Conflict Resolution Modal for Supervisor Deactivation */}
      {showDeactivateModal && deactivatingUser && (
        <div className="modal-overlay">
          <div className="modal-content">
            <h3>⚠️ Conflicto al Desactivar Supervisor</h3>
            <p className="modal-intro">
              No se puede desactivar a <strong>{deactivatingUser.nombre}</strong> porque tiene {pendingRequests.length} solicitud(es) activa(s) bajo su revisión en este momento:
            </p>

            <div className="pending-requests-list">
              <table>
                <thead>
                  <tr>
                    <th>Colaborador</th>
                    <th>Tipo</th>
                    <th>Estado</th>
                  </tr>
                </thead>
                <tbody>
                  {pendingRequests.map(r => (
                    <tr key={r.id}>
                      <td>{r.empleadoNombre}</td>
                      <td>{r.tipoNombre}</td>
                      <td><span className="badge badge-revision">{r.estado}</span></td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            <div className="reassignment-block">
              <label htmlFor="modal-supervisor-select">Seleccione un nuevo Supervisor para reasignar estas solicitudes en bloque:</label>
              <select
                id="modal-supervisor-select"
                value={targetSupervisorId}
                onChange={e => setTargetSupervisorId(e.target.value)}
              >
                <option value="">-- Seleccionar Supervisor Destino --</option>
                {supervisors
                  .filter(s => s.id !== deactivatingUser.id)
                  .map(s => (
                    <option key={s.id} value={s.id}>{s.nombre}</option>
                  ))}
              </select>
            </div>

            <div className="modal-actions">
              <button 
                className="btn-cancel"
                onClick={() => { setShowDeactivateModal(false); setDeactivatingUser(null); }}
              >
                Cancelar
              </button>
              <button
                className="btn-primary btn-reassign-deactivate"
                disabled={!targetSupervisorId || bulkReassignMutation.isPending || deactivateUserMutation.isPending}
                onClick={handleBulkReassignAndDeactivate}
              >
                Reasignar en Bloque y Desactivar
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};
