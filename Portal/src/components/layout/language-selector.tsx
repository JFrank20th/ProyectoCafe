'use client';

import { useEffect, useMemo, useState } from 'react';

import IconButton from '@mui/material/IconButton';
import Menu from '@mui/material/Menu';
import MenuItem from '@mui/material/MenuItem';
import Tooltip from '@mui/material/Tooltip';

import { IconWorld } from '@tabler/icons-react';

import { useLocale, useTranslations } from '@/hooks/useTranslations';
import { localeNames, type Locale } from '@/i18n/config';
import { getCurrentLocale, setLocale } from '@/lib/locale-utils';

export type LanguageSelectorProps = {
  className?: string;
};

export function LanguageSelector({ className }: LanguageSelectorProps) {
  const t = useTranslations('common');
  const contextLocale = useLocale();
  const [currentLocale, setCurrentLocale] = useState<Locale>(contextLocale);
  const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null);
  const open = Boolean(anchorEl);

  useEffect(() => {
    setCurrentLocale(getCurrentLocale());
  }, []);

  const languages = useMemo(
    () =>
      Object.entries(localeNames).map(([code, name]) => ({
        code: code as Locale,
        name,
      })),
    []
  );

  function handleChange(locale: Locale) {
    if (locale !== currentLocale) setLocale(locale);
    setAnchorEl(null);
  }

  return (
    <>
      <Tooltip title={t('changeLanguage')}>
        <IconButton
          className={className}
          onClick={(e) => setAnchorEl(e.currentTarget)}
          size="small"
          aria-label={t('changeLanguage')}
          aria-controls={open ? 'language-menu' : undefined}
          aria-haspopup="true"
          aria-expanded={open ? 'true' : undefined}
        >
          <IconWorld stroke={1.5} size={20} />
        </IconButton>
      </Tooltip>

      <Menu
        id="language-menu"
        anchorEl={anchorEl}
        open={open}
        onClose={() => setAnchorEl(null)}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'right' }}
        transformOrigin={{ vertical: 'top', horizontal: 'right' }}
      >
        {languages.map((lang) => (
          <MenuItem key={lang.code} selected={currentLocale === lang.code} onClick={() => handleChange(lang.code)}>
            {lang.name}
          </MenuItem>
        ))}
      </Menu>
    </>
  );
}
