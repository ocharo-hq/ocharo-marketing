/**
 * The `localStorage` key Editorial's colour mode lives under: the SDK `ColorModeProvider` persists `light`, `dark` or
 * `system` there, and the pre-paint `/studio/theme.js` the build generates reads it before the first frame.
 */
export const ThemeStorageKey = 'ocharo.editorial.theme';
