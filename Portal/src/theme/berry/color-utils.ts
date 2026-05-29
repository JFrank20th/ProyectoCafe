import { alpha } from '@mui/material/styles';

/**
 * Convierte un color hex a un string de canal RGB: "r g b".
 *
 * Soporta formatos: #RGB, #RGBA, #RRGGBB, #RRGGBBAA (ignora el canal alpha).
 */
export function hexToRgbChannel(hex: string) {
  let cleaned = hex.replace(/^#/, '');

  if (cleaned.length === 3 || cleaned.length === 4) {
    cleaned = cleaned
      .split('')
      .map((c) => c + c)
      .join('');
  }

  if (cleaned.length !== 6 && cleaned.length !== 8) {
    throw new Error(`Invalid hex color: ${hex}`);
  }

  const r = parseInt(cleaned.substring(0, 2), 16);
  const g = parseInt(cleaned.substring(2, 4), 16);
  const b = parseInt(cleaned.substring(4, 6), 16);

  return `${r} ${g} ${b}`;
}

/**
 * Extiende un objeto de paleta agregando campos "*Channel" en los valores hex.
 *
 * Esto ayuda a que MUI/CSS vars puedan calcular transparencias consistentemente.
 */
export function extendPaletteWithChannels<T extends Record<string, unknown>>(palette: T): T {
  const result: Record<string, unknown> = { ...palette };

  Object.entries(palette).forEach(([key, value]) => {
    if (typeof value === 'string' && value.startsWith('#')) {
      result[`${key}Channel`] = hexToRgbChannel(value);
      return;
    }

    if (typeof value === 'object' && value !== null) {
      result[key] = extendPaletteWithChannels(value as Record<string, unknown>);
    }
  });

  return result as T;
}

export function withAlpha(color: string, opacity: number) {
  // Caso 1: color normal (hex, rgb, hsl…)
  if (/^#|rgb|hsl|color/i.test(color)) {
    return alpha(color, opacity);
  }

  // Caso 2: CSS var: var(--mui-palette-xxx) o var(--palette-xxx, #hex)
  if (color.startsWith('var(')) {
    return color
      .replace(/(--[a-zA-Z0-9-]+)(.*)\)/, `$1Channel$2)`)
      .replace(/^var\((.+)\)$/, `rgba(var($1) / ${opacity})`);
  }

  return color;
}
