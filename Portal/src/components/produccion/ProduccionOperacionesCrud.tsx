'use client';

import { useEffect, useMemo, useState } from 'react';

import Alert from '@mui/material/Alert';
import Button from '@mui/material/Button';
import Card from '@mui/material/Card';
import CardContent from '@mui/material/CardContent';
import CardHeader from '@mui/material/CardHeader';
import Chip from '@mui/material/Chip';
import Dialog from '@mui/material/Dialog';
import DialogActions from '@mui/material/DialogActions';
import DialogContent from '@mui/material/DialogContent';
import DialogTitle from '@mui/material/DialogTitle';
import MenuItem from '@mui/material/MenuItem';
import Stack from '@mui/material/Stack';
import Table from '@mui/material/Table';
import TableBody from '@mui/material/TableBody';
import TableCell from '@mui/material/TableCell';
import TableContainer from '@mui/material/TableContainer';
import TableHead from '@mui/material/TableHead';
import TableRow from '@mui/material/TableRow';
import TextField from '@mui/material/TextField';
import Typography from '@mui/material/Typography';

import { useTranslations } from '@/hooks/useTranslations';
import { apiFetch } from '@/lib/api/client';
import { useAuth } from '@/lib/auth/AuthProvider';

type Parcela = {
	parcelaId: number;
	fincaId: number;
	productoCosechaId?: number | null;
	productoCosechaNombre?: string | null;
	fincaNombre: string;
	nombre: string;
	estado: boolean;
};

type Insumo = {
	insumoId: number;
	nombre: string;
	cantidad: number;
	unidadMedida: string;
	costoUnitario: number;
	estado: boolean;
};

type OperacionAgricola = {
	operacionAgricolaId: number;
	parcelaId: number;
	parcelaNombre: string;
	fincaId: number;
	fincaNombre: string;
	tipoOperacion: string;
	fechaOperacion?: string | null;
	insumoId?: number | null;
	insumoNombre?: string | null;
	productoId?: number | null;
	productoNombre?: string | null;
	cantidadInsumo?: number | null;
	cantidad?: number | null;
	cultivo?: string | null;
	humedad?: number | null;
	loteId?: number | null;
	codigoLote?: string | null;
	costoInsumo: number;
	costoManoObra: number;
	costoOtros: number;
	costoTotal: number;
	observaciones?: string | null;
	estado: string;
};

type OperacionAgricolaCreateRequest = {
	parcelaId: number;
	tipoOperacion: string;
	fechaOperacion?: string;
	insumoId?: number;
	cantidadInsumo?: number;
	cantidad?: number;
	costoManoObra: number;
	costoOtros: number;
	observaciones?: string;
};

const OPERATION_TYPES = ['AplicacionInsumo', 'Fertilizacion', 'Poda', 'Riego', 'Jornal', 'Mantenimiento', 'Cosecha'] as const;

function toInputDate(value?: string | null) {
	if (!value) return '';
	return value.slice(0, 10);
}

function todayInputDate() {
	return new Date().toISOString().slice(0, 10);
}

function parseOptionalNumber(value: string) {
	const trimmed = value.trim();
	if (!trimmed) return undefined;
	return Number(trimmed);
}

function isHarvestOperation(value: string) {
	return value === 'Cosecha';
}

function mapOperationTypeLabel(value: string, tproduction: (key: string) => string) {
	switch (value) {
		case 'AplicacionInsumo':
			return tproduction('operationTypeAplicacionInsumo');
		case 'Fertilizacion':
			return tproduction('operationTypeFertilizacion');
		case 'Poda':
			return tproduction('operationTypePoda');
		case 'Riego':
			return tproduction('operationTypeRiego');
		case 'Jornal':
			return tproduction('operationTypeJornal');
		case 'Cosecha':
			return tproduction('operationTypeCosecha');
		default:
			return tproduction('operationTypeMantenimiento');
	}
}

function mapStatusLabel(value: string, tproduction: (key: string) => string) {
	if (value === 'Anulada') {
		return tproduction('statusAnnulled');
	}

	return tproduction('statusRegistered');
}

