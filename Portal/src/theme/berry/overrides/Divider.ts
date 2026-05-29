// ==============================|| OVERRIDES - DIVIDER ||============================== //

import type { Theme } from '@mui/material/styles';

export default function Divider(theme: Theme) {
  const vars = theme.vars ?? { palette: theme.palette };

  return {
    MuiDivider: {
      styleOverrides: {
        root: {
          borderColor: vars.palette.divider
        }
      }
    }
  };
}
