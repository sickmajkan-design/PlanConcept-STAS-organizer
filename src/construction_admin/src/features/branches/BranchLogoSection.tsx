import { DeleteOutlineOutlined, StarOutlined } from '@mui/icons-material';
import { Alert, Box, Button, FormControlLabel, Stack, Switch, Typography } from '@mui/material';
import { useState } from 'react';

import { toApiError } from '../../api/apiError';
import type { Branch } from '../../api/types';
import { FileDropzone } from '../../components/files/FileDropzone';
import { useT } from '../../i18n/useI18n';
import { branchLogoUrl } from './branchLogo';
import { useBranchesQuery, useRemoveBranchLogo, useSetMainBranch, useUploadBranchLogo } from './useBranches';

const LOGO_ACCEPT = '.png,.jpg,.jpeg,.webp,.svg';
const LOGO_MAX_BYTES = 2 * 1024 * 1024;

/** One logo: its picture on the background it is meant for, or the zone to drop one on. */
function LogoSlot({
  branch,
  dark,
  busy,
  onFile,
  onRemove,
  onReject,
}: {
  branch: Branch;
  dark: boolean;
  busy: boolean;
  onFile: (file: File) => void;
  onRemove: () => void;
  onReject: () => void;
}) {
  const t = useT();
  const has = dark ? branch.hasDarkLogo : branch.hasLogo;
  // A dark logo needs a light logo to stand beside, so the dark slot opens only once there is one.
  const locked = dark && !branch.hasLogo;

  return (
    <Box sx={{ flex: '1 1 260px', minWidth: 0 }}>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 0.5 }}>
        {t(dark ? 'branchLogo.dark' : 'branchLogo.light')}
      </Typography>
      <Box
        sx={{
          height: 120,
          mb: 1,
          borderRadius: 2,
          border: '1px solid',
          borderColor: 'divider',
          bgcolor: dark ? '#0f141d' : '#ffffff',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          p: 1,
        }}
      >
        {has ? (
          <Box
            component="img"
            src={branchLogoUrl(branch, dark)}
            alt={t(dark ? 'branchLogo.dark' : 'branchLogo.light')}
            sx={{ maxWidth: '100%', maxHeight: '100%', objectFit: 'contain' }}
          />
        ) : (
          <Typography variant="body2" sx={{ color: dark ? '#9aa6bb' : 'text.secondary', textAlign: 'center' }}>
            {dark && branch.hasLogo ? t('branchLogo.darkFallback') : t('branchLogo.none')}
          </Typography>
        )}
      </Box>
      {has ? (
        <Button size="small" color="error" startIcon={<DeleteOutlineOutlined />} onClick={onRemove} disabled={busy}>
          {t('branchLogo.remove')}
        </Button>
      ) : (
        <FileDropzone
          accept={LOGO_ACCEPT}
          maxBytes={LOGO_MAX_BYTES}
          multiple={false}
          compact
          camera={false}
          disabled={busy || locked}
          helper={locked ? t('branchLogo.darkNeedsLight') : t('branchLogo.rules')}
          onFiles={(files) => onFile(files[0])}
          onReject={onReject}
        />
      )}
      {has && (
        <Box sx={{ mt: 1 }}>
          <FileDropzone
            accept={LOGO_ACCEPT}
            maxBytes={LOGO_MAX_BYTES}
            multiple={false}
            compact
            camera={false}
            disabled={busy}
            helper={t('branchLogo.replaceHint')}
            onFiles={(files) => onFile(files[0])}
            onReject={onReject}
          />
        </Box>
      )}
    </Box>
  );
}

/**
 * The logo of a business unit: the one for a light background, an optional one for the dark theme,
 * and whether this unit is the main one, whose logo stands for "all business units". Changes are
 * saved at once, like the company logo, not with the rest of the form.
 */
export function BranchLogoSection({ branch }: { branch: Branch }) {
  const t = useT();
  const { data: branches } = useBranchesQuery();
  const live = branches?.find((b) => b.id === branch.id) ?? branch;
  const upload = useUploadBranchLogo();
  const remove = useRemoveBranchLogo();
  const setMain = useSetMainBranch();
  const [error, setError] = useState<string | null>(null);
  const busy = upload.isPending || remove.isPending || setMain.isPending;

  const run = async (action: () => Promise<unknown>) => {
    setError(null);

    try {
      await action();
    } catch (e) {
      setError(toApiError(e).message);
    }
  };

  return (
    <Stack spacing={1.5}>
      <Typography variant="body2" color="text.secondary">
        {t('branchLogo.intro')}
      </Typography>
      {error && <Alert severity="error">{error}</Alert>}
      <Stack direction="row" spacing={2} useFlexGap sx={{ flexWrap: 'wrap' }}>
        <LogoSlot
          branch={live}
          dark={false}
          busy={busy}
          onFile={(file) => void run(() => upload.mutateAsync({ id: live.id, file }))}
          onRemove={() => void run(() => remove.mutateAsync({ id: live.id }))}
          onReject={() => setError(t('branchLogo.rejected'))}
        />
        <LogoSlot
          branch={live}
          dark
          busy={busy}
          onFile={(file) => void run(() => upload.mutateAsync({ id: live.id, file, dark: true }))}
          onRemove={() => void run(() => remove.mutateAsync({ id: live.id, dark: true }))}
          onReject={() => setError(t('branchLogo.rejected'))}
        />
      </Stack>
      <FormControlLabel
        control={
          <Switch
            checked={live.isMain}
            disabled={busy || live.isMain || !live.isActive}
            onChange={() => void run(() => setMain.mutateAsync(live.id))}
          />
        }
        label={
          <Stack direction="row" spacing={0.75} sx={{ alignItems: 'center' }}>
            <StarOutlined fontSize="small" />
            <span>{t('branchLogo.main')}</span>
          </Stack>
        }
      />
      <Typography variant="caption" color="text.secondary">
        {live.isMain ? t('branchLogo.mainHintOn') : t('branchLogo.mainHint')}
      </Typography>
    </Stack>
  );
}
