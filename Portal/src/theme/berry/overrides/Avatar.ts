// ==============================|| OVERRIDES - AVATAR ||============================== //

import type { Theme } from '@mui/material/styles';

import { withAlpha } from '../color-utils';

export default function Avatar(theme: Theme) {
  const vars = theme.vars ?? { palette: theme.palette };
  const primary = vars.palette.primary;

  return {
    MuiAvatar: {
      styleOverrides: {
        root: {
          '--avatar-default-color': primary.dark,
          '--avatar-default-bg': primary[200] ?? primary.light,

          color: 'var(--avatar-default-color)',
          backgroundColor: 'var(--avatar-default-bg)',

          // En oscuro se usa una superficie tintada para no dejar bloques claros
          // sobre el fondo oscuro.
          ...theme.applyStyles('dark', {
            '--avatar-default-color': primary.light,
            '--avatar-default-bg': withAlpha(primary.main, 0.22),
          }),
        },
      },
    },
  };
}
