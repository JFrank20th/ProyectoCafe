// ==============================|| OVERRIDES - DATA GRID ||============================== //

import { withAlpha } from '../color-utils';

import type { Theme } from '@mui/material/styles';

export default function DataGrid(theme: Theme) {
  const vars = theme.vars ?? { palette: theme.palette };

  return {
    MuiDataGrid: {
      defaultProps: {
        rowHeight: 54
      },
      styleOverrides: {
        root: {
          borderWidth: 1,
          borderStyle: 'solid',
          borderColor: vars.palette.divider,
          borderRadius:
            typeof theme.shape.borderRadius === 'number'
              ? `${theme.shape.borderRadius + 8}px`
              : theme.shape.borderRadius,
          overflow: 'hidden',
          backgroundColor: vars.palette.background.paper,

          '& .MuiDataGrid-columnHeader--filledGroup': {
            borderBottomWidth: 0
          },

          '& .MuiDataGrid-columnHeader--emptyGroup': {
            borderBottomWidth: 0
          },

          '& .MuiFormControl-root>.MuiInputBase-root': {
            backgroundColor: `${vars.palette.background.paper} !important`,
            borderColor: `${vars.palette.divider} !important`
          },

          '& .MuiDataGrid-columnHeaders, & .MuiDataGrid-filler': {
            backgroundColor: vars.palette.grey[50],
            borderBottom: `1px solid ${vars.palette.divider}`
          },

          '& .MuiDataGrid-row:hover': {
            backgroundColor: withAlpha(vars.palette.primary.main, 0.05)
          },

          '& .MuiDataGrid-row.Mui-selected': {
            backgroundColor: withAlpha(vars.palette.primary.main, 0.09),

            '&:hover': {
              backgroundColor: withAlpha(vars.palette.primary.main, 0.12)
            }
          },

          '& .MuiDataGrid-cell:focus, & .MuiDataGrid-columnHeader:focus': {
            outline: `2px solid ${withAlpha(vars.palette.primary.main, 0.18)}`,
            outlineOffset: '-2px'
          }
        },
        withBorderColor: {
          borderColor: vars.palette.divider
        },
        toolbarContainer: {
          backgroundColor: vars.palette.grey[50],
          borderBottom: `1px solid ${vars.palette.divider}`,

          '& .MuiButton-root': {
            paddingLeft: '16px !important',
            paddingRight: '16px !important'
          }
        },
        columnHeader: {
          color: vars.palette.grey[700],
          fontWeight: 700,
          paddingLeft: 24,
          paddingRight: 24
        },
        footerContainer: {
          '&.MuiDataGrid-withBorderColor': {
            borderBottom: 'none'
          },
          backgroundColor: vars.palette.grey[50]
        },
        columnHeaderCheckbox: {
          paddingLeft: 0,
          paddingRight: 0
        },
        cellCheckbox: {
          paddingLeft: 0,
          paddingRight: 0
        },
        cell: {
          borderWidth: 1,
          paddingLeft: 24,
          paddingRight: 24,
          borderColor: vars.palette.divider,
          color: vars.palette.text.primary,

          '&.MuiDataGrid-cell--withRenderer > div': {
            ' > .high': {
              backgroundColor: vars.palette.success.light,
              color: vars.palette.success.dark,
              border: `1px solid ${withAlpha(vars.palette.success.main, 0.18)}`,
              borderRadius: '999px',
              display: 'inline-flex',
              alignItems: 'center',
              padding: '4px 10px',
              fontWeight: 700
            },
            '& > .medium': {
              backgroundColor: vars.palette.warning.light,
              color: vars.palette.warning.dark,
              border: `1px solid ${withAlpha(vars.palette.warning.main, 0.2)}`,
              borderRadius: '999px',
              display: 'inline-flex',
              alignItems: 'center',
              padding: '4px 10px',
              fontWeight: 700
            },
            '& > .low': {
              backgroundColor: vars.palette.error.light,
              color: vars.palette.error.dark,
              border: `1px solid ${withAlpha(vars.palette.error.main, 0.18)}`,
              borderRadius: '999px',
              display: 'inline-flex',
              alignItems: 'center',
              padding: '4px 10px',
              fontWeight: 700
            }
          }
        }
      }
    }
  };
}
