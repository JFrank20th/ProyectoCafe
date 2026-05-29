// ==============================|| OVERRIDES - TYPOGRAPHY ||============================== //

import type { Theme } from '@mui/material/styles';

export default function Typography(theme: Theme) {
  const vars = theme.vars ?? { palette: theme.palette };
  const headingColor = vars.palette.text.heading ?? vars.palette.text.primary;

  return {
    MuiTypography: {
      styleOverrides: {
        root: {
          variants: [
            {
              props: { variant: 'h1' },
              style: { color: headingColor }
            },
            {
              props: { variant: 'h2' },
              style: { color: headingColor }
            },
            {
              props: { variant: 'h3' },
              style: { color: headingColor }
            },
            {
              props: { variant: 'h4' },
              style: { color: headingColor }
            },
            {
              props: { variant: 'h5' },
              style: { color: headingColor }
            },
            {
              props: { variant: 'h6' },
              style: { color: headingColor }
            },
            {
              props: { variant: 'subtitle1' },
              style: { color: vars.palette.text.dark ?? vars.palette.text.primary }
            },
            {
              props: { variant: 'subtitle2' },
              style: { color: vars.palette.text.secondary }
            },
            {
              props: { variant: 'caption' },
              style: { color: vars.palette.text.secondary }
            },
            {
              props: { variant: 'body2' },
              style: { color: vars.palette.text.primary }
            }
          ]
        }
      }
    }
  };
}
