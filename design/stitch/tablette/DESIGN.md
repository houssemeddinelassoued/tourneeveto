---
name: Clinical Fieldwork
colors:
  surface: '#f9f9ff'
  surface-dim: '#cfdaf2'
  surface-bright: '#f9f9ff'
  surface-container-lowest: '#ffffff'
  surface-container-low: '#f0f3ff'
  surface-container: '#e7eeff'
  surface-container-high: '#dee8ff'
  surface-container-highest: '#d8e3fb'
  on-surface: '#111c2d'
  on-surface-variant: '#3f4850'
  inverse-surface: '#263143'
  inverse-on-surface: '#ecf1ff'
  outline: '#707881'
  outline-variant: '#bfc7d2'
  surface-tint: '#006398'
  primary: '#006194'
  on-primary: '#ffffff'
  primary-container: '#007bb9'
  on-primary-container: '#fdfcff'
  inverse-primary: '#93ccff'
  secondary: '#006591'
  on-secondary: '#ffffff'
  secondary-container: '#39b8fd'
  on-secondary-container: '#004666'
  tertiary: '#006947'
  on-tertiary: '#ffffff'
  tertiary-container: '#00855b'
  on-tertiary-container: '#f5fff6'
  error: '#ba1a1a'
  on-error: '#ffffff'
  error-container: '#ffdad6'
  on-error-container: '#93000a'
  primary-fixed: '#cce5ff'
  primary-fixed-dim: '#93ccff'
  on-primary-fixed: '#001d31'
  on-primary-fixed-variant: '#004b73'
  secondary-fixed: '#c9e6ff'
  secondary-fixed-dim: '#89ceff'
  on-secondary-fixed: '#001e2f'
  on-secondary-fixed-variant: '#004c6e'
  tertiary-fixed: '#6ffbbe'
  tertiary-fixed-dim: '#4edea3'
  on-tertiary-fixed: '#002113'
  on-tertiary-fixed-variant: '#005236'
  background: '#f9f9ff'
  on-background: '#111c2d'
  surface-variant: '#d8e3fb'
typography:
  display:
    fontFamily: Atkinson Hyperlegible Next
    fontSize: 36px
    fontWeight: '700'
    lineHeight: 44px
    letterSpacing: -0.02em
  headline-lg:
    fontFamily: Atkinson Hyperlegible Next
    fontSize: 28px
    fontWeight: '700'
    lineHeight: 36px
    letterSpacing: -0.01em
  headline-lg-mobile:
    fontFamily: Atkinson Hyperlegible Next
    fontSize: 24px
    fontWeight: '700'
    lineHeight: 32px
    letterSpacing: -0.01em
  headline-md:
    fontFamily: Atkinson Hyperlegible Next
    fontSize: 22px
    fontWeight: '600'
    lineHeight: 28px
  headline-sm:
    fontFamily: Atkinson Hyperlegible Next
    fontSize: 18px
    fontWeight: '600'
    lineHeight: 24px
  body-lg:
    fontFamily: Atkinson Hyperlegible Next
    fontSize: 18px
    fontWeight: '400'
    lineHeight: 26px
  body-md:
    fontFamily: Atkinson Hyperlegible Next
    fontSize: 16px
    fontWeight: '400'
    lineHeight: 24px
  body-sm:
    fontFamily: Atkinson Hyperlegible Next
    fontSize: 14px
    fontWeight: '500'
    lineHeight: 20px
  label-lg:
    fontFamily: Atkinson Hyperlegible Next
    fontSize: 16px
    fontWeight: '700'
    lineHeight: 20px
    letterSpacing: 0.01em
  label-md:
    fontFamily: Atkinson Hyperlegible Next
    fontSize: 14px
    fontWeight: '700'
    lineHeight: 18px
    letterSpacing: 0.02em
  label-sm:
    fontFamily: Atkinson Hyperlegible Next
    fontSize: 12px
    fontWeight: '700'
    lineHeight: 16px
    letterSpacing: 0.04em
  data-id:
    fontFamily: Atkinson Hyperlegible Next
    fontSize: 20px
    fontWeight: '700'
    lineHeight: 24px
    letterSpacing: 0.05em
