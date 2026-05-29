// ==============================|| OVERRIDES - LIST ITEM ICON ||============================== //

import type { Theme } from '@mui/material/styles';

export default function ListItemIcon(theme: Theme) {
  const vars = theme.vars ?? { palette: theme.palette };

  return {
    MuiListItemIcon: {
      styleOverrides: {
        root: {
          color: vars.palette.text.primary,
          minWidth: '36px'
        }
      }
    }
  };
}