function buildItemLabel(item: OperacionAgricola) {
	if (isHarvestOperation(item.tipoOperacion)) {
		if (item.productoNombre && item.cultivo) {
			return `${item.productoNombre} · ${item.cultivo}`;
		}

		return item.productoNombre ?? item.cultivo ?? '—';
	}

	return item.insumoNombre ?? '—';
}

function buildItemQuantity(item: OperacionAgricola) {
	if (isHarvestOperation(item.tipoOperacion)) {
		return item.cantidad ?? '—';
	}

	return item.cantidadInsumo ?? '—';
}

export function ProduccionOperacionesCrud() {
	const nav = useTranslations('navigation');
	const tc = useTranslations('common');
	const tf = useTranslations('fields');
	const tp = useTranslations('pages');
	const tcrud = useTranslations('crud');
	const tproduction = useTranslations('production');

	const { has } = useAuth();
	const canView = has('Produccion.OperacionesAgricolas', 'Ver');
	const canCreate = has('Produccion.OperacionesAgricolas', 'Crear');
	const canAnnul = has('Produccion.OperacionesAgricolas', 'Eliminar');

	const [items, setItems] = useState<OperacionAgricola[]>([]);
	const [parcelas, setParcelas] = useState<Parcela[]>([]);
	const [insumos, setInsumos] = useState<Insumo[]>([]);
	const [loading, setLoading] = useState(true);
	const [error, setError] = useState<string | null>(null);

	const [editOpen, setEditOpen] = useState(false);
	const [editError, setEditError] = useState<string | null>(null);
	const [editSaving, setEditSaving] = useState(false);

	const [parcelaId, setParcelaId] = useState('');
	const [tipoOperacion, setTipoOperacion] = useState<(typeof OPERATION_TYPES)[number]>('AplicacionInsumo');
	const [fechaOperacion, setFechaOperacion] = useState(todayInputDate());
	const [insumoId, setInsumoId] = useState('');
	const [cantidadInsumo, setCantidadInsumo] = useState('');
	const [cantidad, setCantidad] = useState('');
	const [costoManoObra, setCostoManoObra] = useState('0');
	const [costoOtros, setCostoOtros] = useState('0');
	const [observaciones, setObservaciones] = useState('');

	const [annulOpen, setAnnulOpen] = useState(false);
	const [annulError, setAnnulError] = useState<string | null>(null);
	const [annulSaving, setAnnulSaving] = useState(false);
	const [annulTarget, setAnnulTarget] = useState<OperacionAgricola | null>(null);

	const activeParcelas = useMemo(() => parcelas.filter((item) => item.estado), [parcelas]);
	const activeInsumos = useMemo(() => insumos.filter((item) => item.estado), [insumos]);
	const selectedParcela = useMemo(
		() => activeParcelas.find((item) => String(item.parcelaId) === parcelaId) ?? null,
		[activeParcelas, parcelaId],
	);
	const harvestMode = isHarvestOperation(tipoOperacion);

	async function reload() {
		setLoading(true);
		setError(null);
		try {
			const [operacionesRes, parcelasRes, insumosRes] = await Promise.allSettled([
				apiFetch<OperacionAgricola[]>('/api/v1/produccion/operaciones-agricolas'),
				apiFetch<Parcela[]>('/api/v1/produccion/parcelas'),
				apiFetch<Insumo[]>('/api/v1/insumos'),
			]);

			if (operacionesRes.status === 'fulfilled') {
				setItems(operacionesRes.value);
			} else {
				setItems([]);
				setError(operacionesRes.reason instanceof Error ? operacionesRes.reason.message : tcrud('errorLoadingData'));
			}

			const parcelasData = parcelasRes.status === 'fulfilled' ? parcelasRes.value : [];
			const insumosData = insumosRes.status === 'fulfilled' ? insumosRes.value : [];
			setParcelas(parcelasData);
			setInsumos(insumosData);

			if (!parcelaId && parcelasData.some((item) => item.estado)) {
				setParcelaId(String(parcelasData.find((item) => item.estado)?.parcelaId ?? ''));
			}
		} catch (e) {
			setError(e instanceof Error ? e.message : tcrud('errorLoadingData'));
		} finally {
			setLoading(false);
		}
	}

	useEffect(() => {
		void reload();
		// eslint-disable-next-line react-hooks/exhaustive-deps
	}, []);

	function resetForm() {
		setParcelaId(String(activeParcelas[0]?.parcelaId ?? ''));
		setTipoOperacion('AplicacionInsumo');
		setFechaOperacion(todayInputDate());
		setInsumoId('');
		setCantidadInsumo('');
		setCantidad('');
		setCostoManoObra('0');
		setCostoOtros('0');
		setObservaciones('');
	}

	function handleTipoOperacionChange(value: (typeof OPERATION_TYPES)[number]) {
		setTipoOperacion(value);

		if (value === 'Cosecha') {
			setInsumoId('');
			setCantidadInsumo('');
			setCostoManoObra('0');
			return;
		}

		setCantidad('');
	}

	function openCreate() {
		if (!canCreate) return;
		resetForm();
		setEditError(null);
		setEditOpen(true);
	}

	function closeEdit() {
		if (editSaving) return;
		setEditOpen(false);
		setEditError(null);
	}

	async function saveEdit() {
		setEditSaving(true);
		setEditError(null);

		const cantidadInsumoValue = parseOptionalNumber(cantidadInsumo);
		const cantidadValue = parseOptionalNumber(cantidad);

		const payload: OperacionAgricolaCreateRequest = {
			parcelaId: Number(parcelaId),
			tipoOperacion,
			costoManoObra: harvestMode ? 0 : Number(costoManoObra),
			costoOtros: Number(costoOtros),
			...(fechaOperacion ? { fechaOperacion: new Date(fechaOperacion).toISOString() } : {}),
			...(observaciones.trim() ? { observaciones: observaciones.trim() } : {}),
			...(harvestMode
				? {
						...(cantidadValue !== undefined ? { cantidad: cantidadValue } : {}),
					}
				: {
						...(insumoId ? { insumoId: Number(insumoId) } : {}),
						...(cantidadInsumoValue !== undefined ? { cantidadInsumo: cantidadInsumoValue } : {}),
					}),
		};

		try {
			await apiFetch<{ operacionAgricolaId: number }>('/api/v1/produccion/operaciones-agricolas', {
				method: 'POST',
				body: JSON.stringify(payload),
			});

			setEditOpen(false);
			await reload();
		} catch (e) {
			setEditError(e instanceof Error ? e.message : tcrud('errorSaving'));
		} finally {
			setEditSaving(false);
		}
	}

	function openAnnul(item: OperacionAgricola) {
		if (!canAnnul || item.estado !== 'Registrada') return;
		setAnnulTarget(item);
		setAnnulError(null);
		setAnnulOpen(true);
	}

	function closeAnnul() {
		if (annulSaving) return;
		setAnnulOpen(false);
		setAnnulTarget(null);
		setAnnulError(null);
	}

	async function confirmAnnul() {
		if (!canAnnul || !annulTarget) return;
		setAnnulSaving(true);
		setAnnulError(null);
		try {
			await apiFetch<void>(`/api/v1/produccion/operaciones-agricolas/${annulTarget.operacionAgricolaId}/anular`, { method: 'POST' });
			setAnnulOpen(false);
			setAnnulTarget(null);
			await reload();
		} catch (e) {
			setAnnulError(
				e instanceof Error
					? e.message
					: annulTarget.tipoOperacion === 'Cosecha'
						? tproduction('errorAnnullingHarvest')
						: tproduction('errorAnnullingAgriculturalOperation'),
			);
		} finally {
			setAnnulSaving(false);
		}
	}

	const canCreateRecord = canCreate && activeParcelas.length > 0;
	const canSave = harvestMode
		? Boolean(parcelaId) && Boolean(selectedParcela?.productoCosechaId) && Number(cantidad) > 0
		: Boolean(parcelaId) && (!insumoId || Number(cantidadInsumo) > 0);

	if (!canView) {
		return (
			<Stack spacing={1}>
				<Typography variant="h4">{nav('operacionesProduccion')}</Typography>
				<Typography variant="body2" color="text.secondary">{tp('noPermission')}</Typography>
			</Stack>
		);
	}

	return (
		<Stack spacing={2}>
			<Stack direction="row" alignItems="center" justifyContent="space-between" spacing={2}>
				<Typography variant="h4">{nav('operacionesProduccion')}</Typography>
				<Stack direction="row" spacing={1}>
					<Button variant="outlined" onClick={() => void reload()} disabled={loading}>{tc('refresh')}</Button>
					<Button variant="contained" onClick={openCreate} disabled={!canCreateRecord}>{tcrud('new')}</Button>
				</Stack>
			</Stack>

			{error ? <Alert severity="error">{error}</Alert> : null}
			{!loading && activeParcelas.length === 0 ? <Alert severity="warning">{tproduction('missingActivePlots')}</Alert> : null}

			<Card variant="outlined">
				<CardHeader title={tc('listTitle')} />
				<CardContent sx={{ pt: 0 }}>
					{loading ? (
						<Typography variant="body2" color="text.secondary">{tc('loading')}</Typography>
					) : (
						<TableContainer>
							<Table size="small">
								<TableHead>
									<TableRow>
										<TableCell>{tf('id')}</TableCell>
										<TableCell>{tf('date')}</TableCell>
										<TableCell>{tf('farm')}</TableCell>
										<TableCell>{tf('plot')}</TableCell>
										<TableCell>{tf('operationType')}</TableCell>
										<TableCell>{tf('product')}</TableCell>
										<TableCell>{tf('quantity')}</TableCell>
										<TableCell>{tf('batchCode')}</TableCell>
										<TableCell>{tf('totalCost')}</TableCell>
										<TableCell>{tf('status')}</TableCell>
										<TableCell align="right">{tf('actions')}</TableCell>
									</TableRow>
								</TableHead>
								<TableBody>
									{items.map((item) => (
										<TableRow key={item.operacionAgricolaId} hover>
											<TableCell>{item.operacionAgricolaId}</TableCell>
											<TableCell>{toInputDate(item.fechaOperacion) || '—'}</TableCell>
											<TableCell>{item.fincaNombre}</TableCell>
											<TableCell>{item.parcelaNombre}</TableCell>
											<TableCell>{mapOperationTypeLabel(item.tipoOperacion, tproduction)}</TableCell>
											<TableCell>{buildItemLabel(item)}</TableCell>
											<TableCell>{buildItemQuantity(item)}</TableCell>
											<TableCell>{item.codigoLote ?? '—'}</TableCell>
											<TableCell>{item.costoTotal}</TableCell>
											<TableCell>
												<Chip size="small" color={item.estado === 'Registrada' ? 'success' : 'default'} label={mapStatusLabel(item.estado, tproduction)} />
											</TableCell>
											<TableCell align="right">
												<Stack direction="row" spacing={1} justifyContent="flex-end">
													<Button size="small" color="warning" variant="outlined" onClick={() => openAnnul(item)} disabled={!canAnnul || item.estado !== 'Registrada'}>
														{tc('annul')}
													</Button>
												</Stack>
											</TableCell>
										</TableRow>
									))}
								</TableBody>
							</Table>
						</TableContainer>
					)}
				</CardContent>
			</Card>

			<Dialog open={editOpen} onClose={closeEdit} fullWidth maxWidth="md">
				<DialogTitle>{`${tcrud('new')} — ${nav('operacionesProduccion')}`}</DialogTitle>
				<DialogContent dividers>
					<Stack spacing={2} sx={{ pt: 1 }}>
						{editError ? <Alert severity="error">{editError}</Alert> : null}
						{harvestMode && selectedParcela && !selectedParcela.productoCosechaId ? <Alert severity="warning">{tproduction('selectedPlotMissingHarvestProduct')}</Alert> : null}

						<Stack direction={{ xs: 'column', md: 'row' }} spacing={2}>
							<TextField select fullWidth label={tf('plot')} value={parcelaId} onChange={(e) => setParcelaId(e.target.value)}>
								{activeParcelas.map((item) => (
									<MenuItem key={item.parcelaId} value={String(item.parcelaId)}>{item.fincaNombre} · {item.nombre}</MenuItem>
								))}
							</TextField>
							<TextField select fullWidth label={tf('operationType')} value={tipoOperacion} onChange={(e) => handleTipoOperacionChange(e.target.value as (typeof OPERATION_TYPES)[number])}>
								{OPERATION_TYPES.map((item) => (
									<MenuItem key={item} value={item}>{mapOperationTypeLabel(item, tproduction)}</MenuItem>
								))}
							</TextField>
						</Stack>

						{harvestMode ? (
							<>
								<Stack direction={{ xs: 'column', md: 'row' }} spacing={2}>
									<TextField fullWidth label={tf('product')} value={selectedParcela?.productoCosechaNombre ?? ''} InputProps={{ readOnly: true }} />
									<TextField fullWidth label={tf('harvestedAt')} value={fechaOperacion} onChange={(e) => setFechaOperacion(e.target.value)} type="date" InputLabelProps={{ shrink: true }} />
								</Stack>

								<Stack direction={{ xs: 'column', md: 'row' }} spacing={2}>
									<TextField fullWidth label={tf('quantity')} value={cantidad} onChange={(e) => setCantidad(e.target.value)} type="number" inputProps={{ min: 0.001, step: 0.001 }} />
									<TextField fullWidth label={tf('harvestCostOptional')} value={costoOtros} onChange={(e) => setCostoOtros(e.target.value)} type="number" inputProps={{ min: 0, step: 0.01 }} />
								</Stack>
							</>
						) : (
							<>
								<Stack direction={{ xs: 'column', md: 'row' }} spacing={2}>
									<TextField fullWidth label={tf('date')} value={fechaOperacion} onChange={(e) => setFechaOperacion(e.target.value)} type="date" InputLabelProps={{ shrink: true }} />
									<TextField select fullWidth label={tf('supply')} value={insumoId} onChange={(e) => setInsumoId(e.target.value)}>
										<MenuItem value="">—</MenuItem>
										{activeInsumos.map((item) => (
											<MenuItem key={item.insumoId} value={String(item.insumoId)}>{item.nombre} · {item.cantidad} {item.unidadMedida}</MenuItem>
										))}
									</TextField>
								</Stack>

								<Stack direction={{ xs: 'column', md: 'row' }} spacing={2}>
									<TextField fullWidth label={tf('supplyQuantityOptional')} value={cantidadInsumo} onChange={(e) => setCantidadInsumo(e.target.value)} type="number" inputProps={{ min: 0, step: 0.001 }} />
									<TextField fullWidth label={tf('laborCost')} value={costoManoObra} onChange={(e) => setCostoManoObra(e.target.value)} type="number" inputProps={{ min: 0, step: 0.01 }} />
									<TextField fullWidth label={tf('otherCost')} value={costoOtros} onChange={(e) => setCostoOtros(e.target.value)} type="number" inputProps={{ min: 0, step: 0.01 }} />
								</Stack>
							</>
						)}

						<TextField fullWidth label={tf('notes')} value={observaciones} onChange={(e) => setObservaciones(e.target.value)} multiline minRows={3} />
					</Stack>
				</DialogContent>
				<DialogActions>
					<Button onClick={closeEdit} disabled={editSaving}>{tc('cancel')}</Button>
					<Button onClick={() => void saveEdit()} variant="contained" disabled={editSaving || !canSave || !canCreateRecord}>{tc('save')}</Button>
				</DialogActions>
			</Dialog>

			<Dialog open={annulOpen} onClose={closeAnnul} fullWidth maxWidth="xs">
				<DialogTitle>{tc('annul')}</DialogTitle>
				<DialogContent dividers>
					<Stack spacing={2} sx={{ pt: 1 }}>
						{annulError ? <Alert severity="error">{annulError}</Alert> : null}
						<Typography variant="body2">
							{annulTarget?.tipoOperacion === 'Cosecha'
								? tproduction('confirmAnnulHarvest')
								: tproduction('confirmAnnulAgriculturalOperation')}
						</Typography>
						{annulTarget ? (
							<Typography variant="body2" color="text.secondary">
								#{annulTarget.operacionAgricolaId} · {annulTarget.fincaNombre} / {annulTarget.parcelaNombre}{annulTarget.codigoLote ? ` · ${annulTarget.codigoLote}` : ''}
							</Typography>
						) : null}
					</Stack>
				</DialogContent>
				<DialogActions>
					<Button onClick={closeAnnul} disabled={annulSaving}>{tc('cancel')}</Button>
					<Button onClick={() => void confirmAnnul()} color="warning" variant="contained" disabled={annulSaving || !annulTarget || !canAnnul}>{tc('annul')}</Button>
				</DialogActions>
			</Dialog>
		</Stack>
	);
}
