import { withAlpha } from './color-utils';

type CustomShadowsPalette = {
  primary: { main: string };
  secondary: { main: string };
  orange: { main: string };
  success: { main: string };
  warning: { main: string };
  error: { main: string };
  grey?: Record<number, string>;
  common?: { black?: string };
};

function createCustomShadow(palette: CustomShadowsPalette, baseColor: string) {
  const transparent = withAlpha(baseColor, 0.24);
  const commonShadow = (color: string) => `0px 12px 14px 0px ${withAlpha(color, 0.3)}`;

  return {
    z1: `0 1px 2px 0 ${transparent}`,
    z8: `0 8px 16px 0 ${transparent}`,
    z12: `0 12px 24px 0 ${transparent}, 0 10px 20px 0 ${transparent}`,
    z16: `0 0 3px 0 ${transparent}, 0 14px 28px -5px ${transparent}`,
    z20: `0 0 3px 0 ${transparent}, 0 18px 36px -5px ${transparent}`,
    z24: `0 0 6px 0 ${transparent}, 0 21px 44px 0 ${transparent}`,

    primary: commonShadow(palette.primary.main),
    secondary: commonShadow(palette.secondary.main),
    orange: commonShadow(palette.orange.main),
    success: commonShadow(palette.success.main),
    warning: commonShadow(palette.warning.main),
    error: commonShadow(palette.error.main),
  };
}

export function buildCustomShadows(palette: CustomShadowsPalette) {
  const baseColor = palette.grey?.[900] ?? palette.common?.black ?? '#000';
  return createCustomShadow(palette, baseColor);
}
