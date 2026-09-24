import { Button, Dialog, DialogActions, DialogContent, DialogTitle, MenuItem, Stack, TextField } from '@mui/material';
import { useEffect, useState, type ReactNode } from 'react';

import { useT } from '../../../i18n/useI18n';
import { useAllProjectsQuery } from '../../projects/useProjects';

/** The frame every widget's settings share: a title, the fields, and Save / Cancel. */
export function WidgetSettingsDialog({
  open,
  title,
  onClose,
  onSave,
  saveDisabled = false,
  children,
}: {
  open: boolean;
  title: string;
  onClose: () => void;
  onSave: () => void;
  saveDisabled?: boolean;
  children: ReactNode;
}) {
  const t = useT();

  return (
    <Dialog open={open} onClose={onClose} fullWidth maxWidth="xs">
      <DialogTitle>{title}</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ pt: 1 }}>
          {children}
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>{t('common.cancel')}</Button>
        <Button variant="contained" disabled={saveDisabled} onClick={onSave}>
          {t('common.save')}
        </Button>
      </DialogActions>
    </Dialog>
  );
}

/**
 * Picks the project a widget shows. `allowNone` offers "whole company" for a
 * widget that can also show everything; without it a project has to be chosen.
 */
export function ProjectSettingsDialog({
  open,
  title,
  projectId,
  allowNone,
  onClose,
  onSave,
}: {
  open: boolean;
  title: string;
  projectId: string;
  allowNone: boolean;
  onClose: () => void;
  onSave: (projectId: string) => void;
}) {
  const t = useT();
  const projects = useAllProjectsQuery();
  const [draft, setDraft] = useState(projectId);

  // Reopening starts from what is saved, not from an abandoned edit.
  useEffect(() => {
    if (open) setDraft(projectId);
  }, [open, projectId]);

  return (
    <WidgetSettingsDialog
      open={open}
      title={title}
      onClose={onClose}
      onSave={() => onSave(draft)}
      saveDisabled={!allowNone && draft === ''}
    >
      <TextField
        select
        fullWidth
        label={t('finance.project')}
        value={draft}
        onChange={(event) => setDraft(event.target.value)}
      >
        {allowNone && (
          <MenuItem value="">
            <em>{t('finance.wholeCompany')}</em>
          </MenuItem>
        )}
        {(projects.data?.items ?? []).map((project) => (
          <MenuItem key={project.id} value={project.id}>
            {project.name}
          </MenuItem>
        ))}
      </TextField>
    </WidgetSettingsDialog>
  );
}
