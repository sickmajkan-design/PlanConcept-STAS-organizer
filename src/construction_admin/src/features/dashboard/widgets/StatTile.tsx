import { ButtonBase, Stack, Typography } from "@mui/material";
import type { ReactNode } from "react";
import { Link as RouterLink } from "react-router-dom";

import { Pill, type Tone } from "./widgetUi";

interface StatTileProps {
  /** Kept for the callers that pass one; the tile leads with its number, not an icon. */
  icon?: ReactNode;
  label: string;
  value: string;
  hint?: string;
  hintColor?: "warning.main" | "error.main" | "success.main" | "text.secondary";
  /** Kept for the callers that pass one; the tile uses the theme's own colours. */
  accent?: string;
  /** Where a press goes. A tile with none is not pressable. */
  to?: string;
}

const HINT_TONE: Record<NonNullable<StatTileProps["hintColor"]>, Tone> = {
  "warning.main": "warn",
  "error.main": "bad",
  "success.main": "good",
  "text.secondary": "neutral",
};

/**
 * One figure on a soft tile: the label above, the number as the biggest thing on it, and a small pill for the
 * change or the state. The number's size follows the width the widget was given, so a stretched widget on a wide
 * monitor gets larger figures and a narrow one on a phone gets compact ones. A tile with a destination presses
 * in under the finger and goes there.
 */
export function StatTile({
  label,
  value,
  hint,
  hintColor = "text.secondary",
  to,
}: StatTileProps) {
  const body = (
    // The label at the top and the figure at the bottom, so tiles side by side line their numbers up even when
    // one label takes two lines.
    <Stack
      sx={{
        alignItems: "flex-start",
        justifyContent: "space-between",
        gap: 0.75,
        minWidth: 0,
        width: "100%",
        textAlign: "left",
      }}
    >
      <Typography
        variant="caption"
        color="text.secondary"
        sx={{ lineHeight: 1.25, fontSize: "0.8rem" }}
      >
        {label}
      </Typography>
      <Stack
        spacing={0.5}
        sx={{ alignItems: "flex-start", minWidth: 0, maxWidth: "100%" }}
      >
        <Typography
          component="span"
          sx={{
            fontVariantNumeric: "tabular-nums",
            fontWeight: 800,
            lineHeight: 1.05,
            letterSpacing: "-0.03em",
            fontSize: "clamp(1.6rem, 7cqw, 2.6rem)",
            // A zero is the absence of something, so it recedes.
            color: /^[0\s/]+$/.test(value) ? "text.secondary" : "text.primary",
            overflowWrap: "anywhere",
          }}
        >
          {value}
        </Typography>
        {hint && <Pill tone={HINT_TONE[hintColor]}>{hint}</Pill>}
      </Stack>
    </Stack>
  );

  const sx = {
    // Two to a row on the narrowest widget, more as it widens.
    flex: "1 1 140px",
    minWidth: 0,
    borderRadius: "18px",
    px: 2,
    py: 1.75,
    display: "flex",
    alignItems: "flex-start",
    color: "inherit",
    bgcolor: (theme: { palette: { mode: string } }) =>
      theme.palette.mode === "dark"
        ? "rgba(255,255,255,0.05)"
        : "rgba(31,27,22,0.04)",
  };

  if (!to) {
    return <Stack sx={sx}>{body}</Stack>;
  }

  return (
    <ButtonBase
      component={RouterLink}
      to={to}
      sx={{
        ...sx,
        transition: "transform 0.12s ease-out",
        "&:active": { transform: "scale(0.97)" },
        "&:focus-visible": {
          outline: "2px solid",
          outlineColor: "primary.main",
          outlineOffset: 2,
        },
        "@media (prefers-reduced-motion: reduce)": {
          transition: "none",
          "&:active": { transform: "none" },
        },
      }}
    >
      {body}
    </ButtonBase>
  );
}
