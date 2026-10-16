import { MAX_ATTACHMENT_BYTES } from '../../api/attachments';
import type { Rejection } from '../../features/files/validateFiles';
import type { useT } from '../../i18n/useI18n';

/** One line saying why a file was turned away, in the person's language. */
export function rejectionText(
  t: ReturnType<typeof useT>,
  { file, reason }: Rejection,
  maxBytes: number = MAX_ATTACHMENT_BYTES,
): string {
  const name = file.name || '—';

  switch (reason) {
    case 'size':
      return t('files.reject.size', { name, limit: Math.round(maxBytes / (1024 * 1024)) });
    case 'type':
      return t('files.reject.type', { name });
    case 'duplicate':
      return t('files.reject.duplicate', { name });
    case 'empty':
      return t('files.reject.empty', { name });
    case 'count':
      return t('files.reject.count', { name });
  }
}
