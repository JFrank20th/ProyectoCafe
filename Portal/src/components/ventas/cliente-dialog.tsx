'use client';

import { useEffect, useState } from 'react';

import Alert from '@mui/material/Alert';
import Button from '@mui/material/Button';
import Dialog from '@mui/material/Dialog';
import DialogActions from '@mui/material/DialogActions';
import DialogContent from '@mui/material/DialogContent';
import DialogTitle from '@mui/material/DialogTitle';
import FormControl from '@mui/material/FormControl';
import FormHelperText from '@mui/material/FormHelperText';
import InputLabel from '@mui/material/InputLabel';
import MenuItem from '@mui/material/MenuItem';
import Select, { type SelectChangeEvent } from '@mui/material/Select';
import Stack from '@mui/material/Stack';
import TextField from '@mui/material/TextField';

import { useTranslations } from '@/hooks/useTranslations';
import { apiFetch } from '@/lib/api/client';
import {
  type Cliente,
  type ClienteFormValues,
  type ClienteSaved,
  validateClienteInput,
} from '@/lib/ventas/clientes';

export type ClienteDialogProps = {
  open: boolean;
  item: Cliente | null;
  onClose: () => void;
  onSaved: (result: ClienteSaved) => void;
};

export function ClienteDialog({ open, item, onClose, onSaved }: ClienteDialogProps) {
  const tc = useTranslations('common');
  const tf = useTranslations('fields');
  const ts = useTranslations('sales');

  const [tipoIdentificacion, setTipoIdentificacion] = useState('');
  const [numeroIdentificacion, setNumeroIdentificacion] = useState('');
  const [nombre, setNombre] = useState('');
  const [telefono, setTelefono] = useState('');
  const [email, setEmail] = useState('');
  const [direccion, setDireccion] = useState('');
  const [observacion, setObservacion] = useState('');
  const [estado, setEstado] = useState<1 | 2>(1);
  const [submitted, setSubmitted] = useState(false);
  const [saving, setSaving] = useState(false);
  const [dialogError, setDialogError] = useState<string | null>(null);

  const editing = item !== null;
  const values: ClienteFormValues = {
    tipoIdentificacion,
    numeroIdentificacion,
    nombre,
    telefono,
    email,
    direccion,
    observacion,
    estado,
  };
  const errors = submitted ? validateClienteInput(values) : {};

  useEffect(() => {
    if (!open) return;

    setTipoIdentificacion(item?.tipoIdentificacion ?? '');
    setNumeroIdentificacion(item?.numeroIdentificacion ?? '');
    setNombre(item?.nombre ?? '');
    setTelefono(item?.telefono ?? '');
    setEmail(item?.email ?? '');
    setDireccion(item?.direccion ?? '');
    setObservacion(item?.observacion ?? '');
    setEstado(item?.estado === 2 ? 2 : 1);
    setSubmitted(false);
    setSaving(false);
    setDialogError(null);
  }, [item, open]);

  function closeDialog() {
    if (saving) return;
    onClose();
  }

  async function submit() {
    if (saving) return;

    setSubmitted(true);
    setDialogError(null);

    const currentErrors = validateClienteInput(values);
    if (Object.keys(currentErrors).length > 0) {
      setDialogError(ts('validationErrorSummary') ?? 'Revise los campos obligatorios.');
      return;
    }

    setSaving(true);

    const payload = {
      tipoIdentificacion: values.tipoIdentificacion.trim().toUpperCase(),
      numeroIdentificacion: values.numeroIdentificacion.trim(),
      nombre: values.nombre.trim(),
      telefono: values.telefono.trim() || null,
      email: values.email.trim() || null,
      direccion: values.direccion.trim() || null,
      observacion: values.observacion.trim() || null,
      estado: values.estado,
    };

    try {
      if (item) {
        await apiFetch<void>(`/api/v1/ventas/clientes/${item.clienteId}`, {
          method: 'PUT',
          body: JSON.stringify(payload),
        });
        onSaved({ id: item.clienteId, nombre: payload.nombre, created: false });
      } else {
        const created = await apiFetch<{ clienteId: number }>(
          '/api/v1/ventas/clientes',
          {
            method: 'POST',
            body: JSON.stringify(payload),
          },
        );
        onSaved({ id: created.clienteId, nombre: payload.nombre, created: true });
      }

      onClose();
    } catch (requestError) {
      setDialogError(
        requestError instanceof Error ? requestError.message : ts('errorSavingCliente'),
      );
    } finally {
      setSaving(false);
    }
  }

  const title = editing ? ts('editClienteTitle') : ts('newClienteTitle');

  return (
    <Dialog open={open} onClose={closeDialog} fullWidth maxWidth="md">
      <DialogTitle>{title}</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ pt: 1 }}>
          {dialogError ? <Alert severity="error">{dialogError}</Alert> : null}

          <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
            <TextField
              required
              label={ts('idTypeLabel')}
              value={tipoIdentificacion}
              onChange={(event) => setTipoIdentificacion(event.target.value)}
              error={Boolean(errors.tipoIdentificacion)}
              helperText={errors.tipoIdentificacion ? ts('idTypeRequired') : undefined}
              disabled={saving}
              fullWidth
            />

            <TextField
              required
              label={ts('idNumberLabel')}
              value={numeroIdentificacion}
              onChange={(event) => setNumeroIdentificacion(event.target.value)}
              error={Boolean(errors.numeroIdentificacion)}
              helperText={
                errors.numeroIdentificacion ? ts('idNumberRequired') : undefined
              }
              disabled={saving}
              fullWidth
            />
          </Stack>

          <TextField
            required
            label={tf('name')}
            value={nombre}
            onChange={(event) => setNombre(event.target.value)}
            error={Boolean(errors.nombre)}
            helperText={errors.nombre ? ts('clienteNameRequired') : undefined}
            disabled={saving}
            fullWidth
          />

          <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
            <TextField
              label={ts('phoneLabel')}
              value={telefono}
              onChange={(event) => setTelefono(event.target.value)}
              error={Boolean(errors.telefono)}
              helperText={errors.telefono ? ts('phoneTooLong') : undefined}
              disabled={saving}
              fullWidth
            />

            <TextField
              label={ts('emailLabel')}
              value={email}
              onChange={(event) => setEmail(event.target.value)}
              error={Boolean(errors.email)}
              helperText={errors.email ? ts('emailInvalid') : undefined}
              disabled={saving}
              fullWidth
            />
          </Stack>

          <TextField
            label={ts('addressLabel')}
            value={direccion}
            onChange={(event) => setDireccion(event.target.value)}
            error={Boolean(errors.direccion)}
            helperText={errors.direccion ? ts('addressTooLong') : undefined}
            disabled={saving}
            fullWidth
          />

          <TextField
            label={tf('observation')}
            value={observacion}
            onChange={(event) => setObservacion(event.target.value)}
            error={Boolean(errors.observacion)}
            helperText={errors.observacion ? ts('observationTooLong') : undefined}
            disabled={saving}
            fullWidth
            multiline
            minRows={2}
          />

          <FormControl fullWidth error={Boolean(errors.estado)} disabled={saving}>
            <InputLabel id="cliente-estado-label">{tf('status')}</InputLabel>
            <Select
              labelId="cliente-estado-label"
              label={tf('status')}
              value={String(estado)}
              onChange={(event: SelectChangeEvent) => {
                setEstado(event.target.value === '2' ? 2 : 1);
              }}
            >
              <MenuItem value="1">{ts('statusActive')}</MenuItem>
              <MenuItem value="2">{ts('statusInactive')}</MenuItem>
            </Select>
            <FormHelperText>
              {errors.estado ? ts('statusInvalid') : undefined}
            </FormHelperText>
          </FormControl>
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={closeDialog} disabled={saving}>
          {tc('cancel')}
        </Button>
        <Button variant="contained" onClick={() => void submit()} disabled={saving}>
          {saving ? ts('savingCliente') : tc('save')}
        </Button>
      </DialogActions>
    </Dialog>
  );
}