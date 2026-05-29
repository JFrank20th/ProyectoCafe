// ==============================|| OVERRIDES - BUTTON ||============================== //

import type { Theme } from '@mui/material/styles';

export default function Button(theme: Theme) {
  const vars = theme.vars ?? { palette: theme.palette };

  return {
    MuiSlider: {
      styleOverrides: {
        root: {
          '&.Mui-disabled': {
            color: vars.palette.grey[300]
          }
        },
        mark: {
          backgroundColor: vars.palette.background.paper,
          width: '4px'
        },
        valueLabel: {
          color: vars.palette.primary.light
        }
      }
    }
  };
}
