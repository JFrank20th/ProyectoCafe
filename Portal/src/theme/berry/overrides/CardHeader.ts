// ==============================|| OVERRIDES - CARD HEADER ||============================== //

import type { Theme } from '@mui/material/styles';

export default function CardHeader(theme: Theme) {
  const vars = theme.vars ?? { palette: theme.palette };

  return {
    MuiCardHeader: {
      styleOverrides: {
        root: {
          color: vars.palette.text.dark ?? vars.palette.text.primary,
          padding: '24px'
        },
        title: {
          fontSize: '1.125rem'
        }
      }
    }
  };
}
