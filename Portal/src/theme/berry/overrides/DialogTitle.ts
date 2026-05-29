// ==============================|| OVERRIDES - DIALOG TITLE ||============================== //

import type { Theme } from '@mui/material/styles';

import { withAlpha } from '../color-utils';

export default function DialogTitle(theme: Theme) {
  const vars = theme.vars ?? { palette: theme.palette };

  return {
    MuiDialogTitle: {
      styleOverrides: {
        root: {
          padding: '24px 24px 16px',
          fontSize: '1.125rem',
          fontWeight: 700,
          letterSpacing: '-0.01em',
          color: vars.palette.text.dark ?? vars.palette.text.primary,
          borderBottom: `1px solid ${withAlpha(vars.palette.divider, 0.72)}`,
          backgroundColor: withAlpha(vars.palette.grey[50], 0.72)
        }
      }
    }
  };
}
