import React, { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { apiClient } from '../../../shared/api/axios';
import './TiposSolicitudAdmin.css';

interface Campo {
  nombre: string;
  tipo: 'texto' | 'numero' | 'fecha' | 'lista';
  requerido: boolean;
  opciones?: string[];
}

interface TipoSolicitud {
  id: string;
  nombre: string;
  descripcion: string;
  camposDefinicion: string; // JSON string
  activo: boolean;
}

export const TiposSolicitudAdmin: React.FC = () => {
  const queryClient = useQueryClient();
  const [editingTipo, setEditingTipo] = useState<Partial<TipoSolicitud> | null>(null);
  const [campos, setCampos] = useState<Campo[]>([]);
  const [newFieldName, setNewFieldName] = useState('');
  const [newFieldTipo, setNewFieldTipo] = useState<'texto' | 'numero' | 'fecha' | 'lista'>('texto');
  const [newFieldRequerido, setNewFieldRequerido] = useState(false);
  const [newFieldOpciones, setNewFieldOpciones] = useState('');

  // Fetch all request types (including inactive)
  const { data: response, isLoading } = useQuery<{ data: TipoSolicitud[] }>({
    queryKey: ['tipos-solicitud-admin'],
    queryFn: async () => {
      const res = await apiClient.get('/tipos-solicitud');
      return res.data;
    }
  });

  const tipos = response?.data || [];

  // Create/Update mutations
  const saveMutation = useMutation({
    mutationFn: async (data: any) => {
      if (data.id) {
        await apiClient.put(`/tipos-solicitud/${data.id}`, data);
      } else {
        await apiClient.post('/tipos-solicitud', data);
      }
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['tipos-solicitud-admin'] });
      setEditingTipo(null);
      setCampos([]);
    },
    onError: (err: any) => {
      alert(err.response?.data?.detail || 'Error al guardar el tipo de solicitud.');
    }
  });

  // Toggle active mutation
  const toggleMutation = useMutation({
    mutationFn: async ({ id, activo }: { id: string; activo: boolean }) => {
      await apiClient.post(`/tipos-solicitud/${id}/toggle`, { id, activo });
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['tipos-solicitud-admin'] });
    },
    onError: (err: any) => {
      alert(err.response?.data?.detail || 'Error al cambiar estado.');
    }
  });

  // Delete mutation
  const deleteMutation = useMutation({
    mutationFn: async (id: string) => {
      await apiClient.delete(`/tipos-solicitud/${id}`);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['tipos-solicitud-admin'] });
    },
    onError: (err: any) => {
      alert(err.response?.data?.detail || 'No se puede eliminar este tipo de solicitud porque tiene solicitudes asociadas.');
    }
  });

  const handleStartCreate = () => {
    setEditingTipo({ nombre: '', descripcion: '', activo: false });
    setCampos([]);
  };

  const handleStartEdit = (tipo: TipoSolicitud) => {
    setEditingTipo(tipo);
    try {
      const parsedFields = typeof tipo.camposDefinicion === 'string'
        ? JSON.parse(tipo.camposDefinicion)
        : tipo.camposDefinicion;
      setCampos(parsedFields || []);
    } catch {
      setCampos([]);
    }
  };

  const handleAddField = () => {
    if (!newFieldName.trim()) return;
    const opcionesList = newFieldOpciones.split(',').map(o => o.trim()).filter(Boolean);
    const newField: Campo = {
      nombre: newFieldName.trim(),
      tipo: newFieldTipo,
      requerido: newFieldRequerido,
      opciones: newFieldTipo === 'lista' ? opcionesList : undefined
    };
    setCampos([...campos, newField]);
    setNewFieldName('');
    setNewFieldRequerido(false);
    setNewFieldOpciones('');
  };

  const handleRemoveField = (index: number) => {
    setCampos(campos.filter((_, i) => i !== index));
  };

  const handleSave = () => {
    if (!editingTipo?.nombre?.trim()) {
      alert('El nombre es requerido.');
      return;
    }
    const payload = {
      id: editingTipo.id,
      nombre: editingTipo.nombre,
      descripcion: editingTipo.descripcion,
      camposDefinicion: JSON.stringify(campos)
    };
    saveMutation.mutate(payload);
  };

  return (
    <div className="admin-tipos-container">
      <div className="admin-header-row">
        <h2>Configuración de Tipos de Solicitud</h2>
        {!editingTipo && (
          <button className="btn-primary" onClick={handleStartCreate}>
            ➕ Nuevo Tipo
          </button>
        )}
      </div>

      {editingTipo ? (
        <div className="card form-card">
          <h3>{editingTipo.id ? 'Editar Tipo de Solicitud' : 'Nuevo Tipo de Solicitud'}</h3>
          <div className="form-group">
            <label>Nombre del Tipo</label>
            <input
              type="text"
              value={editingTipo.nombre || ''}
              onChange={e => setEditingTipo({ ...editingTipo, nombre: e.target.value })}
              placeholder="Ej: Permiso de Ausencia"
            />
          </div>
          <div className="form-group">
            <label>Descripción</label>
            <textarea
              value={editingTipo.descripcion || ''}
              onChange={e => setEditingTipo({ ...editingTipo, descripcion: e.target.value })}
              placeholder="Descripción breve para guiar al colaborador"
            />
          </div>

          <div className="fields-builder-section">
            <h4>Campos Dinámicos</h4>
            <div className="fields-list">
              {campos.length === 0 ? (
                <p className="no-fields-text">No hay campos dinámicos configurados aún.</p>
              ) : (
                campos.map((c, i) => (
                  <div key={i} className="field-badge">
                    <span>
                      <strong>{c.nombre}</strong> ({c.tipo}){c.requerido && ' *'} 
                      {c.opciones && c.opciones.length > 0 && ` [Opciones: ${c.opciones.join(', ')}]`}
                    </span>
                    <button className="remove-field-btn" onClick={() => handleRemoveField(i)}>❌</button>
                  </div>
                ))
              )}
            </div>

            <div className="add-field-form">
              <h5>Agregar Nuevo Campo</h5>
              <div className="add-field-grid">
                <input
                  type="text"
                  placeholder="Nombre del campo (ej: Motivo)"
                  value={newFieldName}
                  onChange={e => setNewFieldName(e.target.value)}
                />
                <select
                  value={newFieldTipo}
                  onChange={e => setNewFieldTipo(e.target.value as any)}
                >
                  <option value="texto">Texto</option>
                  <option value="numero">Número</option>
                  <option value="fecha">Fecha</option>
                  <option value="lista">Lista de Opciones</option>
                </select>
                <label className="checkbox-label">
                  <input
                    type="checkbox"
                    checked={newFieldRequerido}
                    onChange={e => setNewFieldRequerido(e.target.checked)}
                  />
                  ¿Requerido?
                </label>
              </div>
              {newFieldTipo === 'lista' && (
                <input
                  type="text"
                  className="options-input"
                  placeholder="Opciones separadas por comas (ej: Vacaciones, Cita médica, Trámite)"
                  value={newFieldOpciones}
                  onChange={e => setNewFieldOpciones(e.target.value)}
                />
              )}
              <button type="button" className="btn-secondary" onClick={handleAddField}>
                Agregar Campo
              </button>
            </div>
          </div>

          <div className="form-actions">
            <button className="btn-cancel" onClick={() => setEditingTipo(null)}>
              Cancelar
            </button>
            <button 
              className="btn-primary" 
              onClick={handleSave}
              disabled={saveMutation.isPending}
            >
              {saveMutation.isPending ? 'Guardando...' : 'Guardar Tipo de Solicitud'}
            </button>
          </div>
        </div>
      ) : isLoading ? (
        <div>Cargando tipos de solicitud...</div>
      ) : (
        <div className="tipos-grid">
          {tipos.map(t => (
            <div key={t.id} className="card tipo-card">
              <div className="tipo-card-header">
                <h4>{t.nombre}</h4>
                <span className={`status-badge ${t.activo ? 'status-active' : 'status-inactive'}`}>
                  {t.activo ? 'Activo' : 'Borrador'}
                </span>
              </div>
              <p className="tipo-desc">{t.descripcion}</p>
              <div className="tipo-card-actions">
                <button className="btn-icon" onClick={() => handleStartEdit(t)} title="Editar">
                  ✏️
                </button>
                <button 
                  className="btn-icon" 
                  onClick={() => toggleMutation.mutate({ id: t.id, activo: !t.activo })}
                  title={t.activo ? 'Desactivar' : 'Activar'}
                >
                  {t.activo ? '⏸️' : '▶️'}
                </button>
                <button 
                  className="btn-icon btn-delete" 
                  onClick={() => { if(confirm('¿Eliminar tipo de solicitud?')) deleteMutation.mutate(t.id); }}
                  title="Eliminar"
                >
                  🗑️
                </button>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
};
