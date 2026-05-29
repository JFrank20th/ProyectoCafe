'use client';

import { styled } from '@mui/material/styles';

export const AuthWrapper1 = styled('div')(({ theme }) => ({
  backgroundColor: theme.vars?.palette.grey[100] ?? theme.palette.grey[100],
  minHeight: '100vh',
}));
