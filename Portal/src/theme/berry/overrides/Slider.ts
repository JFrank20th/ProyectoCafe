// ==============================|| OVERRIDES - SLIDER ||============================== //

import type { Theme } from '@mui/material/styles';

export default function Slider(theme: Theme) {
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
          color: vars.palette.primary.light,
          // El fondo por defecto (grey[600]) se aclara en oscuro y deja el
          // texto casi invisible. Se usa el verde de acento con contraste.
          ...theme.applyStyles('dark', {
            backgroundColor: vars.palette.primary.main,
            color: vars.palette.primary.contrastText
          })
        }
      }
    }
  };
}
