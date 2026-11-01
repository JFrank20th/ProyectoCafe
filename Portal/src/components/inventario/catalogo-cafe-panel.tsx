'use client';

import { useLocale, useTranslations } from '@/hooks/useTranslations';
import {
  estadoCatalogoColor,
  estadoCatalogoKey,
  formatDateTime,
  formatGrams,
} from '@/lib/inventario/cafe';
import type { CatalogoCafeKind, CatalogoCafeRow } from '@/lib/inventario/catalogos';

import Alert from '@mui/material/Alert';
import Button from '@mui/material/Button';
import Card from '@mui/material/Card';
import CardContent from '@mui/material/CardContent';
import CardHeader from '@mui/material/CardHeader';
import Chip from '@mui/material/Chip';
import Stack from '@mui/material/Stack';
import Table from '@mui/material/Table';
import TableBody from '@mui/material/TableBody';
import TableCell from '@mui/material/TableCell';
import TableContainer from '@mui/material/TableContainer';
import TableHead from '@mui/material/TableHead';
import TableRow from '@mui/material/TableRow';
import Typography from '@mui/material/Typography';

export type CatalogoCafePanelProps = {
  kind: CatalogoCafeKind;
  items: readonly CatalogoCafeRow[];
  loading: boolean;
  error: string | null;
  canCreate: boolean;
  canEdit: boolean;
  canDelete: boolean;
  onCreate: () => void;
  onEdit: (item: CatalogoCafeRow) => void;
  onDelete: (item: CatalogoCafeRow) => void;
};

export function CatalogoCafePanel({
  kind,
  items,
  loading,
  error,
  canCreate,
  canEdit,
  canDelete,
  onCreate,
  onEdit,
  onDelete,
}: CatalogoCafePanelProps) {
  const tc = useTranslations('common');
  const tf = useTranslations('fields');
  const ti = useTranslations('inventory');
  const locale = useLocale();

  const isPresentacion = kind === 'presentacion';
  const isVariedad = kind === 'variedad';
  const isTipoTueste = kind === 'tipoTueste';
  const isTipoProceso = kind === 'tipoProceso';
  const title = isPresentacion
    ? ti('presentacionesTitle')
    : isVariedad
      ? ti('variedadesTitle')
      : isTipoTueste
        ? ti('tiposTuesteTitle')
        : isTipoProceso
          ? ti('tiposProcesoTitle')
          : ti('tiposCafeTitle');
  const countLabel = isPresentacion
    ? ti('presentacionesCount', { count: items.length })
    : isVariedad
      ? ti('variedadesCount', { count: items.length })
      : isTipoTueste
        ? ti('tiposTuesteCount', { count: items.length })
        : isTipoProceso
          ? ti('tiposProcesoCount', { count: items.length })
          : ti('tiposCafeCount', { count: items.length });
  const createLabel = isPresentacion
    ? ti('newPresentacion')
    : isVariedad
      ? ti('newVariedad')
      : isTipoTueste
        ? ti('newTipoTueste')
        : isTipoProceso
          ? ti('newTipoProceso')
          : ti('newTipoCafe');
  const emptyMessage = isPresentacion
    ? ti('noPresentaciones')
    : isVariedad
      ? ti('noVariedades')
      : isTipoTueste
        ? ti('noTiposTueste')
        : isTipoProceso
          ? ti('noTiposProceso')
          : ti('noTiposCafe');
  const hasActions = canEdit || canDelete;

  return (
    <Card variant="outlined">
      <CardHeader
        title={title}
        subheader={countLabel}
        action={
          <Button variant="contained" size="small" onClick={onCreate} disabled={!canCreate}>
            {createLabel}
          </Button>
        }
      />
      <CardContent sx={{ pt: 0 }}>
        {loading ? (
          <Typography variant="body2" color="text.secondary">
            {tc('loading')}
          </Typography>
        ) : error ? (
          <Alert severity="error">{error}</Alert>
        ) : items.length === 0 ? (
          <Alert severity="info">{emptyMessage}</Alert>
        ) : (
          <TableContainer>
            <Table size="small" sx={{ minWidth: isPresentacion ? 720 : 620 }}>
              <TableHead>
                <TableRow>
                  <TableCell>{tf('code')}</TableCell>
                  <TableCell>{tf('name')}</TableCell>
                  {isPresentacion ? (
                    <TableCell align="right">{tf('unitWeight')}</TableCell>
                  ) : null}
                  <TableCell>{tf('status')}</TableCell>
                  <TableCell>{tf('updatedAt')}</TableCell>
                  {hasActions ? <TableCell align="right">{tf('actions')}</TableCell> : null}
                </TableRow>
              </TableHead>
              <TableBody>
                {items.map((item) => (
                  <TableRow key={item.id} hover>
                    <TableCell>
                      <Typography variant="body2" sx={{ fontWeight: 600 }}>
                        {item.codigo}
                      </Typography>
                    </TableCell>
                    <TableCell>{item.nombre}</TableCell>
                    {isPresentacion ? (
                      <TableCell align="right">
                        {item.pesoUnitarioGramos != null
                          ? formatGrams(locale, item.pesoUnitarioGramos)
                          : '—'}
                      </TableCell>
                    ) : null}
                    <TableCell>
                      <Chip
                        size="small"
                        color={estadoCatalogoColor(item.estado)}
                        label={ti(estadoCatalogoKey(item.estado))}
                      />
                    </TableCell>
                    <TableCell>{formatDateTime(locale, item.actualizadoEn)}</TableCell>
                    {hasActions ? (
                      <TableCell align="right">
                        <Stack
                          direction="row"
                          spacing={1}
                          justifyContent="flex-end"
                          useFlexGap
                          flexWrap="wrap"
                        >
                          {canEdit ? (
                            <Button size="small" variant="outlined" onClick={() => onEdit(item)}>
                              {tc('edit')}
                            </Button>
                          ) : null}
                          {canDelete ? (
                            <Button
                              size="small"
                              color="error"
                              variant="outlined"
                              onClick={() => onDelete(item)}
                            >
                              {tc('delete')}
                            </Button>
                          ) : null}
                        </Stack>
                      </TableCell>
                    ) : null}
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>
        )}
      </CardContent>
    </Card>
  );
}
