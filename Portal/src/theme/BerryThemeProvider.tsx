'use client';

import type { ReactNode } from 'react';
import { useMemo } from 'react';

import { createTheme, StyledEngineProvider, ThemeProvider, type ThemeOptions } from '@mui/material/styles';
import CssBaseline from '@mui/material/CssBaseline';

import { buildPalette } from '@/theme/berry/palette';
import { buildTypography } from '@/theme/berry/typography';
import { buildCustomShadows } from '@/theme/berry/custom-shadows';
import componentsOverrides from '@/theme/berry/overrides';
import { useUiConfig } from '@/hooks/useUiConfig';

const CSS_VAR_PREFIX = '';
const DEFAULT_THEME_MODE: 'light' | 'dark' | 'system' = 'system';

export function BerryThemeProvider({ children }: { children: ReactNode }) {
  const {
    state: { borderRadius, fontFamily },
  } = useUiConfig();
  const outlinedFilled = true;

  const palette = useMemo(() => buildPalette(), []);
  const themeTypography = useMemo(() => buildTypography(fontFamily), [fontFamily]);

  const themeOptions = useMemo(
    () =>
      ({
        direction: 'ltr',
        shape: { borderRadius },
        mixins: {
          toolbar: {
            minHeight: '48px',
            padding: '16px',
            '@media (min-width: 600px)': {
              minHeight: '48px',
            },
          },
        },
        typography: themeTypography,
        colorSchemes: {
          light: {
            palette: palette.light,
            customShadows: buildCustomShadows(palette.light),
          },
          dark: {
            palette: palette.dark,
            customShadows: buildCustomShadows(palette.dark),
          },
        },
        cssVariables: {
          cssVarPrefix: CSS_VAR_PREFIX,
          colorSchemeSelector: 'data-color-scheme',
        },
      }) as unknown as ThemeOptions,
    [themeTypography, palette, borderRadius]
  );

  const theme = useMemo(() => {
    const nextTheme = createTheme(themeOptions);
    nextTheme.components = componentsOverrides(nextTheme, borderRadius, outlinedFilled);
    return nextTheme;
  }, [themeOptions, borderRadius, outlinedFilled]);

  return (
    <StyledEngineProvider injectFirst>
      <ThemeProvider
        disableTransitionOnChange
        theme={theme}
        modeStorageKey="theme-mode"
        defaultMode={DEFAULT_THEME_MODE}
      >
        <CssBaseline enableColorScheme />
        {children}
      </ThemeProvider>
    </StyledEngineProvider>
  );
}
