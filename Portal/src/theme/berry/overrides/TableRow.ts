// ==============================|| OVERRIDES - TABLE ROW ||============================== //

import type { Theme } from '@mui/material/styles';

import { withAlpha } from '../color-utils';

export default function TableRow(theme: Theme) {
  const vars = theme.vars ?? { palette: theme.palette };

  return {
    MuiTableRow: {
      styleOverrides: {
        root: {
          transition: 'background-color 160ms ease, border-color 160ms ease',

          '&:last-of-type .MuiTableCell-root': {
            borderBottom: 'none'
          },

          '&.MuiTableRow-hover:hover': {
            backgroundColor: withAlpha(vars.palette.primary.main, 0.05)
          },

          '&.Mui-selected': {
            backgroundColor: withAlpha(vars.palette.primary.main, 0.1)
          },

          '&.Mui-selected:hover': {
            backgroundColor: withAlpha(vars.palette.primary.main, 0.14)
          }
        }
      }
    }
  };
}