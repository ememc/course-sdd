import React, { useEffect, useState, useRef } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { apiClient } from '../../shared/api/axios';
import './SolicitudForm.css';

interface FieldDefinition {
  nombre: string;
  tipo: 'texto' | 'numero' | 'fecha' | 'lista';
  requerido: boolean;
  opciones?: string[];
}

interface TipoSolicitud {
  id: string;
  nombre: string;
  descripcion: string;
  camposDefinicion: string; // JSON string or array
}

export const SolicitudForm: React.FC = () => {
  const { id } = useParams<{ id?: string }>();
  const navigate = useNavigate();
  const [tipos, setTipos] = useState<TipoSolicitud[]>([]);
  const [selectedTipo, setSelectedTipo] = useState<TipoSolicitud | null>(null);
  const [fields, setFields] = useState<FieldDefinition[]>([]);
  const [solicitudId, setSolicitudId] = useState<string | null>(id || null);
  const [rowVersion, setRowVersion] = useState<string | null>(null);
  const [autoSaveStatus, setAutoSaveStatus] = useState<string>('');
  const [loading, setLoading] = useState<boolean>(false);
  const [submitting, setSubmitting] = useState<boolean>(false);

  const { register, handleSubmit, watch, setValue } = useForm();
  const formValues = watch();
  const autoSaveTimerRef = useRef<any>(null);

  // Load request types
  useEffect(() => {
    const fetchTipos = async () => {
      try {
        const res = await apiClient.get('/tipos-solicitud?activo=true');
        setTipos(res.data.data || []);
      } catch (err) {
        console.error('Error fetching request types', err);
      }
    };
    fetchTipos();
  }, []);

  // Load existing draft if editing
  useEffect(() => {
    if (id) {
      const fetchDraft = async () => {
        setLoading(true);
        try {
          const res = await apiClient.get(`/solicitudes/${id}`);
          const data = res.data;
          setRowVersion(data.rowVersion);
          
          // Set type
          const tipo = data.tipo;
          setSelectedTipo(tipo);
          
          const parsedFields = typeof tipo.camposDefinicion === 'string' 
            ? JSON.parse(tipo.camposDefinicion) 
            : tipo.camposDefinicion;
          setFields(parsedFields);

          // Prepopulate dynamic values
          if (data.camposDinamicos) {
            const values = typeof data.camposDinamicos === 'string' 
              ? JSON.parse(data.camposDinamicos) 
              : data.camposDinamicos;
            
            Object.keys(values).forEach(key => {
              setValue(key, values[key]);
            });
          }
        } catch (err) {
          console.error('Error loading draft', err);
        } finally {
          setLoading(false);
        }
      };
      fetchDraft();
    }
  }, [id, setValue]);

  // Handle request type selection (initializes borrador in backend)
  const handleTipoChange = async (e: React.ChangeEvent<HTMLSelectElement>) => {
    const tipoId = e.target.value;
    const tipo = tipos.find(t => t.id === tipoId) || null;
    setSelectedTipo(tipo);

    if (tipo) {
      const parsedFields = JSON.parse(tipo.camposDefinicion);
      setFields(parsedFields);

      // Create new draft in backend
      try {
        const res = await apiClient.post('/solicitudes', { tipoSolicitudId: tipo.id });
        setSolicitudId(res.data.id);
        setRowVersion(res.data.rowVersion);
        navigate(`/editar/${res.data.id}`, { replace: true });
      } catch (err) {
        console.error('Error creating draft', err);
      }
    } else {
      setFields([]);
    }
  };

  // Debounced auto-save hook
  useEffect(() => {
    if (!solicitudId || !rowVersion || Object.keys(formValues).length === 0) return;

    if (autoSaveTimerRef.current) {
      clearTimeout(autoSaveTimerRef.current);
    }

    setAutoSaveStatus('Guardando borrador...');
    autoSaveTimerRef.current = setTimeout(async () => {
      try {
        const res = await apiClient.patch(
          `/solicitudes/${solicitudId}/borrador`,
          { camposDinamicos: formValues },
          { headers: { 'If-Match': rowVersion } }
        );
        setRowVersion(res.data.rowVersion);
        setAutoSaveStatus('Borrador guardado automáticamente');
      } catch (err) {
        setAutoSaveStatus('Error al guardar borrador');
        console.error('Auto-save failed', err);
      }
    }, 1000);

    return () => {
      if (autoSaveTimerRef.current) clearTimeout(autoSaveTimerRef.current);
    };
  }, [formValues, solicitudId]);

  // Handle final submission (enviar)
  const onSubmit = async () => {
    if (!solicitudId || !rowVersion) return;
    setSubmitting(true);
    try {
      await apiClient.post(
        `/solicitudes/${solicitudId}/enviar`,
        {},
        { headers: { 'If-Match': rowVersion } }
      );
      navigate('/');
    } catch (err: any) {
      alert(err.response?.data?.detail || 'Error al enviar la solicitud.');
    } finally {
      setSubmitting(false);
    }
  };

  if (loading) return <div className="loading">Cargando borrador...</div>;

  return (
    <div className="form-container">
      <div className="form-card">
        <h2>{id ? 'Editar Solicitud' : 'Nueva Solicitud'}</h2>
        
        {!id && (
          <div className="form-group">
            <label htmlFor="tipo">Seleccione el Tipo de Solicitud</label>
            <select id="tipo" onChange={handleTipoChange} defaultValue="">
              <option value="" disabled>-- Seleccionar --</option>
              {tipos.map(t => (
                <option key={t.id} value={t.id}>{t.nombre}</option>
              ))}
            </select>
          </div>
        )}

        {selectedTipo && (
          <div>
            <p className="tipo-description">{selectedTipo.descripcion}</p>
            {autoSaveStatus && <p className="autosave-status">{autoSaveStatus}</p>}

            <form onSubmit={handleSubmit(onSubmit)}>
              {fields.map(field => (
                <div key={field.nombre} className="form-group">
                  <label htmlFor={field.nombre}>
                    {field.nombre} {field.requerido && <span className="required">*</span>}
                  </label>
                  
                  {field.tipo === 'texto' && (
                    <input 
                      type="text" 
                      id={field.nombre}
                      {...register(field.nombre, { required: field.requerido })} 
                    />
                  )}

                  {field.tipo === 'numero' && (
                    <input 
                      type="number" 
                      id={field.nombre}
                      {...register(field.nombre, { required: field.requerido })} 
                    />
                  )}

                  {field.tipo === 'fecha' && (
                    <input 
                      type="date" 
                      id={field.nombre}
                      {...register(field.nombre, { required: field.requerido })} 
                    />
                  )}

                  {field.tipo === 'lista' && (
                    <select 
                      id={field.nombre}
                      {...register(field.nombre, { required: field.requerido })}
                    >
                      <option value="">-- Seleccionar --</option>
                      {field.opciones?.map(opt => (
                        <option key={opt} value={opt}>{opt}</option>
                      ))}
                    </select>
                  )}
                </div>
              ))}

              <div className="form-actions">
                <button 
                  type="button" 
                  className="cancel-btn" 
                  onClick={() => navigate('/')}
                >
                  Regresar
                </button>
                <button 
                  type="submit" 
                  className="submit-btn" 
                  disabled={submitting}
                >
                  {submitting ? 'Enviando...' : 'Enviar Solicitud'}
                </button>
              </div>
            </form>
          </div>
        )}
      </div>
    </div>
  );
};
