import { withAlpha } from '../color-utils';

import type { ChipProps } from '@mui/material/Chip';
import type { Theme } from '@mui/material/styles';

// ===============================||  OVERRIDES - CHIP  ||=============================== //

type ChipOwnerState = {
  color?: ChipProps['color'];
};

type ChipVariantArgs = {
  ownerState: ChipOwnerState;
  theme: Theme;
};

function getSemanticChipOpacity(color: ChipProps['color']) {
  switch (color) {
    case 'warning':
      return 0.22;
    case 'success':
      return 0.18;
    case 'error':
      return 0.14;
    case 'info':
    case 'secondary':
      return 0.12;
    case 'primary':
    default:
      return 0.1;
  }
}

export default function Chip(theme: Theme) {
  const vars = theme.vars ?? { palette: theme.palette };

  return {
    MuiChip: {
      defaultProps: {
        color: 'primary',
        variant: 'light'
      },
      styleOverrides: {
        root: {
          variants: [
            {
              props: { variant: 'light' },
              style: ({ ownerState, theme }: ChipVariantArgs) => {
                const innerVars = theme.vars ?? { palette: theme.palette };
                const colorKey = ownerState.color;

                const paletteColor =
                  colorKey === 'primary'
                    ? innerVars.palette.primary
                    : colorKey === 'secondary'
                      ? innerVars.palette.secondary
                      : colorKey === 'error'
                        ? innerVars.palette.error
                        : colorKey === 'warning'
                          ? innerVars.palette.warning
                          : colorKey === 'success'
                            ? innerVars.palette.success
                            : colorKey === 'info'
                              ? innerVars.palette.info
                              : undefined;

                if (!paletteColor) return {};

                const surfaceOpacity = getSemanticChipOpacity(ownerState.color);

                return {
                  color: paletteColor.dark ?? paletteColor.main,
                  backgroundColor: withAlpha(paletteColor.main, surfaceOpacity),
                  border: `1px solid ${withAlpha(paletteColor.main, 0.18)}`,
                  fontWeight: 600,

                  '& .MuiChip-icon, & .MuiChip-deleteIcon': {
                    color: 'inherit'
                  },

                  '&.MuiChip-clickable': {
                    '&:hover': {
                      color:
                        ownerState.color === 'warning'
                          ? innerVars.palette.common.black
                          : innerVars.palette.common.white,
                      backgroundColor: paletteColor.main,
                      borderColor: paletteColor.main
                    }
                  }
                };
              }
            },
            {
              props: { variant: 'light', color: 'default' },
              style: {
                color: vars.palette.grey[700],
                backgroundColor: vars.palette.grey[200],
                border: `1px solid ${vars.palette.grey[300]}`,
                fontWeight: 600
              }
            },
            {
              props: { variant: 'outlined', color: 'warning' },
              style: {
                borderColor: vars.palette.warning.dark,
                color: vars.palette.warning.dark
              }
            },
            {
              props: { variant: 'outlined', color: 'success' },
              style: {
                borderColor: vars.palette.success.dark,
                color: vars.palette.success.dark
              }
            },
            {
              props: { variant: 'outlined', color: 'info' },
              style: {
                borderColor: vars.palette.info.dark,
                color: vars.palette.info.dark
              }
            },
            {
              props: { variant: 'outlined', color: 'secondary' },
              style: {
                borderColor: vars.palette.secondary.dark,
                color: vars.palette.secondary.dark
              }
            }
          ],
          '&.MuiChip-deletable .MuiChip-deleteIcon': {
            color: 'inherit'
          }
        }
      }
    }
  };
}
