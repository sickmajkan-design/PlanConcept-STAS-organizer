import { ArrowDropDownOutlined } from '@mui/icons-material';
import { Button, ListItemIcon, ListItemText, Menu, MenuItem } from '@mui/material';
import { useId, useState, type ReactNode } from 'react';

export interface MoreAction {
  key: string;
  label: string;
  icon?: ReactNode;
  onClick: () => void;
  disabled?: boolean;
}

/**
 * Several secondary actions behind one button, so a page header keeps a single loud button and the rest
 * waits one tap away. A menu is the right tool for what is used now and then (export, history, a guide) and
 * the wrong one for what is used every visit, which stays on the page.
 */
export function MoreActionsMenu({
  label,
  startIcon,
  actions,
}: {
  label: string;
  startIcon?: ReactNode;
  actions: readonly MoreAction[];
}) {
  const [anchor, setAnchor] = useState<HTMLElement | null>(null);
  const id = useId();

  if (actions.length === 0) return null;

  return (
    <>
      <Button
        variant="outlined"
        startIcon={startIcon}
        endIcon={<ArrowDropDownOutlined />}
        aria-haspopup="menu"
        aria-expanded={anchor ? 'true' : undefined}
        aria-controls={anchor ? id : undefined}
        onClick={(event) => setAnchor(event.currentTarget)}
      >
        {label}
      </Button>
      <Menu id={id} anchorEl={anchor} open={!!anchor} onClose={() => setAnchor(null)}>
        {actions.map((action) => (
          <MenuItem
            key={action.key}
            disabled={action.disabled}
            onClick={() => {
              setAnchor(null);
              action.onClick();
            }}
          >
            {action.icon && <ListItemIcon>{action.icon}</ListItemIcon>}
            <ListItemText>{action.label}</ListItemText>
          </MenuItem>
        ))}
      </Menu>
    </>
  );
}
