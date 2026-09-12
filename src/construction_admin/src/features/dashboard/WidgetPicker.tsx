import {
  Dialog,
  DialogContent,
  DialogTitle,
  List,
  ListItemButton,
  ListItemText,
  Typography,
} from '@mui/material';

import { useT } from '../../i18n/useI18n';
import { dashboardWidgetTypes, type DashboardWidgetType } from './widgetTypes';
import { widgetRegistry } from './widgetRegistry';

interface WidgetPickerProps {
  open: boolean;
  excludeTypes: Set<DashboardWidgetType>;
  onClose: () => void;
  onPick: (type: DashboardWidgetType) => void;
}

/** JIRA's "Add gadget" dialog — the catalog entries not already on the board. */
export function WidgetPicker({ open, excludeTypes, onClose, onPick }: WidgetPickerProps) {
  const t = useT();
  const available = dashboardWidgetTypes.filter((type) => !excludeTypes.has(type));

  return (
    <Dialog open={open} onClose={onClose} fullWidth maxWidth="xs">
      <DialogTitle>{t('dashboard.pickerTitle')}</DialogTitle>
      <DialogContent>
        {available.length === 0 ? (
          <Typography color="text.secondary" sx={{ py: 2 }}>
            {t('dashboard.pickerEmpty')}
          </Typography>
        ) : (
          <List>
            {available.map((type) => (
              <ListItemButton key={type} onClick={() => onPick(type)}>
                <ListItemText primary={t(widgetRegistry[type].titleKey)} />
              </ListItemButton>
            ))}
          </List>
        )}
      </DialogContent>
    </Dialog>
  );
}
