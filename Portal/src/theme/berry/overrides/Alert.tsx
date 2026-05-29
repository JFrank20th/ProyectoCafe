import { withAlpha } from '../color-utils';

import InfoOutlinedIcon from '@mui/icons-material/InfoOutlined';

import type { AlertProps } from '@mui/material/Alert';
import type { Theme } from '@mui/material/styles';

// ==============================|| OVERRIDES - ALERT ||============================== //

type AlertOwnerState = {
  severity?: AlertProps['severity'];
};

type VariantArgs = {
  ownerState: AlertOwnerState;
};

export default function Alert(theme: Theme) {
  const vars = theme.vars ?? { palette: theme.palette };

  const getPaletteColor = (severity: AlertProps['severity'] | undefined) =>
    severity ? vars.palette[severity] : vars.palette.info;

  const getCommonStyles = (ownerState: AlertOwnerState) => {
    const paletteColor = getPaletteColor(ownerState.severity);
    const isWarning = ownerState.severity === 'warning';

    return {
      paletteColor,
      textColor: paletteColor.dark ?? paletteColor.main,
      filledTextColor: isWarning ? vars.palette.common.black : vars.palette.common.white,
    };
  };

  const standardVariant = ({ ownerState }: VariantArgs) => {
    const { paletteColor, textColor } = getCommonStyles(ownerState);

    return {
      color: textColor,
      backgroundColor: withAlpha(paletteColor.main, 0.12),
      border: `1px solid ${withAlpha(paletteColor.main, 0.16)}`,
      '& .MuiAlert-icon': { color: textColor }
    };
  };

  const outlinedVariant = ({ ownerState }: VariantArgs) => {
    const { paletteColor, textColor } = getCommonStyles(ownerState);

    return {
      color: textColor,
      backgroundColor: withAlpha(paletteColor.main, 0.04),
      borderColor: withAlpha(paletteColor.main, 0.34),
      '& .MuiAlert-icon': { color: textColor }
    };
  };

  const filledVariant = ({ ownerState }: VariantArgs) => {
    const { paletteColor, filledTextColor } = getCommonStyles(ownerState);

    return {
      color: filledTextColor,
      backgroundColor: paletteColor.main,
      '& .MuiAlert-icon': {
        color: filledTextColor
      }
    };
  };

  return {
    MuiAlert: {
      defaultProps: {
        iconMapping: {
          primary: <InfoOutlinedIcon sx={{ fontSize: 'inherit' }} />
        }
      },
      styleOverrides: {
        root: {
          alignItems: 'center',
          borderRadius:
            typeof theme.shape.borderRadius === 'number'
              ? `${theme.shape.borderRadius + 4}px`
              : theme.shape.borderRadius,
          backgroundImage: 'none',
          variants: [
            { props: { variant: 'standard' }, style: standardVariant },
            { props: { variant: 'outlined' }, style: outlinedVariant },
            { props: { variant: 'filled' }, style: filledVariant }
          ]
        },
        outlined: { border: '1px dashed' }
      }
    }
  };
}
