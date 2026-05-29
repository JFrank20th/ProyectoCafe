import { styled } from '@mui/material/styles';

import { berryDrawerWidth } from '@/theme/berry/constants';

// ==============================|| MAIN LAYOUT - STYLED ||============================== //

const MainContentStyled = styled('main', {
  shouldForwardProp: (prop) => prop !== 'open' && prop !== 'borderRadius',
})<{
  open: boolean;
  borderRadius: number;
}>(({ theme, open, borderRadius }) => ({
  backgroundColor: theme.palette.background.default,
  minWidth: '1%',
  width: '100%',
  minHeight: 'calc(100vh - 88px)',
  flexGrow: 1,
  padding: 20,
  marginTop: 88,
  marginRight: 20,
  borderRadius: `${borderRadius}px`,
  borderBottomLeftRadius: 0,
  borderBottomRightRadius: 0,
  ...(!open && {
    transition: theme.transitions.create('margin', {
      easing: theme.transitions.easing.sharp,
      duration: theme.transitions.duration.shorter + 200,
    }),
    [theme.breakpoints.up('md')]: {
      marginLeft: -(berryDrawerWidth - 72),
      width: `calc(100% - ${berryDrawerWidth}px)`,
      marginTop: 88,
    },
  }),
  ...(open && {
    transition: theme.transitions.create('margin', {
      easing: theme.transitions.easing.easeOut,
      duration: theme.transitions.duration.shorter + 200,
    }),
    marginLeft: 0,
    marginTop: 88,
    width: `calc(100% - ${berryDrawerWidth}px)`,
    [theme.breakpoints.up('md')]: {
      marginTop: 88,
    },
  }),
  [theme.breakpoints.down('md')]: {
    marginLeft: 20,
    padding: 16,
    marginTop: 88,
    ...(!open && {
      width: `calc(100% - ${berryDrawerWidth}px)`,
    }),
  },
  [theme.breakpoints.down('sm')]: {
    marginLeft: 10,
    marginRight: 10,
  },
}));

export default MainContentStyled;