rounded:
  sm: 0.25rem
  DEFAULT: 0.5rem
  md: 0.75rem
  lg: 1rem
  xl: 1.5rem
  full: 9999px
spacing:
  gutter: 1rem
  gutter-tablet: 1.5rem
  gutter-desktop: 2rem
  margin: 1rem
  margin-tablet: 1.5rem
  margin-desktop: 2.5rem
  space-xs: 0.375rem
  space-sm: 0.75rem
  space-md: 1rem
  space-lg: 1.5rem
  space-xl: 2.25rem
---

## Brand & Style

This design system delivers a clinical, high-dependability workspace engineered for rural dairy veterinarians operating in demanding environments (cold barns, bright natural glare, dirty touchscreens, and gloved operation). 

The emotional tone balances veterinary authority, hygienic precision, and effortless operational speed. Visually, the aesthetic marries modern clinical minimalism with high-legibility rugged utility:
- **Crisp, clean clinical surfaces** over pure white and balanced slate foundations.
- **High-contrast visual landmarks** allowing instant data capture at arm's length or in direct sunlight.
- **Physical-grade tactile reliability**: generously proportioned controls that eliminate mis-taps when wearing latex or nitrile examination gloves.
- **Always-present sync transparency**: explicit, unmissable states communicating offline persistence, pending uploads, and record validation.

## Colors

