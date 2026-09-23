import { FolderZipOutlined } from '@mui/icons-material';
import { Button, Typography } from '@mui/material';
import { useState } from 'react';

import { attachmentsApi } from '../api/attachments';
import { useT } from '../i18n/useI18n';

/**
 * Downloads the given documents as one ZIP, filed by owner and category.
 *
 * The archive is built by the API, which checks every document against the
 * caller's own read rights — so this can be pointed at any list without
 * widening what anyone can get.
 */
export function ExportZipButton({ ids }: { ids: readonly string[] }) {
  const t = useT();
  const [busy, setBusy] = useState(false);
  const [failed, setFailed] = useState(false);

  const download = async () => {
    setBusy(true);
    setFailed(false);

    let url: string | undefined;

    try {
      const { blob, fileName } = await attachmentsApi.exportZip(ids);

      url = URL.createObjectURL(blob);

      const anchor = document.createElement('a');
      anchor.href = url;
      anchor.download = fileName;
      anchor.click();
    } catch {
      setFailed(true);
    } finally {
      if (url) {
        URL.revokeObjectURL(url);
      }

      setBusy(false);
    }
  };

  return (
    <>
      <Button
        size="small"
        startIcon={<FolderZipOutlined />}
        disabled={busy || ids.length === 0}
        onClick={() => void download()}
      >
        {t('attachments.exportZip')}
      </Button>
      {failed && (
        <Typography variant="caption" color="error">
          {t('attachments.exportFailed')}
        </Typography>
      )}
    </>
  );
}
