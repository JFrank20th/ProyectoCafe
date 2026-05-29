// ==============================|| OVERRIDES - LIST ITEM BUTTON ||============================== //

import type { Theme } from '@mui/material/styles';

export default function ListItemButton(theme: Theme) {
  const vars = theme.vars ?? { palette: theme.palette };
  const highlight = vars.palette.primary;

  return {
    MuiListItemButton: {
      styleOverrides: {
        root: {
          color: vars.palette.text.primary,
          paddingTop: '10px',
          paddingBottom: '10px',

          '&.Mui-selected': {
            color: highlight.dark,
            backgroundColor: highlight.light,
            '&:hover': {
              backgroundColor: highlight.light
            },
            '& .MuiListItemIcon-root': {
              color: highlight.dark
            }
          },

          '&:hover': {
            backgroundColor: highlight.light,
            color: highlight.dark,
            '& .MuiListItemIcon-root': {
              color: highlight.dark
            }
          }
        }
      }
    }
  };
}
