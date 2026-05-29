'use client';

import { useMemo, type ElementType } from 'react';
import Link from 'next/link';
import { usePathname } from 'next/navigation';

import Box from '@mui/material/Box';
import Card from '@mui/material/Card';
import Typography from '@mui/material/Typography';
import MuiBreadcrumbs from '@mui/material/Breadcrumbs';

import { IconChevronRight, IconHome2 } from '@tabler/icons-react';

import { NAV } from '@/lib/navigation';
import { useTranslations } from '@/hooks/useTranslations';

type BreadcrumbMatch = {
  sectionLabelKey?: string;
  itemLabelKey: string;
};

export function AppBreadcrumbs() {
  const pathname = usePathname();
  const t = useTranslations('navigation');

  const match = useMemo<BreadcrumbMatch | null>(() => {
    for (const section of NAV) {
      const item = section.items.find((x) => x.href === pathname);
      if (item) {
        return {
          sectionLabelKey: section.labelKey,
          itemLabelKey: item.labelKey,
        };
      }
    }

    return null;
  }, [pathname]);

  if (!match) return null;

  const pageTitle = t(match.itemLabelKey);

  return (
    <Card sx={{ mb: 3, bgcolor: 'background.paper' }}>
      <Box sx={{ p: 1.25, px: 2 }}>
        <Box sx={{ display: 'flex', flexWrap: 'wrap', alignItems: 'center', justifyContent: 'space-between', gap: 1 }}>
          <Typography variant="h4" sx={{ fontWeight: 500 }}>
            {pageTitle}
          </Typography>

          <MuiBreadcrumbs
            aria-label="breadcrumb"
            maxItems={8}
            separator={<IconChevronRight stroke={1.5} size="16px" />}
            sx={{ '& .MuiBreadcrumbs-separator': { width: 16, mx: 0.75 } }}
          >
            <Typography
              component={Link as unknown as ElementType}
              href="/dashboard"
              variant="h6"
              sx={{
                display: 'flex',
                alignContent: 'center',
                alignItems: 'center',
                textDecoration: 'none',
                color: 'text.secondary',
              }}
            >
              <Box component="span" sx={{ mr: 0.75, display: 'inline-flex' }}>
                <IconHome2 stroke={1.5} size={16} />
              </Box>
              {t('dashboard')}
            </Typography>

            {match.sectionLabelKey && match.sectionLabelKey !== 'main' ? (
              <Typography variant="h6" sx={{ color: 'text.secondary' }}>
                {t(match.sectionLabelKey)}
              </Typography>
            ) : null}

            <Typography variant="h6" sx={{ color: 'text.secondary' }}>
              {pageTitle}
            </Typography>
          </MuiBreadcrumbs>
        </Box>
      </Box>
    </Card>
  );
}
