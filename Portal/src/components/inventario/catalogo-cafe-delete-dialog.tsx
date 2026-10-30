'use client';

import { useEffect, useState } from 'react';

import Alert from '@mui/material/Alert';
import Button from '@mui/material/Button';
import Dialog from '@mui/material/Dialog';
import DialogActions from '@mui/material/DialogActions';
import DialogContent from '@mui/material/DialogContent';
import DialogContentText from '@mui/material/DialogContentText';
import DialogTitle from '@mui/material/DialogTitle';

import { useTranslations } from '@/hooks/useTranslations';
import { apiFetch } from '@/lib/api/client';
import {
  catalogoCafePath,
  type CatalogoCafeKind,
  type CatalogoCafeRow,
} from '@/lib/inventario/catalogos';
import { resolveInventoryError } from '@/lib/inventario/operations';

export type CatalogoCafeDeleteDialogProps = {
  kind: CatalogoCafeKind;
  open: boolean;
  item: CatalogoCafeRow | null;
  onClose: () => void;
  onDeleted: (item: CatalogoCafeRow) => void;
};

export function CatalogoCafeDeleteDialog({
  kind,
  open,
  item,
  onClose,
  onDeleted,
}: CatalogoCafeDeleteDialogProps) {
  const tc = useTranslations('common');
  const ti = useTranslations('inventory');

  const [deleting, setDeleting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const isPresentacion = kind === 'presentacion';

  useEffect(() => {
    if (!open) return;

    setDeleting(false);
    setError(null);
  }, [item, kind, open]);

  function closeDialog() {
    if (deleting) return;
    onClose();
  }

  async function confirmDelete() {
    if (!item || deleting) return;

    setDeleting(true);
    setError(null);

    try {
      await apiFetch<void>(`${catalogoCafePath(kind)}/${item.id}`, {
        method: 'DELETE',
      });
      onDeleted(item);
      onClose();
    } catch (requestError) {
      setError(
        resolveInventoryError(
          requestError,
          ti(isPresentacion ? 'errorDeletingPresentacion' : 'errorDeletingTipoCafe'),
          {
            conflict: ti('conflictError'),
            forbidden: ti('forbiddenError'),
            notFound: ti('catalogNotFoundError'),
          },
        ),
      );
    } finally {
      setDeleting(false);
    }
  }

  return (
    <Dialog open={open} onClose={closeDialog} fullWidth maxWidth="xs">
      <DialogTitle>
        {isPresentacion ? ti('deletePresentacionTitle') : ti('deleteTipoCafeTitle')}
      </DialogTitle>
      <DialogContent>
        <Alert severity="warning" sx={{ mb: 2 }}>
          {ti('deleteCatalogRestriction')}
        </Alert>
        <DialogContentText>
          {isPresentacion
            ? ti('deletePresentacionBody', {
                name: item?.nombre ?? '',
                code: item?.codigo ?? '',
              })
            : ti('deleteTipoCafeBody', {
                name: item?.nombre ?? '',
                code: item?.codigo ?? '',
              })}
        </DialogContentText>
        {error ? (
          <Alert severity="error" sx={{ mt: 2 }}>
            {error}
          </Alert>
        ) : null}
      </DialogContent>
      <DialogActions>
        <Button onClick={closeDialog} disabled={deleting}>
          {tc('cancel')}
        </Button>
        <Button
          color="error"
          variant="contained"
          onClick={() => void confirmDelete()}
          disabled={deleting || !item}
        >
          {tc('delete')}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