The palette is anchored by clinical cyan-blues, high-legibility slate neutrals, and functional alert accents that exceed WCAG AAA/AA+ contrast thresholds against pure white (#FFFFFF) and barn-safe tinted backgrounds (#F8FAFC).

### Core Roles
- **Primary Clinical (`#0284C7`)**: Used for focal actions, active herd navigation states, and primary diagnostic triggers.
- **Secondary Interactive (`#0EA5E9`)**: Used for secondary touches, segmented controls, focused states, and non-destructive confirmations.
- **Clinical Wash (`#E0F2FE`)**: Used for primary selection backgrounds, active record highlights, and table row selections.
- **Tertiary Healthy / Synced (`#10B981`)**: Dedicated to validated health statuses, successful synchronizations, and routine checks.

### Diagnostic & Status Accents
- **Amber Warning (`#D97706`)**: Withdrawals, withdrawal periods (délais d'attente lait/viande), pending sync records.
- **Coral Alert (`#E11D48`)**: Immediate veterinary emergencies, critical protocol alerts, contagion warnings.
- **Neutral Deep Slate (`#1E293B`)**: Ultra-sharp text and structural iconography; never uses pure black to prevent harsh eye strain under rapid ambient lighting shifts.
- **Neutral Muted Slate (`#475569`)**: Secondary metadata, herd tags, and unit labels.
- **Surface Canvas (`#F8FAFC`)**: Base page background to mitigate field glare.
- **Surface Card (`#FFFFFF`)**: Pure white containers for data cards and input blocks.

## Typography

The design system exclusively implements Atkinson Hyperlegible Next across all roles. Its hyper-distinct letterforms (such as disambiguated zeros, capital 'I' with serifs, and open counters) prevent misreading critical cattle national ID numbers (FR xx xxxx xxxx), drug dosages, and batch dates in low-light barns.

- **Data-ID Level**: Specifically tailored for National Identification Tags (boucles auriculaires). It emphasizes letter tracking and bold optical weight for fast distance verification.
- **Labels & Micro-indicators**: Always set to medium or bold weights. Light and thin weights are strictly prohibited to ensure readability through smeared screens or wet gloves.

## Layout & Spacing

Layouts follow a structured fluid grid optimized around rugged field tablets (iPad/Active Tab) and one-handed mobile triage. 

- **Mobile (< 768px)**: 4-column fluid layout, single-column workflow stack. Bottom-docked primary action trays for thumb reachable gloved interaction.
- **Tablet / In-Cab Mount (768px - 1199px)**: 8-column layout. Split-pane navigation: herd listing on the left, active animal clinical protocol on the right.
- **Desktop / Clinic Station (≥ 1200px)**: 12-column layout. Comprehensive batch overview and synchronization dashboard.

### Field-Specific Spacing Rules
- All interactive tap targets require a minimum clearance box of 56px in primary workflows (treatment entry, animal switching) and never drop below 48px anywhere else.
- Padding inside cards and data entry containers is generous (`space-md` to `space-lg`) to prevent boundary clipping when tap precision is degraded by cold fingers or gloved hands.

## Elevation & Depth

This design system deliberately minimizes soft ambient shadows in favor of **structural boundaries and clean tonal elevation**. In outdoor light and barn environments, subtle drop shadows wash out completely and reduce interface contrast.

- **Ground Level (Canvas)**: Background color `#F8FAFC`.
- **Level 1 (Clinical Cards & Panels)**: Pure white background `#FFFFFF`, bordered with a 1.5px solid border in `#CBD5E1`. No shadow.
- **Level 2 (Active Modals & Drawer Overlays)**: Surface `#FFFFFF`, reinforced by a 2px solid border `#94A3B8` and a high-opacity functional shadow: `0 8px 24px -4px rgba(15, 23, 42, 0.18)`.
- **Level 3 (Sticky Offline & Critical Alert Trays)**: Surface `#1E293B` or `#E0F2FE` with a distinct 2px contrasting border for immediate situational awareness regardless of surface lighting.

## Shapes

The roundedness level is set to `2` (medium-soft):
- Base interactive elements (buttons, text inputs, selectors) use `0.75rem` (12px) border radiuses.
- Larger structural card panels and modal dialogs use `1rem` (16px) border radiuses.
- Badge indicators and status pills maintain a full continuous curve (`9999px`) to distinguish categorical metadata from tap targets.

This geometry softens harsh boxy clinical lines while preserving clear spatial anchors for finger placement.

## Components

### Buttons
- **Primary**: Min height 56px. Filled `#0284C7`, text `#FFFFFF`, bold `label-lg`. Border radius 12px. Active press state shifts to `#0369A1` with an immediate 2px inset ring.
- **Secondary**: Min height 56px. Background `#E0F2FE`, text `#0284C7`, 1.5px border `#BAE6FD`.
- **Destructive**: Min height 56px. Background `#FFE4E6`, text `#BE123C`, 1.5px border `#FDA4AF`.
- **Glove Safety**: Minimum 12px margin around all button perimeters to prevent accidental double-activations.

### Offline & Sync Status Banner
- Prominent fixed header indicator:
  - **Online & Synced**: `#ECFDF5` background, `#047857` label, small solid green dot.
  - **Offline (Autonomous)**: `#FEF3C7` background, 2px `#F59E0B` solid border, text `#92400E` with offline duration and pending action counter (e.g., "Hors-ligne · 8 actes en attente").
  - **Sync Error**: `#FFF1F2` background, 2px `#E11D48` solid border, text `#9F1239` with explicit retry button.

### Form Inputs & Steppers
- **Inputs**: Min height 56px. 2px solid border `#CBD5E1`. Focused state: 2px solid `#0284C7` with a 4px soft ring `#BAE6FD`. Font size 18px (`body-lg`) to prevent iOS zoom and maintain effortless legibility.
- **Veterinary Quick-Steppers**: High-frequency dosage and gestation inputs replace standard small spinners with large 56x56px minus/plus blocks flanking an 18px bold numeric center.

### Bovine ID & Animal Cards
- Card container with 16px radius, pure white background, 1.5px `#CBD5E1` border.
- Features a dedicated top bar displaying the National Cow Tag in `data-id` (`20px Atkinson Hyperlegible Next`), accompanied by high-contrast health chips (e.g., "Mammitis", "Gestation J+210").
- Withdrawal period warnings are styled with a diagonal striped amber-and-white header flag whenever milk or meat exclusion applies.

### Chips & Badges
- Min height 36px for non-interactive indicators; min height 48px for selectable filter chips.
- Non-interactive badges use bold 12px/14px text with strong border contrasts (e.g., background `#F1F5F9`, border 1px `#94A3B8`, text `#1E293B`).

### Checkboxes & Segmented Toggles
- Selection boxes use a minimum size of 28x28px inside a 52px target container.
- Segmented switches (e.g., Left Quarter / Right Quarter diagnosis) use equal 52px height blocks with high-contrast active fill (`#0284C7` with white text) and distinct separation gaps.