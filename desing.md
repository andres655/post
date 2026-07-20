---
name: Kinetic Ledger
colors:
  surface: '#f7f9fb'
  surface-dim: '#d8dadc'
  surface-bright: '#f7f9fb'
  surface-container-lowest: '#ffffff'
  surface-container-low: '#f2f4f6'
  surface-container: '#eceef0'
  surface-container-high: '#e6e8ea'
  surface-container-highest: '#e0e3e5'
  on-surface: '#191c1e'
  on-surface-variant: '#45464d'
  inverse-surface: '#2d3133'
  inverse-on-surface: '#eff1f3'
  outline: '#76777d'
  outline-variant: '#c6c6cd'
  surface-tint: '#565e74'
  primary: '#000000'
  on-primary: '#ffffff'
  primary-container: '#131b2e'
  on-primary-container: '#7c839b'
  inverse-primary: '#bec6e0'
  secondary: '#006c49'
  on-secondary: '#ffffff'
  secondary-container: '#6cf8bb'
  on-secondary-container: '#00714d'
  tertiary: '#000000'
  on-tertiary: '#ffffff'
  tertiary-container: '#2a1700'
  on-tertiary-container: '#b87500'
  error: '#ba1a1a'
  on-error: '#ffffff'
  error-container: '#ffdad6'
  on-error-container: '#93000a'
  primary-fixed: '#dae2fd'
  primary-fixed-dim: '#bec6e0'
  on-primary-fixed: '#131b2e'
  on-primary-fixed-variant: '#3f465c'
  secondary-fixed: '#6ffbbe'
  secondary-fixed-dim: '#4edea3'
  on-secondary-fixed: '#002113'
  on-secondary-fixed-variant: '#005236'
  tertiary-fixed: '#ffddb8'
  tertiary-fixed-dim: '#ffb95f'
  on-tertiary-fixed: '#2a1700'
  on-tertiary-fixed-variant: '#653e00'
  background: '#f7f9fb'
  on-background: '#191c1e'
  surface-variant: '#e0e3e5'
typography:
  display-price:
    fontFamily: Inter
    fontSize: 48px
    fontWeight: '700'
    lineHeight: 56px
    letterSpacing: -0.02em
  headline-lg:
    fontFamily: Inter
    fontSize: 30px
    fontWeight: '600'
    lineHeight: 38px
  headline-md:
    fontFamily: Inter
    fontSize: 24px
    fontWeight: '600'
    lineHeight: 32px
  headline-sm:
    fontFamily: Inter
    fontSize: 20px
    fontWeight: '600'
    lineHeight: 28px
  body-lg:
    fontFamily: Inter
    fontSize: 18px
    fontWeight: '400'
    lineHeight: 28px
  body-md:
    fontFamily: Inter
    fontSize: 16px
    fontWeight: '400'
    lineHeight: 24px
  label-md:
    fontFamily: JetBrains Mono
    fontSize: 14px
    fontWeight: '500'
    lineHeight: 20px
  label-sm:
    fontFamily: JetBrains Mono
    fontSize: 12px
    fontWeight: '500'
    lineHeight: 16px
  headline-md-mobile:
    fontFamily: Inter
    fontSize: 20px
    fontWeight: '600'
    lineHeight: 28px
rounded:
  sm: 0.25rem
  DEFAULT: 0.5rem
  md: 0.75rem
  lg: 1rem
  xl: 1.5rem
  full: 9999px
spacing:
  unit: 4px
  touch-target-min: 48px
  gutter: 24px
  margin-mobile: 16px
  margin-desktop: 32px
  card-padding: 20px
---

## Brand & Style

The design system is engineered for the high-velocity environment of Small and Medium Enterprises (SMEs). It balances the reliability of traditional financial institutions with the agility of modern SaaS platforms. The visual narrative is built on **Modern Corporate** principles: efficient, structured, and inherently trustworthy.

The goal is to minimize cognitive load for staff during long shifts. This is achieved through a "Focus-First" philosophy, where whitespace is used strategically to separate transactional data from navigational controls. The aesthetic is clean and professional, prioritizing clarity of information over decorative flair, ensuring that the interface feels like a high-performance tool rather than a generic application.

## Colors

