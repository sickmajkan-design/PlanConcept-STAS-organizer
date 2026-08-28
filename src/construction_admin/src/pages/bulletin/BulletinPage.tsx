import {
  AddOutlined,
  CampaignOutlined,
  DeleteOutlined,
  VisibilityOutlined,
} from '@mui/icons-material';
import {
  Alert,
  Avatar,
  Box,
  Button,
  Card,
  CardContent,
  Chip,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Grid,
  IconButton,
  Stack,
  TextField,
  Tooltip,
  Typography,
} from '@mui/material';
import { useEffect, useState } from 'react';

import { toApiError } from '../../api/apiError';
import type { BulletinPost } from '../../api/types';
import { canAdministerAccounts } from '../../auth/authHelpers';
import { useAuth } from '../../auth/useAuth';
import { ConfirmDialog } from '../../components/ConfirmDialog';
import { PageHeader } from '../../components/PageHeader';
import {
  useBulletinPostsQuery,
  useBulletinViewersQuery,
  useCreateBulletinPost,
  useDeleteBulletinPost,
  useMarkBulletinViewed,
} from '../../features/bulletin/useBulletin';
import { useDeleteWithConfirm } from '../../hooks/useDeleteWithConfirm';
import { useT } from '../../i18n/useI18n';
import { formatDateTime, initialsOf } from '../../utils/formatting';

/**
 * The company bulletin board: notices posted by Admin+ that stay up for
 * everyone — including people hired after they went up — until removed.
 *
 * Cards, not a table: this is a wall of notices, not a report to sort and
 * filter. Opening the page is itself the "I saw this" signal — each unread
 * card is marked viewed the moment its data lands, no extra tap required.
 */
