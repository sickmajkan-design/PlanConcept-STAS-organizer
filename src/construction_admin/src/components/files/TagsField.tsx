import { LabelOutlined } from '@mui/icons-material';
import { Autocomplete, Chip, TextField } from '@mui/material';

import { MAX_ATTACHMENT_TAG_LENGTH, MAX_ATTACHMENT_TAGS } from '../../api/attachments';
import { useT } from '../../i18n/useI18n';

/** Labels as the server keeps them: trimmed, no blanks, one of each regardless of case. */
export function normaliseTags(tags: readonly string[]): string[] {
  const seen = new Set<string>();
  const result: string[] = [];

  for (const raw of tags) {
    const tag = raw.trim().slice(0, MAX_ATTACHMENT_TAG_LENGTH);

    if (tag && !seen.has(tag.toLowerCase())) {
      seen.add(tag.toLowerCase());
      result.push(tag);
    }
  }

  return result;
}

/**
 * Labels for a document: pick one already in use or type a new one and press Enter. Suggestions come
 * from the labels used so far, so the same thing is not spelled two ways.
 */
export function TagsField({
  value,
  onChange,
  suggestions = [],
  size,
}: {
  value: string[];
  onChange: (tags: string[]) => void;
  suggestions?: readonly string[];
  size?: 'small' | 'medium';
}) {
  const t = useT();
  const full = value.length >= MAX_ATTACHMENT_TAGS;

  return (
    <Autocomplete
      multiple
      freeSolo
      size={size}
      options={full ? [] : [...suggestions]}
      value={value}
      filterSelectedOptions
      onChange={(_, next) => onChange(normaliseTags(next as string[]).slice(0, MAX_ATTACHMENT_TAGS))}
      renderValue={(tags, getItemProps) =>
        tags.map((tag, index) => {
          const { key, ...props } = getItemProps({ index });

          return <Chip key={key} size="small" label={tag} {...props} />;
        })
      }
      renderInput={(params) => (
        <TextField
          {...params}
          label={t('attachments.field.tags')}
          placeholder={full ? '' : t('attachments.tags.placeholder')}
          helperText={t('attachments.tags.max', { max: MAX_ATTACHMENT_TAGS })}
          slotProps={{
            ...params.slotProps,
            input: {
              ...params.slotProps.input,
              startAdornment: (
                <>
                  <LabelOutlined fontSize="small" sx={{ color: 'text.secondary', mr: 0.5 }} />
                  {params.slotProps.input.startAdornment}
                </>
              ),
            },
            htmlInput: { ...params.slotProps.htmlInput, maxLength: MAX_ATTACHMENT_TAG_LENGTH },
          }}
        />
      )}
    />
  );
}
