import type { Theme } from '@mui/material/styles';

import { withAlpha } from './color-utils';

export type SoftAccentColor = 'primary' | 'secondary' | 'warning' | 'orange' | 'success' | 'error' | 'info';

/**
 * Superficie de acento suave para avatares, chips y elementos destacados.
 *
 * En modo claro conserva el patrón original de la identidad visual: fondo
 * pastel con texto oscuro. En modo oscuro cambia a un fondo tintado translúcido
 * con texto claro, evitando superficies claras sobre el tema oscuro.
 */
export function softAccentSx(theme: Theme, color: SoftAccentColor = 'primary', withHover = true) {
  const palette = theme.vars?.palette[color] ?? theme.palette[color];

  const styles = {
    color: palette.dark,
    backgroundColor: palette.light,
    ...theme.applyStyles('dark', {
      color: palette.light,
      backgroundColor: withAlpha(palette.main, 0.22),
    }),
  };

  if (!withHover) return styles;

  return {
    ...styles,
    '&:hover': {
      color: palette.light,
      backgroundColor: palette.dark,
      ...theme.applyStyles('dark', {
        color: palette.contrastText,
        backgroundColor: palette.main,
      }),
    },
  };
}
