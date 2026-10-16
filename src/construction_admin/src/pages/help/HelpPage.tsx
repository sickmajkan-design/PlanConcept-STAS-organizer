import {
  ArrowBackOutlined,
  ArrowForwardOutlined,
  LightbulbOutlined,
  SearchOutlined,
} from '@mui/icons-material';
import {
  Alert,
  Box,
  Button,
  InputAdornment,
  Paper,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import { useMemo, useState } from 'react';
import { Link as RouterLink, useParams } from 'react-router-dom';

import { PageHeader } from '../../components/PageHeader';
import { useAuth } from '../../auth/useAuth';
import {
  helpGroups,
  helpLocale,
  helpMatches,
  helpTopicById,
  helpTopicsFor,
  type HelpTopic,
} from '../../features/help/helpTopics';
import { useI18n } from '../../i18n/useI18n';
import { paths } from '../../routes/paths';

/**
 * The detailed guides: what each part of the platform is for, the steps in order, a worked example and the
 * things that are easy to miss. The same topics are found through Ctrl+K, so someone who does not know where
 * to look can type a word and land here.
 */
export function HelpPage() {
  const { t, locale } = useI18n();
  const { user } = useAuth();
  const { topicId } = useParams();
  const lang = helpLocale(locale);
  const [query, setQuery] = useState('');

  const topics = useMemo(() => helpTopicsFor(user), [user]);
  const topic = helpTopicById(topicId);

  if (topicId) {
    if (!topic || !topics.includes(topic)) {
      return (
        <Stack spacing={2}>
          <PageHeader title={t('help.title')} />
          <Alert severity="info">{t('help.notFound')}</Alert>
          <Box>
            <Button component={RouterLink} to={paths.help} startIcon={<ArrowBackOutlined />}>
              {t('help.all')}
            </Button>
          </Box>
        </Stack>
      );
    }

    return <TopicView topic={topic} />;
  }

  const visible = topics.filter((item) => helpMatches(item.content[lang], query));

  return (
    <Stack spacing={3}>
      <PageHeader title={t('help.title')} description={t('help.description')} />

      <TextField
        value={query}
        onChange={(event) => setQuery(event.target.value)}
        placeholder={t('help.search')}
        fullWidth
        slotProps={{
          input: {
            startAdornment: (
              <InputAdornment position="start">
                <SearchOutlined color="action" />
              </InputAdornment>
            ),
          },
          htmlInput: { 'aria-label': t('help.search') },
        }}
      />

      {visible.length === 0 && <Alert severity="info">{t('help.noResults')}</Alert>}

      {helpGroups.map((group) => {
        const inGroup = visible.filter((item) => item.group === group.id);

        if (inGroup.length === 0) return null;

        return (
          <Stack key={group.id} spacing={1.5}>
            <Typography variant="subtitle1" sx={{ fontWeight: 700 }}>
              {group[lang]}
            </Typography>
            <Box
              sx={{
                display: 'grid',
                gap: 1.5,
                gridTemplateColumns: { xs: '1fr', md: 'repeat(2, 1fr)' },
              }}
            >
              {inGroup.map((item) => (
                <Paper
                  key={item.id}
                  variant="outlined"
                  component={RouterLink}
                  to={paths.helpTopic(item.id)}
                  sx={{
                    p: 2,
                    textDecoration: 'none',
                    color: 'inherit',
                    '&:hover': { borderColor: 'primary.main' },
                    '&:focus-visible': { outline: '2px solid', outlineColor: 'primary.main' },
                  }}
                >
                  <Typography variant="subtitle2" sx={{ fontWeight: 700 }}>
                    {item.content[lang].title}
                  </Typography>
                  <Typography variant="body2" color="text.secondary" sx={{ mt: 0.5 }}>
                    {item.content[lang].summary}
                  </Typography>
                </Paper>
              ))}
            </Box>
          </Stack>
        );
      })}
    </Stack>
  );
}

function TopicView({ topic }: { topic: HelpTopic }) {
  const { t, locale } = useI18n();
  const content = topic.content[helpLocale(locale)];

  return (
    <Stack spacing={3} sx={{ maxWidth: 820 }}>
      <Box>
        <Button component={RouterLink} to={paths.help} startIcon={<ArrowBackOutlined />} size="small">
          {t('help.all')}
        </Button>
      </Box>

      <PageHeader title={content.title} description={content.summary} />

      <Paper variant="outlined" sx={{ p: 2.5 }}>
        <Typography variant="subtitle1" sx={{ fontWeight: 700, mb: 1.5 }}>
          {t('help.steps')}
        </Typography>
        <Stack component="ol" spacing={1.25} sx={{ m: 0, pl: 3 }}>
          {content.steps.map((step) => (
            <Typography key={step} component="li" variant="body2">
              {step}
            </Typography>
          ))}
        </Stack>
      </Paper>

      {content.example && (
        <Paper variant="outlined" sx={{ p: 2.5, bgcolor: 'action.hover' }}>
          <Typography variant="subtitle1" sx={{ fontWeight: 700, mb: 1 }}>
            {t('help.example')}
          </Typography>
          <Typography variant="body2">{content.example}</Typography>
        </Paper>
      )}

      {content.tips.length > 0 && (
        <Paper variant="outlined" sx={{ p: 2.5 }}>
          <Stack direction="row" spacing={1} sx={{ alignItems: 'center', mb: 1.5 }}>
            <LightbulbOutlined fontSize="small" color="warning" />
            <Typography variant="subtitle1" sx={{ fontWeight: 700 }}>
              {t('help.tips')}
            </Typography>
          </Stack>
          <Stack component="ul" spacing={1} sx={{ m: 0, pl: 3 }}>
            {content.tips.map((tip) => (
              <Typography key={tip} component="li" variant="body2">
                {tip}
              </Typography>
            ))}
          </Stack>
        </Paper>
      )}

      <Stack direction="row" spacing={1} sx={{ flexWrap: 'wrap', alignItems: 'center' }} useFlexGap>
        <Button
          component={RouterLink}
          to={topic.path}
          variant="contained"
          endIcon={<ArrowForwardOutlined />}
        >
          {t('help.open')}
        </Button>
      </Stack>
    </Stack>
  );
}
