'use client';

import { useEffect, useMemo, useState } from 'react';

import { useTranslations } from '@/hooks/useTranslations';
import { IconSearch } from '@tabler/icons-react';
import { apiFetch } from '@/lib/api/client';
import { type Cliente, type ClienteSaved } from '@/lib/ventas/clientes';

import Alert from '@mui/material/Alert';
import Button from '@mui/material/Button';
import Card from '@mui/material/Card';
import CardContent from '@mui/material/CardContent';
import CardHeader from '@mui/material/CardHeader';
import Chip from '@mui/material/Chip';
import FormControl from '@mui/material/FormControl';
import InputAdornment from '@mui/material/InputAdornment';
import InputLabel from '@mui/material/InputLabel';
import MenuItem from '@mui/material/MenuItem';
import Select, { type SelectChangeEvent } from '@mui/material/Select';
import Stack from '@mui/material/Stack';
import Table from '@mui/material/Table';
import TableBody from '@mui/material/TableBody';
import TableCell from '@mui/material/TableCell';
import TableContainer from '@mui/material/TableContainer';
import TableHead from '@mui/material/TableHead';
import TableRow from '@mui/material/TableRow';
import TextField from '@mui/material/TextField';
import Typography from '@mui/material/Typography';

import { ClienteDialog } from './cliente-dialog';

export type ClientesPanelProps = {
  canView: boolean;
  canCreate: boolean;
  canEdit: boolean;
};

