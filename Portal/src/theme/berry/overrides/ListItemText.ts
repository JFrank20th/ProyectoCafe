// ==============================|| OVERRIDES - LIST ITEM TEXT ||============================== //

import type { Theme } from '@mui/material/styles';

export default function ListItemText(theme: Theme) {
  const vars = theme.vars ?? { palette: theme.palette };

  return {
    MuiListItemText: {
      styleOverrides: {
        primary: {
          color: vars.palette.text.dark ?? vars.palette.text.primary
        }
      }
    }
  };
}
