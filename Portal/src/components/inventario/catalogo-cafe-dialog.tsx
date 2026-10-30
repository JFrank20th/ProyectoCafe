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
  buildPresentacionCafePayload,
  buildTipoCafePayload,
  CATALOGO_CODIGO_MAX_LENGTH,
  CATALOGO_NOMBRE_MAX_LENGTH,
  catalogoCafePath,
  validatePresentacionCafeInput,
  validateTipoCafeInput,
  type CatalogoCafeFieldError,
  type CatalogoCafeFormValues,
  type CatalogoCafeKind,
  type CatalogoCafeRow,
  type EstadoCatalogoCafeEditable,
} from '@/lib/inventario/catalogos';
import { resolveInventoryError } from '@/lib/inventario/operations';

export type CatalogoCafeSaved = {
  id: number;
  nombre: string;
  created: boolean;
};

export type CatalogoCafeDialogProps = {
  kind: CatalogoCafeKind;
  open: boolean;
  item: CatalogoCafeRow | null;
  onClose: () => void;
  onSaved: (result: CatalogoCafeSaved) => void;
};

export function CatalogoCafeDialog({
  kind,
  open,
  item,
  onClose,
  onSaved,
}: CatalogoCafeDialogProps) {
  const tc = useTranslations('common');
  const tf = useTranslations('fields');
  const ti = useTranslations('inventory');

  const [codigo, setCodigo] = useState('');
  const [nombre, setNombre] = useState('');
  const [pesoUnitarioGramos, setPesoUnitarioGramos] = useState('');
  const [estado, setEstado] = useState<EstadoCatalogoCafeEditable>(1);
  const [submitted, setSubmitted] = useState(false);
  const [saving, setSaving] = useState(false);
  const [dialogError, setDialogError] = useState<string | null>(null);

  const isPresentacion = kind === 'presentacion';
  const editing = item !== null;
  const values: CatalogoCafeFormValues = {
    codigo,
    nombre,
    pesoUnitarioGramos,
    estado,
  };
  const errors = submitted
    ? isPresentacion
      ? validatePresentacionCafeInput(values)
      : validateTipoCafeInput(values)
    : {};

  useEffect(() => {
    if (!open) return;

    setCodigo(item?.codigo ?? '');
    setNombre(item?.nombre ?? '');
    setPesoUnitarioGramos(
      item?.pesoUnitarioGramos != null ? String(item.pesoUnitarioGramos) : '',
    );
    setEstado(item?.estado === 2 ? 2 : 1);
    setSubmitted(false);
    setSaving(false);
    setDialogError(null);
  }, [item, kind, open]);

  function closeDialog() {
    if (saving) return;
    onClose();
  }

  function fieldErrorText(
    field: keyof CatalogoCafeFormValues,
    error?: CatalogoCafeFieldError,
  ): string | undefined {
    if (!error) return undefined;

    switch (error) {
      case 'required':
        if (field === 'codigo') return ti('catalogCodeRequired');
        if (field === 'nombre') return ti('catalogNameRequired');
        return ti('presentationWeightRequired');
      case 'maxLength':
        return field === 'codigo'
          ? ti('catalogCodeTooLong', { max: CATALOGO_CODIGO_MAX_LENGTH })
          : ti('catalogNameTooLong', { max: CATALOGO_NOMBRE_MAX_LENGTH });
      case 'weightTooLarge':
        return ti('presentationWeightTooLarge');
      case 'invalidState':
        return ti('catalogStateInvalid');
      default:
        return ti('presentationWeightInvalid');
    }
  }

  async function submit() {
    if (saving) return;

    setSubmitted(true);
    setDialogError(null);

    const currentErrors = isPresentacion
      ? validatePresentacionCafeInput(values)
      : validateTipoCafeInput(values);

    if (Object.keys(currentErrors).length > 0) {
      setDialogError(ti('validationErrorSummary'));
      return;
    }

    setSaving(true);

    const path = catalogoCafePath(kind);
    const fallback = ti(
      isPresentacion ? 'errorSavingPresentacion' : 'errorSavingTipoCafe',
    );
    const errorMessages = {
      conflict: ti('conflictError'),
      forbidden: ti('forbiddenError'),
      notFound: ti('catalogNotFoundError'),
    };

    try {
      if (isPresentacion) {
        const payload = buildPresentacionCafePayload(values);

        if (item) {
          await apiFetch<void>(`${path}/${item.id}`, {
            method: 'PUT',
            body: JSON.stringify(payload),
          });
          onSaved({ id: item.id, nombre: payload.nombre, created: false });
        } else {
          const created = await apiFetch<{ presentacionCafeId: number }>(path, {
            method: 'POST',
            body: JSON.stringify(payload),
          });
          onSaved({
            id: created.presentacionCafeId,
            nombre: payload.nombre,
            created: true,
          });
        }
      } else {
        const payload = buildTipoCafePayload(values);

        if (item) {
          await apiFetch<void>(`${path}/${item.id}`, {
            method: 'PUT',
            body: JSON.stringify(payload),
          });
          onSaved({ id: item.id, nombre: payload.nombre, created: false });
        } else {
          const created = await apiFetch<{ tipoCafeId: number }>(path, {
            method: 'POST',
            body: JSON.stringify(payload),
          });
          onSaved({ id: created.tipoCafeId, nombre: payload.nombre, created: true });
        }
      }

      onClose();
    } catch (error) {
      setDialogError(resolveInventoryError(error, fallback, errorMessages));
    } finally {
      setSaving(false);
    }
  }

  const title = isPresentacion
    ? editing
      ? ti('editPresentacionTitle')
      : ti('newPresentacionTitle')
    : editing
      ? ti('editTipoCafeTitle')
      : ti('newTipoCafeTitle');

  return (
    <Dialog open={open} onClose={closeDialog} fullWidth maxWidth="sm">
      <DialogTitle>{title}</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ pt: 1 }}>
          {dialogError ? <Alert severity="error">{dialogError}</Alert> : null}

          <TextField
            autoFocus
            required
            label={tf('code')}
            value={codigo}
            onChange={(event) => setCodigo(event.target.value)}
            error={Boolean(errors.codigo)}
            helperText={
              fieldErrorText('codigo', errors.codigo) ??
              ti('catalogCodeHelp', { max: CATALOGO_CODIGO_MAX_LENGTH })
            }
            disabled={saving}
            fullWidth
          />

          <TextField
            required
            label={tf('name')}
            value={nombre}
            onChange={(event) => setNombre(event.target.value)}
            error={Boolean(errors.nombre)}
            helperText={
              fieldErrorText('nombre', errors.nombre) ??
              ti('catalogNameHelp', { max: CATALOGO_NOMBRE_MAX_LENGTH })
            }
            disabled={saving}
            fullWidth
          />

          {isPresentacion ? (
            <TextField
              required
              type="number"
              label={tf('unitWeight')}
              value={pesoUnitarioGramos}
              onChange={(event) => setPesoUnitarioGramos(event.target.value)}
              error={Boolean(errors.pesoUnitarioGramos)}
              helperText={
                fieldErrorText('pesoUnitarioGramos', errors.pesoUnitarioGramos) ??
                ti('presentationWeightHelp')
              }
              disabled={saving}
              fullWidth
            />
          ) : null}

          <FormControl fullWidth error={Boolean(errors.estado)} disabled={saving}>
            <InputLabel id={`catalogo-${kind}-estado-label`}>{tf('status')}</InputLabel>
            <Select
              labelId={`catalogo-${kind}-estado-label`}
              label={tf('status')}
              value={String(estado)}
              onChange={(event: SelectChangeEvent) => {
                setEstado(event.target.value === '2' ? 2 : 1);
              }}
            >
              <MenuItem value="1">{ti('catalogStateActive')}</MenuItem>
              <MenuItem value="2">{ti('catalogStateInactive')}</MenuItem>
            </Select>
            <FormHelperText>
              {fieldErrorText('estado', errors.estado) ?? ti('catalogStateHelp')}
            </FormHelperText>
          </FormControl>

          {editing ? (
            <Alert severity="info">
              {isPresentacion
                ? ti('presentacionEditRestrictions')
                : ti('tipoCafeEditRestrictions')}
            </Alert>
          ) : null}
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={closeDialog} disabled={saving}>
          {tc('cancel')}
        </Button>
        <Button variant="contained" onClick={() => void submit()} disabled={saving}>
          {saving ? ti('savingCatalog') : tc('save')}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
