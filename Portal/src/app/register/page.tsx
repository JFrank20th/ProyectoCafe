'use client';

import { useEffect, useState, type ElementType } from 'react';
import Link from 'next/link';
import { useRouter } from 'next/navigation';

import Alert from '@mui/material/Alert';
import Avatar from '@mui/material/Avatar';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import Stack from '@mui/material/Stack';
import TextField from '@mui/material/TextField';
import Typography from '@mui/material/Typography';
import useMediaQuery from '@mui/material/useMediaQuery';
import { useTheme } from '@mui/material/styles';

import { AuthCardWrapper } from '@/components/auth/AuthCardWrapper';
import { AuthWrapper1 } from '@/components/auth/AuthWrapper1';
import { useTranslations } from '@/hooks/useTranslations';
import { ApiError, apiFetch } from '@/lib/api/client';
import { useAuth } from '@/lib/auth/AuthProvider';
import { getRuntimeConfig } from '@/lib/runtime-config';
import { HOME_ROUTE, LOGIN_ROUTE } from '@/lib/routes';

type RegisterResponse = {
  message: string;
};

function isRecord(value: unknown): value is Record<string, unknown> {
  return Boolean(value) && typeof value === 'object' && !Array.isArray(value);
}

function describeApiError(err: unknown, fallback: string) {
  if (err instanceof ApiError) {
    if (err.bodyText) {
      try {
        const data: unknown = JSON.parse(err.bodyText);
        if (isRecord(data)) {
          const maybeError = data.error;
          if (isRecord(maybeError)) {
            const msg = maybeError.message;
            if (typeof msg === 'string' && msg.trim()) return msg;
          }

          const maybeErrors = data.errors;
          if (isRecord(maybeErrors)) {
            for (const value of Object.values(maybeErrors)) {
              if (Array.isArray(value)) {
                const first = value.find((x) => typeof x === 'string' && x.trim());
                if (typeof first === 'string') return first;
              }
            }
          }
        }
      } catch {
        // ignore
      }
    }

    return err.message || fallback;
  }

  if (err instanceof Error) return err.message;
  return fallback;
}

export default function RegisterPage() {
  const router = useRouter();
  const { isAuthenticated } = useAuth();

  const t = useTranslations('auth');
  const tf = useTranslations('fields');

  const theme = useTheme();
  const downMD = useMediaQuery(theme.breakpoints.down('md'));
  const { APP_NAME } = getRuntimeConfig();

  const initials = (APP_NAME || 'ProyectoCafe')
    .trim()
    .split(/\s+/)
    .slice(0, 2)
    .map((x) => x[0]?.toUpperCase() ?? '')
    .join('')
    .slice(0, 2);

  const [email, setEmail] = useState('');
  const [name, setName] = useState('');
  const [password, setPassword] = useState('');

  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [result, setResult] = useState<RegisterResponse | null>(null);

  useEffect(() => {
    if (!isAuthenticated) return;
    router.replace(HOME_ROUTE);
  }, [isAuthenticated, router]);

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setResult(null);
    setLoading(true);

    try {
      const res = await apiFetch<RegisterResponse>('/api/v1/auth/register', {
        method: 'POST',
        body: JSON.stringify({
          email,
          password,
          name: name.trim() ? name.trim() : null,
        }),
      });
      setResult(res);
    } catch (err) {
      setError(describeApiError(err, t('registerError')));
    } finally {
      setLoading(false);
    }
  }

  if (isAuthenticated) {
    return null;
  }

  return (
    <AuthWrapper1>
      <Stack sx={{ justifyContent: 'flex-end', minHeight: '100vh' }}>
        <Stack sx={{ justifyContent: 'center', alignItems: 'center', minHeight: 'calc(100vh - 68px)' }}>
          <Box
            sx={{
              display: 'flex',
              justifyContent: 'center',
              alignSelf: 'stretch',
              m: { xs: 1, sm: 3 },
              mb: 0,
            }}
          >
            <AuthCardWrapper>
              <Stack sx={{ alignItems: 'center', justifyContent: 'center', gap: 2 }}>
                <Box sx={{ mb: 3 }}>
                  <Avatar
                    variant="rounded"
                    sx={{
                      ...theme.typography.commonAvatar,
                      ...theme.typography.largeAvatar,
                      overflow: 'hidden',
                      color: 'secondary.dark',
                      bgcolor: 'secondary.light',
                    }}
                    aria-label={t('title')}
                  >
                    {initials}
                  </Avatar>
                </Box>

                <Stack sx={{ alignItems: 'center', justifyContent: 'center', gap: 1 }}>
                  <Typography variant={downMD ? 'h3' : 'h2'} sx={{ color: 'secondary.main' }}>
                    {t('registerTitle')}
                  </Typography>
                  <Typography
                    variant="caption"
                    sx={{ fontSize: '16px', textAlign: { xs: 'center', md: 'inherit' } }}
                  >
                    {t('registerSubtitle')}
                  </Typography>
                </Stack>

                <Box sx={{ width: 1 }}>
                  <Box component="form" onSubmit={onSubmit} sx={{ mt: 1 }}>
                    <Stack sx={{ gap: 2 }}>
                      <TextField
                        label={t('email')}
                        value={email}
                        onChange={(e) => setEmail(e.target.value)}
                        autoComplete="username"
                        fullWidth
                      />
                      <TextField
                        label={tf('name')}
                        value={name}
                        onChange={(e) => setName(e.target.value)}
                        autoComplete="name"
                        fullWidth
                      />
                      <TextField
                        label={t('password')}
                        type="password"
                        value={password}
                        onChange={(e) => setPassword(e.target.value)}
                        autoComplete="new-password"
                        fullWidth
                      />

                      {error ? <Alert severity="error">{error}</Alert> : null}
                      {result ? <Alert severity="success">{result.message}</Alert> : null}

                      <Button
                        fullWidth
                        size="large"
                        variant="contained"
                        color="primary"
                        type="submit"
                        disabled={loading}
                      >
                        {loading ? t('registering') : t('register')}
                      </Button>

                      <Stack direction="row" justifyContent="space-between" sx={{ pt: 0.5 }}>
                        <Typography
                          component={Link as unknown as ElementType}
                          href={LOGIN_ROUTE}
                          variant="body2"
                          sx={{ textDecoration: 'none', color: 'primary.main' }}
                        >
                          {t('backToLogin')}
                        </Typography>
                      </Stack>
                    </Stack>
                  </Box>
                </Box>
              </Stack>
            </AuthCardWrapper>
          </Box>
        </Stack>
      </Stack>
    </AuthWrapper1>
  );
}
