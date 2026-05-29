export type UiConfigState = {
  fontFamily: string;
  borderRadius: number;
};

export const UI_CONFIG_STORAGE_KEY = 'proyectocafe-ui-config';

export const UI_CONFIG_DEFAULTS: UiConfigState = {
  fontFamily: `'Roboto', sans-serif`,
  borderRadius: 8,
};

export const UI_FONT_FAMILIES: Array<{ id: string; value: string; label: string }> = [
  { id: 'inter', value: `'Inter', sans-serif`, label: 'Inter' },
  { id: 'poppins', value: `'Poppins', sans-serif`, label: 'Poppins' },
  { id: 'roboto', value: `'Roboto', sans-serif`, label: 'Roboto' },
];
