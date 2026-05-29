'use client';

import Box from '@mui/material/Box';
import Slider from '@mui/material/Slider';
import Stack from '@mui/material/Stack';
import Typography from '@mui/material/Typography';

import { useUiConfig } from '@/hooks/useUiConfig';
import { useTranslations } from '@/hooks/useTranslations';

function valueText(value: number) {
  return `${value}px`;
}

export function BorderRadius() {
  const t = useTranslations('customization');
  const {
    state: { borderRadius },
    setField,
  } = useUiConfig();

  return (
    <Stack sx={{ pl: 2, pb: 2, pr: 4, gap: 2.5 }}>
      <Typography variant="h5">{t('borderRadius')}</Typography>

      <Stack direction="row" sx={{ pt: 2, alignItems: 'center', gap: 1.25 }}>
        <Typography variant="h6">4px</Typography>
        <Box sx={{ flex: 1 }}>
          <Slider
            size="small"
            value={borderRadius}
            onChange={(_event, newValue) => {
              const next = Array.isArray(newValue) ? newValue[0] : newValue;
              setField('borderRadius', next);
            }}
            getAriaValueText={valueText}
            valueLabelDisplay="on"
            aria-label={t('borderRadius')}
            min={4}
            max={24}
            color="primary"
            sx={{ '& .MuiSlider-valueLabel': { color: 'primary.light' } }}
          />
        </Box>
        <Typography variant="h6">24px</Typography>
      </Stack>
    </Stack>
  );
}
