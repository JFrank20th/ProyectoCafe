import type { CSSProperties } from 'react';

declare module '@mui/material/styles' {
  interface Palette {
    orange: Palette['primary'];
    dark: Palette['primary'];
  }

  interface PaletteOptions {
    orange?: PaletteOptions['primary'];
    dark?: PaletteOptions['primary'];
  }

  interface PaletteColor {
    200?: string;
    800?: string;
    900?: string;
  }

  interface SimplePaletteColorOptions {
    200?: string;
    800?: string;
    900?: string;
  }

  interface TypeText {
    dark?: string;
    hint?: string;
    heading?: string;
  }

  interface TypographyVariants {
    commonAvatar: CSSProperties;
    smallAvatar: CSSProperties;
    mediumAvatar: CSSProperties;
    largeAvatar: CSSProperties;
  }

  interface TypographyVariantsOptions {
    commonAvatar?: CSSProperties;
    smallAvatar?: CSSProperties;
    mediumAvatar?: CSSProperties;
    largeAvatar?: CSSProperties;
  }

  // Útil para portar patrones de Berry sin forzar `any`
  interface Theme {
    customShadows?: Record<string, string>;
  }

  interface ThemeOptions {
    customShadows?: Record<string, string>;
  }
}
