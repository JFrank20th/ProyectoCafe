'use client';

import type { ReactNode } from 'react';

import Box from '@mui/material/Box';
import Card from '@mui/material/Card';

export function AuthCardWrapper({ children }: { children: ReactNode }) {
  return (
    <Card
      sx={{
        maxWidth: { xs: 400, lg: 475 },
        margin: { xs: 2.5, md: 3 },
        border: 0,
        boxShadow: (theme) => theme.shadows[8],
        '& > *': {
          flexGrow: 1,
          flexBasis: '50%',
        },
      }}
    >
      <Box sx={{ p: { xs: 2, sm: 3, xl: 5 } }}>{children}</Box>
    </Card>
  );
}
