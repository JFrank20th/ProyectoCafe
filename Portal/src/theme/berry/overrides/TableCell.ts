// ==============================|| OVERRIDES - TABLE CELL ||============================== //

import type { Theme } from '@mui/material/styles';

import { withAlpha } from '../color-utils';

export default function TableCell(theme: Theme) {
  const vars = theme.vars ?? { palette: theme.palette };

  return {
    MuiTableCell: {
      styleOverrides: {
        root: {
          borderColor: withAlpha(vars.palette.divider, 0.92),
          paddingTop: theme.spacing(1.75),
          paddingBottom: theme.spacing(1.75),

          '&.MuiTableCell-head': {
            fontSize: '0.75rem',
            letterSpacing: '0.08em',
            textTransform: 'uppercase',
            color: vars.palette.text.secondary,
            fontWeight: 700,
            backgroundColor: withAlpha(vars.palette.grey[50], 0.9)
          },

          '&.MuiTableCell-body': {
            color: vars.palette.text.primary
          },

          '&.MuiTableCell-footer': {
            color: vars.palette.text.secondary,
            fontWeight: 600,
            backgroundColor: withAlpha(vars.palette.grey[50], 0.72)
          }
        }
      }
    }
  };
}
