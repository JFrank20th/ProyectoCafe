// ==============================|| OVERRIDES - INPUT BASE ||============================== //

import type { Theme } from '@mui/material/styles';

export default function InputBase(theme: Theme) {
  const vars = theme.vars ?? { palette: theme.palette };

  return {
    MuiInputBase: {
      styleOverrides: {
        input: {
          color: vars.palette.text.dark ?? vars.palette.text.primary,
          '&::placeholder': {
            color: vars.palette.text.secondary,
            fontSize: '0.875rem'
          }
        }
      }
    }
  };
}