export function BulletinPage() {
  const t = useT();
  const { user } = useAuth();
  const canManage = canAdministerAccounts(user);

  const { data, isLoading } = useBulletinPostsQuery();
  const markViewed = useMarkBulletinViewed();
  const remove = useDeleteWithConfirm<BulletinPost>(useDeleteBulletinPost());
  const [composing, setComposing] = useState(false);
  const [viewingPostId, setViewingPostId] = useState<string | null>(null);

  const posts = data ?? [];

  // Fires once per unseen post as the list loads — this is the whole
  // "passive" acknowledgment: opening the board is what counts as seeing it.
  useEffect(() => {
    for (const post of posts) {
      if (!post.viewed) {
        markViewed.mutate(post.id);
      }
    }
    // Only when the post list itself changes — markViewed is a stable mutation.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [posts]);

  return (
    <Box>
      <PageHeader
        title={t('bulletin.title')}
        description={t('bulletin.description')}
        action={
          canManage
            ? {
                label: t('bulletin.new'),
                icon: <AddOutlined />,
                onClick: () => setComposing(true),
              }
            : undefined
        }
      />

      {isLoading ? (
        <Stack sx={{ alignItems: 'center', py: 6 }}>
          <CircularProgress size={28} />
        </Stack>
      ) : posts.length === 0 ? (
        <Card variant="outlined">
          <CardContent>
            <Stack spacing={1.5} sx={{ alignItems: 'center', py: 4 }}>
              <CampaignOutlined sx={{ fontSize: 40, color: 'text.disabled' }} />
              <Typography color="text.secondary">{t('bulletin.empty')}</Typography>
            </Stack>
          </CardContent>
        </Card>
      ) : (
        <Grid container spacing={2}>
          {posts.map((post) => (
            <Grid key={post.id} size={{ xs: 12, md: 6 }}>
              <Card
                variant="outlined"
                sx={{
                  height: '100%',
                  borderLeft: '4px solid',
                  borderLeftColor: 'primary.main',
                }}
              >
                <CardContent>
                  <Stack spacing={1.5}>
                    <Stack
                      direction="row"
                      spacing={1}
                      sx={{ alignItems: 'flex-start', justifyContent: 'space-between' }}
                    >
                      <Typography variant="h6" sx={{ fontWeight: 700 }}>
                        {post.title}
                      </Typography>
                      {canManage && (
                        <Tooltip title={t('common.delete')}>
                          <IconButton size="small" onClick={() => remove.request(post)}>
                            <DeleteOutlined fontSize="small" />
                          </IconButton>
                        </Tooltip>
                      )}
                    </Stack>

                    <Typography variant="body2" sx={{ whiteSpace: 'pre-wrap' }}>
                      {post.body}
                    </Typography>

                    <Stack
                      direction="row"
                      spacing={1.5}
                      sx={{ alignItems: 'center', pt: 0.5 }}
                    >
                      <Avatar sx={{ width: 28, height: 28, fontSize: '0.75rem' }}>
                        {initialsOf(null, null, post.createdByName)}
                      </Avatar>
                      <Stack sx={{ flex: 1, minWidth: 0 }}>
                        <Typography variant="caption" sx={{ fontWeight: 600 }} noWrap>
                          {post.createdByName}
                        </Typography>
                        <Typography variant="caption" color="text.secondary">
                          {formatDateTime(post.createdAt)}
                        </Typography>
                      </Stack>

                      {canManage ? (
                        <Chip
                          size="small"
                          variant="outlined"
                          icon={<VisibilityOutlined fontSize="small" />}
                          label={t('bulletin.viewCount', { count: post.viewCount })}
                          onClick={() => setViewingPostId(post.id)}
                          clickable
                        />
                      ) : (
                        <Chip
                          size="small"
                          variant="outlined"
                          icon={<VisibilityOutlined fontSize="small" />}
                          label={post.viewCount}
                        />
                      )}
                    </Stack>
                  </Stack>
                </CardContent>
              </Card>
            </Grid>
          ))}
        </Grid>
      )}

      <ComposeBulletinDialog open={composing} onClose={() => setComposing(false)} />

      <BulletinViewersDialog
        postId={viewingPostId}
        onClose={() => setViewingPostId(null)}
      />

      <ConfirmDialog
        open={!!remove.pending}
        title={t('bulletin.deleteTitle')}
        description={
          remove.pending ? t('bulletin.deleteBody', { title: remove.pending.title }) : ''
        }
        confirmLabel={t('common.delete')}
        destructive
        loading={remove.isDeleting}
        onConfirm={remove.confirm}
        onCancel={remove.cancel}
      />

      {remove.error && (
        <Box sx={{ mt: 1 }}>
          <Typography variant="body2" color="error">
            {remove.error.message}
          </Typography>
        </Box>
      )}
    </Box>
  );
}

function ComposeBulletinDialog({
  open,
  onClose,
}: {
  open: boolean;
  onClose: () => void;
}) {
  const t = useT();
  const create = useCreateBulletinPost();

  const [title, setTitle] = useState('');
  const [body, setBody] = useState('');

  const reset = create.reset;

  useEffect(() => {
    if (open) {
      setTitle('');
      setBody('');
      reset();
    }
  }, [open, reset]);

  const canSubmit = title.trim() !== '' && body.trim() !== '';
  const error = create.isError ? toApiError(create.error) : null;

  return (
    <Dialog open={open} onClose={onClose} fullWidth maxWidth="sm">
      <DialogTitle>{t('bulletin.new')}</DialogTitle>
      <DialogContent>
        {error && (
          <Alert severity="error" sx={{ mb: 2 }}>
            {error.message}
          </Alert>
        )}

        <Stack spacing={2.5} sx={{ mt: 1 }}>
          <TextField
            fullWidth
            label={t('bulletin.postTitle')}
            value={title}
            onChange={(event) => setTitle(event.target.value)}
          />
          <TextField
            fullWidth
            multiline
            minRows={4}
            label={t('bulletin.postBody')}
            value={body}
            onChange={(event) => setBody(event.target.value)}
          />
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>{t('common.cancel')}</Button>
        <Button
          variant="contained"
          disabled={!canSubmit || create.isPending}
          onClick={() =>
            create.mutate(
              { title: title.trim(), body: body.trim() },
              { onSuccess: onClose },
            )
          }
        >
          {t('bulletin.publish')}
        </Button>
      </DialogActions>
    </Dialog>
  );
}

/** The "who has seen this" roster — same shape as AuditHistoryCard's timeline. */
function BulletinViewersDialog({
  postId,
  onClose,
}: {
  postId: string | null;
  onClose: () => void;
}) {
  const t = useT();
  const { data, isLoading } = useBulletinViewersQuery(postId);

  const viewers = data ?? [];

  return (
    <Dialog open={!!postId} onClose={onClose} fullWidth maxWidth="xs">
      <DialogTitle>{t('bulletin.viewers')}</DialogTitle>
      <DialogContent>
        {isLoading ? (
          <Stack sx={{ alignItems: 'center', py: 3 }}>
            <CircularProgress size={24} />
          </Stack>
        ) : viewers.length === 0 ? (
          <Typography color="text.secondary">{t('bulletin.viewersEmpty')}</Typography>
        ) : (
          <Stack spacing={1.25}>
            {viewers.map((viewer) => (
              <Stack
                key={viewer.userId}
                direction="row"
                spacing={1}
                sx={{ alignItems: 'center', justifyContent: 'space-between' }}
              >
                <Typography variant="body2">{viewer.userEmail}</Typography>
                <Typography variant="caption" color="text.secondary">
                  {formatDateTime(viewer.viewedAt)}
                </Typography>
              </Stack>
            ))}
          </Stack>
        )}
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>{t('common.close')}</Button>
      </DialogActions>
    </Dialog>
  );
}
