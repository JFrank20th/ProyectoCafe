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
import { isCatalogoActivo, type PresentacionCafe } from '@/lib/inventario/cafe';
import {
  type PrecioVenta,
  type PrecioVentaFormValues,
  validatePrecioVentaInput,
} from '@/lib/ventas/precios';

export type PrecioVentaSaved = {
  id: number;
  nombre: string;
  created: boolean;
};

export type PrecioVentaDialogProps = {
  open: boolean;
  item: PrecioVenta | null;
  presentaciones: readonly PresentacionCafe[];
  onClose: () => void;
  onSaved: (result: PrecioVentaSaved) => void;
};

export function PrecioVentaDialog({
  open,
  item,
  presentaciones,
  onClose,
  onSaved,
}: PrecioVentaDialogProps) {
  const tc = useTranslations('common');
  const tf = useTranslations('fields');
  const ts = useTranslations('sales');

  const [nombre, setNombre] = useState('');
  const [presentacionCafeId, setPresentacionCafeId] = useState('');
  const [precio, setPrecio] = useState('');
  const [estado, setEstado] = useState<1 | 2>(1);
  const [submitted, setSubmitted] = useState(false);
  const [saving, setSaving] = useState(false);
  const [dialogError, setDialogError] = useState<string | null>(null);

  const editing = item !== null;
  const values: PrecioVentaFormValues = {
    nombre,
    presentacionCafeId,
    precio,
    estado,
  };
  const errors = submitted ? validatePrecioVentaInput(values) : {};

  const activePresentaciones = presentaciones.filter((presentacion) =>
    isCatalogoActivo(presentacion.estado),
  );

  useEffect(() => {
    if (!open) return;

    setNombre(item?.nombre ?? '');
    setPresentacionCafeId(
      item?.presentacionCafeId ? String(item.presentacionCafeId) : '',
    );
    setPrecio(item?.precio != null ? String(item.precio) : '');
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

    const currentErrors = validatePrecioVentaInput(values);
    if (Object.keys(currentErrors).length > 0) {
      setDialogError(ts('validationErrorSummary') ?? 'Revise los campos obligatorios.');
      return;
    }

    setSaving(true);

    const payload = {
      nombre: values.nombre.trim(),
      presentacionCafeId: Number(values.presentacionCafeId),
      precio: Number(values.precio.trim().replace(',', '.')),
      estado: values.estado,
    };

    try {
      if (item) {
        await apiFetch<void>(`/api/v1/ventas/precios/${item.precioVentaId}`, {
          method: 'PUT',
          body: JSON.stringify(payload),
        });
        onSaved({ id: item.precioVentaId, nombre: payload.nombre, created: false });
      } else {
        const created = await apiFetch<{ precioVentaId: number }>(
          '/api/v1/ventas/precios',
          {
            method: 'POST',
            body: JSON.stringify(payload),
          },
        );
        onSaved({ id: created.precioVentaId, nombre: payload.nombre, created: true });
      }

      onClose();
    } catch (requestError) {
      setDialogError(
        requestError instanceof Error ? requestError.message : ts('errorSavingPrecio'),
      );
    } finally {
      setSaving(false);
    }
  }

  const title = editing ? ts('editPrecioTitle') : ts('newPrecioTitle');

  return (
    <Dialog open={open} onClose={closeDialog} fullWidth maxWidth="sm">
      <DialogTitle>{title}</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ pt: 1 }}>
          {dialogError ? <Alert severity="error">{dialogError}</Alert> : null}

          <TextField
            autoFocus
            required
            label={tf('name')}
            value={nombre}
            onChange={(event) => setNombre(event.target.value)}
            error={Boolean(errors.nombre)}
            helperText={errors.nombre ? ts('precioNameRequired') : undefined}
            disabled={saving}
            fullWidth
          />

          <FormControl fullWidth error={Boolean(errors.presentacionCafeId)} disabled={saving}>
            <InputLabel id="precio-presentacion-label">{tf('presentation')}</InputLabel>
            <Select
              labelId="precio-presentacion-label"
              label={tf('presentation')}
              value={presentacionCafeId}
              onChange={(event: SelectChangeEvent) => setPresentacionCafeId(event.target.value)}
            >
              {activePresentaciones.map((presentacion) => (
                <MenuItem
                  key={presentacion.presentacionCafeId}
                  value={String(presentacion.presentacionCafeId)}
                >
                  {presentacion.nombre} ({presentacion.codigo})
                </MenuItem>
              ))}
            </Select>
            <FormHelperText>
              {errors.presentacionCafeId ? ts('presentationRequired') : undefined}
            </FormHelperText>
          </FormControl>

          <TextField
            required
            type="number"
            label={ts('priceLabel')}
            value={precio}
            onChange={(event) => setPrecio(event.target.value)}
            error={Boolean(errors.precio)}
            helperText={errors.precio ? ts('priceInvalid') : undefined}
            disabled={saving}
            fullWidth
          />

          <FormControl fullWidth error={Boolean(errors.estado)} disabled={saving}>
            <InputLabel id="precio-estado-label">{tf('status')}</InputLabel>
            <Select
              labelId="precio-estado-label"
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
          {saving ? ts('savingPrecio') : tc('save')}
        </Button>
      </DialogActions>
    </Dialog>
  );
}