The palette is anchored by a deep Navy Blue (Primary), representing stability and the "ledger" aspect of the business. An Action Green (Secondary) is reserved exclusively for successful financial outcomes—completing a sale, confirming a payment, or adding stock. 

- **Primary (Navy):** Used for sidebar navigation, primary headers, and core brand elements.
- **Secondary (Green):** Used for "Charge," "Complete Order," and positive growth trends.
- **Tertiary (Amber):** A functional accent for low-stock warnings and pending sync states.
- **Backgrounds:** A tiered system of cool grays (`#F8FAFC` to `#F1F5F9`) distinguishes the main canvas from surface-level cards, reducing screen glare.
- **Semantic Red:** Used sparingly for "Void," "Delete," or "System Error" to maintain high urgency.

## Typography

This design system utilizes **Inter** for all UI elements to ensure maximum legibility across various screen resolutions. Its tall x-height and neutral character make it ideal for data-dense tables and rapid scanning of product names.

For financial figures, SKU numbers, and technical data, **JetBrains Mono** is employed at the label level. This monospaced font ensures that price columns remain perfectly aligned, allowing users to compare totals and quantities at a glance without visual "jitter."

**Typographic Rules:**
- All prices should use `display-price` or `headline-md` with `label-md` for the currency symbol.
- Use `600` weight for headings to maintain a strong hierarchy against dense body text.

## Layout & Spacing

The layout follows a **Fluid Grid** model with a hard constraint on touch-accessibility. Every interactive element must adhere to a minimum 48px touch target, even on desktop, to accommodate fast-paced environments where mouse precision is secondary to speed.

- **Desktop:** A 12-column grid. The POS terminal typically uses a 70/30 split: 70% for product selection/search and 30% for the persistent cart/receipt view.
- **Tablet:** Transitions to a stacked view or a collapsible cart sidebar.
- **Rhythm:** An 8px linear scale is used for all padding and margins to create a logical, predictable breathing room between data points.

## Elevation & Depth

To maintain a clean, "uncluttered" feel, this design system uses **Tonal Layers** rather than heavy shadows. Depth is communicated through subtle border shifts and background fills.

- **Level 0 (Canvas):** The base background layer in the lightest gray.
- **Level 1 (Cards):** White surfaces with a very soft, 1px neutral border (`#E2E8F0`). No shadow.
- **Level 2 (Modals/Popovers):** Elevated with a 15% opacity primary-tinted shadow (8px blur) to focus attention on critical actions like "Confirm Payment."
- **Active State:** Use a 2px Primary Blue border to indicate selected items in a list or active input fields.

## Shapes

The design system employs a **Rounded** (Level 2) shape language. Standard components use a `0.5rem` (8px) radius, providing a modern, approachable feel while maintaining a sense of structural integrity. 

Large containers like dashboard cards or the checkout panel use `rounded-lg` (16px) to clearly define major functional areas. This soft geometry helps differentiate the "software" from the "hardware" (the physical POS terminal) while remaining professional.

## Components

### Buttons
- **Primary Action (Charge/Pay):** High-contrast Secondary Green background with white text. Bold, 18px font.
- **Secondary Action (Add Item):** Primary Navy background with white text.
- **Ghost Buttons:** Used for secondary navigation like "Print Last Receipt."

### Tables & Lists
- **Data Tables:** Large 16px cell padding. Alternate row striping is prohibited; use subtle 1px horizontal dividers only.
- **Inventory List:** Product cards should include a small thumbnail, the name in `body-md`, and the stock level in `label-sm`.

### Inputs
- **Search Bar:** Large, prominent input at the top of the product grid with a persistent magnifying glass icon. 56px height for easy tapping.
- **Numeric Pad:** Custom on-screen numeric inputs for custom prices or quantities, with oversized buttons and haptic-style visual feedback on press.

### Indicators
- **Offline Mode:** A persistent banner at the top of the screen in Tertiary Amber with a "Syncing..." or "Offline" status icon.
- **Status Chips:** Low-opacity background tints (e.g., light green background with dark green text) for statuses like "Paid," "Refunded," or "Partially Fulfilled."

### Dashboard Cards
- Metrics (Daily Sales, Average Basket) should be displayed in cards with `display-price` for the main figure and a small sparkline chart indicating a 24-hour trend.