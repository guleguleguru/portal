# Desktop Portal DESIGN.md

Source reference: downloaded `VoltAgent/awesome-design-md` on 2026-05-13 and used `design-md/raycast/DESIGN.md` as the primary inspiration, with Linear's restraint as a secondary reference.

Desktop Portal is a Windows local command center, not a marketing page. The UI should feel like a fast launcher/control console for people who repeatedly switch to files, folders, apps, and websites.

## Visual Theme

- Direction: Raycast-like dark command surface adapted for Windows WPF.
- Mood: quiet, precise, dense, keyboard-first.
- The product UI is the visual signal. Avoid decorative illustration, hero sections, floating cards inside cards, gradients, or oversized marketing text.
- Use compact rows, hairline borders, keycap badges, and status pills to make rules easy to scan.

## Palette

- `PortalShell`: `#07080A` main window background.
- `PortalPanel`: `#0D0F12` primary elevated panels.
- `PortalPanelElevated`: `#12151A` secondary panel and row hover.
- `PortalCard`: `#171B22` compact stat and input surfaces.
- `PortalHairline`: `#252A33` borders and separators.
- `PortalInk`: `#F4F6F8` primary text.
- `PortalMuted`: `#9CA3AF` secondary text.
- `PortalFaint`: `#6B7280` low-priority text.
- `PortalAccent`: `#57C1FF` primary action and focus.
- `PortalSuccess`: `#59D499` enabled/healthy status.
- `PortalWarning`: `#FFC533` paused/warning status.
- `PortalDanger`: `#FF6161` conflict/error/destructive action.

## Typography

- Font family: `Microsoft YaHei UI`, `Segoe UI`, system sans.
- Window title: 24px, semibold.
- Section title: 20px, semibold.
- Body/table: 13px to 14px.
- Captions and badges: 12px.
- Do not use viewport-scaled text. Use stable fixed sizes and let layout carry hierarchy.

## Component Rules

- `PortalShell`: full dark application surface.
- `CommandButton`: 36px high, 8px radius, hairline border, dark panel background.
- Primary command uses `PortalAccent`; danger command uses red text and red-tinted border only.
- `Keycap`: compact 4px radius badge, dark card background, hairline border, monospaced-ish look for shortcut values.
- `StatusPill`: 6px to 8px radius badge with semantic border and subdued fill.
- Tables should keep fixed row height and clear dividers. Do not let hover/selection resize content.
- Dialogs should use the same shell/panel/input/button vocabulary as the main window.

## Layout

- Keep the left rail narrow and operational: app identity, runtime counters, config path.
- Main area starts with a compact command header, then toolbar, then full-height rules table.
- Do not create a landing page or explanatory hero. The first screen is the usable rule management surface.
- Keep cards at 8px radius or less unless a large panel needs 10px.

## Do

- Make shortcut values look like physical keycaps.
- Use status color sparingly and semantically.
- Keep surfaces dark but separated by one-step elevation and 1px borders.
- Keep text short; controls should be self-explanatory.

## Don't

- Do not add cloud/account language beyond the local privacy badge.
- Do not use gradient blobs, bokeh, decorative orbs, or marketing-style hero layouts.
- Do not introduce a colorful multi-accent palette for chrome.
- Do not make a one-note purple, slate, beige, or brown palette.
