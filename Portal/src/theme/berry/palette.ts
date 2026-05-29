import { berryDefaultColors } from './default-colors';
import { extendPaletteWithChannels } from './color-utils';

// ==============================|| DEFAULT THEME - PALETTE ||============================== //

export function buildPalette() {
  const c = berryDefaultColors;

  const lightColors = {
    primary: {
      light: c.primaryLight,
      main: c.primaryMain,
      dark: c.primaryDark,
      200: c.primary200,
      800: c.primary800,
    },
    secondary: {
      light: c.secondaryLight,
      main: c.secondaryMain,
      dark: c.secondaryDark,
      200: c.secondary200,
      800: c.secondary800,
    },
    error: {
      light: c.errorLight,
      main: c.errorMain,
      dark: c.errorDark,
    },
    info: {
      light: c.infoLight,
      main: c.infoMain,
      dark: c.infoDark,
    },
    orange: {
      light: c.orangeLight,
      main: c.orangeMain,
      dark: c.orangeDark,
    },
    warning: {
      light: c.warningLight,
      main: c.warningMain,
      dark: c.warningDark,
      contrastText: c.grey900,
    },
    success: {
      light: c.successLight,
      200: c.success200,
      main: c.successMain,
      dark: c.successDark,
    },
    grey: {
      50: c.grey50,
      100: c.grey100,
      200: c.grey200,
      300: c.grey300,
      400: c.grey400,
      500: c.grey500,
      600: c.grey600,
      700: c.grey700,
      900: c.grey900,
    },
    dark: {
      light: c.darkTextPrimary,
      main: c.darkLevel1,
      dark: c.darkLevel2,
      800: c.darkBackground,
      900: c.darkPaper,
    },
    text: {
      primary: c.grey700,
      secondary: c.grey500,
      dark: c.grey900,
      hint: c.grey200,
      heading: c.grey900,
    },
    divider: c.grey300,
    background: {
      paper: c.paper,
      default: c.grey100,
    },
  };

  const darkColors = {
    primary: {
      light: c.darkPrimaryLight,
      main: c.darkPrimaryMain,
      dark: c.darkPrimaryDark,
      200: c.darkPrimary200,
      800: c.darkPrimary800,
    },
    secondary: {
      light: c.darkSecondaryLight,
      main: c.darkSecondaryMain,
      dark: c.darkSecondaryDark,
      200: c.darkSecondary200,
      800: c.darkSecondary800,
    },
    error: {
      light: c.errorLight,
      main: c.errorMain,
      dark: c.errorDark,
    },
    info: {
      light: c.infoLight,
      main: c.infoMain,
      dark: c.infoDark,
    },
    orange: {
      light: c.orangeLight,
      main: c.orangeMain,
      dark: c.orangeDark,
    },
    warning: {
      light: c.warningLight,
      main: c.warningMain,
      dark: c.warningDark,
      contrastText: c.darkPaper,
    },
    success: {
      light: c.successLight,
      200: c.success200,
      main: c.successMain,
      dark: c.successDark,
    },
    grey: {
      50: c.darkLevel2,
      100: c.darkLevel1,
      200: c.darkBackground,
      300: c.darkPaper,
      400: c.darkBorder,
      500: c.darkTextSecondary,
      600: c.darkTextSecondary,
      700: c.darkTextPrimary,
      900: c.darkTextTitle,
    },
    dark: {
      light: c.darkTextPrimary,
      main: c.darkLevel1,
      dark: c.darkLevel2,
      800: c.darkBackground,
      900: c.darkPaper,
    },
    text: {
      primary: c.darkTextPrimary,
      secondary: c.darkTextSecondary,
      dark: c.darkTextTitle,
      hint: c.darkTextSecondary,
      heading: c.darkTextTitle,
    },
    divider: c.darkBorder,
    background: {
      paper: c.darkPaper,
      default: c.darkBackground,
    },
  };

  const commonColor = { common: { black: c.darkPaper, white: '#fff' } };

  const extendedCommon = extendPaletteWithChannels(commonColor);
  const extendedLight = extendPaletteWithChannels(lightColors);
  const extendedDark = extendPaletteWithChannels(darkColors);

  return {
    light: {
      mode: 'light',
      ...extendedCommon,
      ...extendedLight,
    },
    dark: {
      mode: 'dark',
      ...extendedCommon,
      ...extendedDark,
    },
  };
}
