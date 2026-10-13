import type { AttachmentCategory, AttachmentOwnerType } from '../api/types';
import { DocumentUploadDialog } from './files/DocumentUploadDialog';

/**
 * Adds documents to one record. The record and the types it offers are fixed by the page that opens it;
 * everything else is the shared upload dialog. Files dropped on the page's document list arrive as
 * `initialFiles`.
 */
export function UploadAttachmentDialog({
  open,
  ownerType,
  ownerId,
  categories,
  onClose,
  initialFiles,
}: {
  open: boolean;
  ownerType: AttachmentOwnerType;
  ownerId: string;
  categories: readonly AttachmentCategory[];
  onClose: () => void;
  initialFiles?: readonly File[];
}) {
  return (
    <DocumentUploadDialog
      open={open}
      onClose={onClose}
      ownerType={ownerType}
      ownerId={ownerId}
      categories={categories}
      initialFiles={initialFiles}
    />
  );
}
