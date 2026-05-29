// ==============================|| OVERRIDES - OUTLINED INPUT ||============================== //

import type { Theme } from '@mui/material/styles';

export default function OutlinedInput(theme: Theme, borderRadius: number, outlinedFilled: boolean) {
  const vars = theme.vars ?? { palette: theme.palette };

  return {
    MuiOutlinedInput: {
      styleOverrides: {
        root: {
          background: outlinedFilled ? vars.palette.grey[50] : 'transparent',
          borderRadius: `${borderRadius}px`,

          '& .MuiOutlinedInput-notchedOutline': {
            borderColor: vars.palette.grey[400]
          },

          '&:hover $notchedOutline': {
            borderColor: vars.palette.primary.light
          },

          '&.MuiInputBase-multiline': {
            padding: 1
          }
        },
        input: {
          fontWeight: 500,
          background: outlinedFilled ? vars.palette.grey[50] : 'transparent',
          padding: '15.5px 14px',
          borderRadius: `${borderRadius}px`,

          '&.MuiInputBase-inputSizeSmall': {
            padding: '10px 14px',

            '&.MuiInputBase-inputAdornedStart': {
              paddingLeft: 0
            }
          }
        },
        inputAdornedStart: {
          paddingLeft: 4
        },
        notchedOutline: {
          borderRadius: `${borderRadius}px`
        }
      }
    }
  };
}
