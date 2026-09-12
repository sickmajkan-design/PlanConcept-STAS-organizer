import { EngineeringOutlined } from '@mui/icons-material';
import { Box, Paper, Stack, Typography } from '@mui/material';

import { config } from '../config';
import { useCompanyBrandingQuery } from '../features/companySettings/useCompanySettings';
import { LanguageSwitcher } from './LanguageSwitcher';
import { OfflineBanner } from './OfflineBanner';
import type { ReactNode } from 'react';

/**
 * Centered card frame shared by every anonymous auth screen. Shows the
 * platform's own company logo/name when one is configured, falling back to
 * the generic app identity — this is the one screen that must work with no
 * auth token at all, which `useCompanyBrandingQuery` is built for.
 */
export function AuthCard({
  title,
  subtitle,
  children,
}: {
  title: string;
  subtitle?: string;
  children: ReactNode;
}) {
  const { data: branding } = useCompanyBrandingQuery();

  return (
    <Box
      sx={{
        minHeight: '100vh',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        bgcolor: 'background.default',
        px: 2,
      }}
    >
      <Paper
        elevation={0}
        sx={{ p: { xs: 3, sm: 5 }, width: '100%', maxWidth: 440, border: '1px solid #e0e0e0' }}
      >
        <Stack spacing={3}>
          {/* Sign-in needs it as much as anything behind it: without this, a
              failed login on a dead connection reads as a wrong password. */}
          <OfflineBanner />
          <Stack spacing={1} sx={{ alignItems: 'center', textAlign: 'center' }}>
            {branding?.hasLogo ? (
              <Box
                component="img"
                src={`${config.apiBaseUrl}/api/v1/company-settings/logo`}
                alt=""
                sx={{ width: 56, height: 56, objectFit: 'contain' }}
              />
            ) : (
              <EngineeringOutlined sx={{ fontSize: 44, color: 'primary.main' }} />
            )}
            <Box sx={{ display: 'flex', justifyContent: 'flex-end' }}>
              {/* Someone who cannot read this screen cannot get past it to
                  change the language anywhere else. */}
              <LanguageSwitcher />
            </Box>
            <Typography variant="h5" sx={{ fontWeight: 700 }}>
              {title}
            </Typography>
            {subtitle && (
              <Typography variant="body2" color="text.secondary">
                {subtitle}
              </Typography>
            )}
          </Stack>
          {children}
        </Stack>
      </Paper>
    </Box>
  );
}
