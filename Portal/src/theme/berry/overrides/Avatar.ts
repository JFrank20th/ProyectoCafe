// ==============================|| OVERRIDES - AVATAR ||============================== //

import type { Theme } from '@mui/material/styles';

export default function Avatar(theme: Theme) {
  const vars = theme.vars ?? { palette: theme.palette };

  return {
    MuiAvatar: {
      styleOverrides: {
        root: {
          '--avatar-default-color': vars.palette.primary.dark,
          '--avatar-default-bg': vars.palette.primary[200] ?? vars.palette.primary.light,

          color: 'var(--avatar-default-color)',
          backgroundColor: 'var(--avatar-default-bg)'
        }
      }
    }
  };
}
