// ==============================|| OVERRIDES - TABS ||============================== //

import type { Theme } from '@mui/material/styles';

export default function Tabs(theme: Theme) {
  const vars = theme.vars ?? { palette: theme.palette };

  return {
    MuiTabs: {
      styleOverrides: {
        flexContainer: {
          borderBottom: '1px solid',
          borderColor: vars.palette.grey[200]
        }
      }
    }
  };
}
