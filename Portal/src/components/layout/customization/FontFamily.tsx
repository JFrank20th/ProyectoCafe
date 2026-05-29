'use client';

import Card from '@mui/material/Card';
import FormControlLabel from '@mui/material/FormControlLabel';
import Radio from '@mui/material/Radio';
import RadioGroup from '@mui/material/RadioGroup';
import Stack from '@mui/material/Stack';
import Typography from '@mui/material/Typography';

import { UI_FONT_FAMILIES } from '@/lib/ui-config/defaults';
import { useUiConfig } from '@/hooks/useUiConfig';
import { useTranslations } from '@/hooks/useTranslations';

export function FontFamily() {
  const t = useTranslations('customization');
  const {
    state: { fontFamily },
    setField,
  } = useUiConfig();

  return (
    <Stack sx={{ p: 2, gap: 2.5 }}>
      <Typography variant="h5">{t('fontStyle')}</Typography>
      <RadioGroup
        aria-label={t('fontStyle')}
        name="font-family"
        value={fontFamily}
        onChange={(e) => setField('fontFamily', e.target.value)}
      >
        <Stack sx={{ gap: 1.25 }}>
          {UI_FONT_FAMILIES.map((item) => {
            const selected = fontFamily === item.value;

            return (
              <Card
                key={item.id}
                variant="outlined"
                sx={{
                  p: 0.75,
                  bgcolor: selected ? 'primary.light' : 'grey.50',
                  borderColor: 'transparent',
                }}
              >
                <Card
                  variant="outlined"
                  sx={{
                    p: 1.75,
                    borderWidth: 1,
                    ...(selected && { borderColor: 'primary.main' }),
                  }}
                >
                  <FormControlLabel
                    sx={{ width: 1, m: 0 }}
                    control={<Radio value={item.value} sx={{ display: 'none' }} />}
                    label={
                      <Typography variant="h5" sx={{ pl: 2, fontFamily: item.value }}>
                        {item.label}
                      </Typography>
                    }
                  />
                </Card>
              </Card>
            );
          })}
        </Stack>
      </RadioGroup>
    </Stack>
  );
}