export function ClientesPanel({ canView, canCreate, canEdit }: ClientesPanelProps) {
  const tc = useTranslations('common');
  const tf = useTranslations('fields');
  const ts = useTranslations('sales');

  const [items, setItems] = useState<Cliente[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  const [query, setQuery] = useState('');
  const [estadoFiltro, setEstadoFiltro] = useState<string>('all');

  const [dialogOpen, setDialogOpen] = useState(false);
  const [editingItem, setEditingItem] = useState<Cliente | null>(null);

  async function loadClientes() {
    if (!canView) return;
    setLoading(true);
    setError(null);
    try {
      const list = await apiFetch<Cliente[]>('/api/v1/ventas/clientes');
      setItems(list);
    } catch (requestError) {
      setError(requestError instanceof Error ? requestError.message : ts('errorLoadingClientes'));
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void loadClientes();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [canView]);

  const filteredItems = useMemo(() => {
    const normalizedQuery = query.trim().toLowerCase();

    return items.filter((cliente) => {
      if (estadoFiltro !== 'all' && String(cliente.estado) !== estadoFiltro) {
        return false;
      }

      if (!normalizedQuery) return true;

      return (
        cliente.nombre.toLowerCase().includes(normalizedQuery) ||
        cliente.numeroIdentificacion.toLowerCase().includes(normalizedQuery) ||
        (cliente.email ?? '').toLowerCase().includes(normalizedQuery) ||
        (cliente.telefono ?? '').toLowerCase().includes(normalizedQuery)
      );
    });
  }, [items, query, estadoFiltro]);

  function openCreate() {
    setEditingItem(null);
    setDialogOpen(true);
  }

  function openEdit(item: Cliente) {
    setEditingItem(item);
    setDialogOpen(true);
  }

  async function toggleEstado(item: Cliente) {
    if (!canEdit) return;
    try {
      await apiFetch<void>(`/api/v1/ventas/clientes/${item.clienteId}`, {
        method: 'PUT',
        body: JSON.stringify({
          tipoIdentificacion: item.tipoIdentificacion,
          numeroIdentificacion: item.numeroIdentificacion,
          nombre: item.nombre,
          telefono: item.telefono,
          email: item.email,
          direccion: item.direccion,
          observacion: item.observacion,
          estado: item.estado === 1 ? 2 : 1,
        }),
      });
      setSuccess(ts('clienteUpdated'));
      await loadClientes();
    } catch (requestError) {
      setError(requestError instanceof Error ? requestError.message : ts('errorUpdatingCliente'));
    }
  }

  function onSaved(result: ClienteSaved) {
    setSuccess(
      ts(result.created ? 'clienteCreated' : 'clienteUpdated', { name: result.nombre }),
    );
    void loadClientes();
  }

  if (!canView) {
    return <Alert severity="info">{ts('clientesNoPermission')}</Alert>;
  }

  return (
    <Card variant="outlined">
      <CardHeader
        title={ts('clientesTitle')}
        subheader={ts('clientesCount', { count: filteredItems.length })}
        action={
          <Button variant="contained" size="small" onClick={openCreate} disabled={!canCreate}>
            {ts('newCliente')}
          </Button>
        }
      />
      <CardContent sx={{ pt: 0 }}>
        <Stack spacing={2}>
          {success ? (
            <Alert severity="success" onClose={() => setSuccess(null)}>
              {success}
            </Alert>
          ) : null}
          {error ? <Alert severity="error">{error}</Alert> : null}

          <Stack
            direction={{ xs: 'column', md: 'row' }}
            spacing={2}
            alignItems={{ xs: 'stretch', md: 'center' }}
            useFlexGap
            flexWrap="wrap"
          >
            <TextField
              size="small"
              label={ts('searchCliente')}
              value={query}
              onChange={(event) => setQuery(event.target.value)}
              sx={{ flex: 1, minWidth: { xs: '100%', md: 260 } }}
              slotProps={{
                input: {
                  startAdornment: (
                    <InputAdornment position="start">
                      <IconSearch size={18} stroke={1.5} />
                    </InputAdornment>
                  ),
                },
              }}
            />

            <FormControl size="small" sx={{ minWidth: { xs: '100%', md: 180 } }}>
              <InputLabel id="clientes-estado-filter-label">{tf('status')}</InputLabel>
              <Select
                labelId="clientes-estado-filter-label"
                label={tf('status')}
                value={estadoFiltro}
                onChange={(event: SelectChangeEvent) => setEstadoFiltro(event.target.value)}
              >
                <MenuItem value="all">{ts('allStatuses')}</MenuItem>
                <MenuItem value="1">{ts('statusActive')}</MenuItem>
                <MenuItem value="2">{ts('statusInactive')}</MenuItem>
              </Select>
            </FormControl>
          </Stack>

          {loading ? (
            <Typography variant="body2" color="text.secondary">
              {tc('loading')}
            </Typography>
          ) : filteredItems.length === 0 ? (
            <Alert severity="info">{ts('noClientes')}</Alert>
          ) : (
            <TableContainer>
              <Table size="small" sx={{ minWidth: 900 }}>
                <TableHead>
                  <TableRow>
                    <TableCell>{ts('idTypeLabel')}</TableCell>
                    <TableCell>{ts('idNumberLabel')}</TableCell>
                    <TableCell>{tf('name')}</TableCell>
                    <TableCell>{ts('phoneLabel')}</TableCell>
                    <TableCell>{ts('emailLabel')}</TableCell>
                    <TableCell>{tf('status')}</TableCell>
                    {canEdit ? <TableCell align="right">{tf('actions')}</TableCell> : null}
                  </TableRow>
                </TableHead>
                <TableBody>
                  {filteredItems.map((cliente) => (
                    <TableRow key={cliente.clienteId} hover>
                      <TableCell>{cliente.tipoIdentificacion}</TableCell>
                      <TableCell>{cliente.numeroIdentificacion}</TableCell>
                      <TableCell>
                        <Typography variant="body2" sx={{ fontWeight: 600 }}>
                          {cliente.nombre}
                        </Typography>
                      </TableCell>
                      <TableCell>{cliente.telefono || '-'}</TableCell>
                      <TableCell>{cliente.email || '-'}</TableCell>
                      <TableCell>
                        <Chip
                          size="small"
                          color={cliente.estado === 1 ? 'success' : 'default'}
                          label={cliente.estado === 1 ? ts('statusActive') : ts('statusInactive')}
                        />
                      </TableCell>
                      {canEdit ? (
                        <TableCell align="right">
                          <Stack
                            direction="row"
                            spacing={1}
                            justifyContent="flex-end"
                            useFlexGap
                            flexWrap="wrap"
                          >
                            <Button size="small" variant="outlined" onClick={() => openEdit(cliente)}>
                              {tc('edit')}
                            </Button>
                            <Button
                              size="small"
                              color={cliente.estado === 1 ? 'warning' : 'success'}
                              variant="outlined"
                              onClick={() => void toggleEstado(cliente)}
                            >
                              {cliente.estado === 1 ? ts('deactivate') : ts('activate')}
                            </Button>
                          </Stack>
                        </TableCell>
                      ) : null}
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </TableContainer>
          )}
        </Stack>
      </CardContent>

      <ClienteDialog
        open={dialogOpen}
        item={editingItem}
        onClose={() => setDialogOpen(false)}
        onSaved={onSaved}
      />
    </Card>
  );
}