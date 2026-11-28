// ==============================|| OVERRIDES - LIST ITEM BUTTON ||============================== //

import type { Theme } from '@mui/material/styles';

import { withAlpha } from '../color-utils';

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
              backgroundColor: highlight.light,
            },
            '& .MuiListItemIcon-root': {
              color: highlight.dark,
            },

            // En oscuro el fondo pastel se cambia por un tinte translúcido
            // para mantener legible el texto claro.
            ...theme.applyStyles('dark', {
              color: highlight.light,
              backgroundColor: withAlpha(highlight.main, 0.18),
              '&:hover': {
                backgroundColor: withAlpha(highlight.main, 0.24),
              },
              '& .MuiListItemIcon-root': {
                color: highlight.light,
              },
            }),
          },

          '&:hover': {
            backgroundColor: highlight.light,
            color: highlight.dark,
            '& .MuiListItemIcon-root': {
              color: highlight.dark,
            },

            ...theme.applyStyles('dark', {
              backgroundColor: withAlpha(highlight.main, 0.12),
              color: highlight.light,
              '& .MuiListItemIcon-root': {
                color: highlight.light,
              },
            }),
          },
        },
      },
    },
  };
}
