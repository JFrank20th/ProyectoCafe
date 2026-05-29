// ==============================|| OVERRIDES - DIALOG ||============================== //

import type { Theme } from '@mui/material/styles';

import { withAlpha } from '../color-utils';

export default function Dialog(theme: Theme) {
  const vars = theme.vars ?? { palette: theme.palette };
  const borderRadius = typeof theme.shape.borderRadius === 'number'
    ? `${theme.shape.borderRadius + 8}px`
    : theme.shape.borderRadius;

  return {
    MuiDialog: {
      styleOverrides: {
        container: {
          padding: theme.spacing(2)
        },
        paper: {
          padding: 0,
          overflow: 'hidden',
          borderRadius,
          border: `1px solid ${withAlpha(vars.palette.divider, 0.9)}`,
          backgroundColor: vars.palette.background.paper,
          backgroundImage: 'none',
          boxShadow: theme.shadows[18]
        }
      }
    },
    MuiDialogContent: {
      styleOverrides: {
        root: {
          padding: '20px 24px 24px',
          color: vars.palette.text.secondary,
          backgroundColor: vars.palette.background.paper
        }
      }
    },
    MuiDialogActions: {
      styleOverrides: {
        root: {
          gap: theme.spacing(1.5),
          padding: '16px 24px 24px',
          borderTop: `1px solid ${withAlpha(vars.palette.divider, 0.8)}`,
          backgroundColor: withAlpha(vars.palette.grey[50], 0.72)
        }
      }
    }
  };
}
