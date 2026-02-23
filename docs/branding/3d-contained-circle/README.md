# Finalist 3D - Contained Circle Pulse - PWA Icons

## Files Included

### Favicons (Browser Tab)
- `favicon.ico` - Multi-size ICO (16x16, 32x32, 48x48) for broad compatibility
- `favicon.svg` - Scalable SVG favicon for modern browsers
- `favicon-16x16.png` - Standard favicon size
- `favicon-32x32.png` - Retina favicon size
- `favicon-48x48.png` - Large favicon size

### Apple Touch Icon
- `apple-touch-icon.png` - 180x180 for iOS home screen

### PWA Manifest Icons
- `icon-192x192.png` - Standard PWA icon
- `icon-512x512.png` - Large PWA icon / splash screen
- `icon-maskable-192x192.png` - Maskable icon for Android adaptive icons
- `icon-maskable-512x512.png` - Large maskable icon

### Source Files
- `icon-source-light.svg` - Light background version (for large sizes)
- `icon-source-dark.svg` - Dark background version (for small sizes)
- `icon-maskable.svg` - Maskable version with safe zone padding

## HTML Integration

Add to your `<head>`:

```html
<!-- Favicons -->
<link rel="icon" href="/favicon.ico" sizes="48x48">
<link rel="icon" href="/favicon.svg" type="image/svg+xml">
<link rel="icon" type="image/png" sizes="32x32" href="/favicon-32x32.png">
<link rel="icon" type="image/png" sizes="16x16" href="/favicon-16x16.png">
<link rel="apple-touch-icon" sizes="180x180" href="/apple-touch-icon.png">
```

## Web App Manifest (manifest.json)

```json
{
  "name": "Cadence",
  "short_name": "Cadence",
  "icons": [
    {
      "src": "/icon-192x192.png",
      "sizes": "192x192",
      "type": "image/png"
    },
    {
      "src": "/icon-512x512.png",
      "sizes": "512x512",
      "type": "image/png"
    },
    {
      "src": "/icon-maskable-192x192.png",
      "sizes": "192x192",
      "type": "image/png",
      "purpose": "maskable"
    },
    {
      "src": "/icon-maskable-512x512.png",
      "sizes": "512x512",
      "type": "image/png",
      "purpose": "maskable"
    }
  ],
  "theme_color": "#1e3a5f",
  "background_color": "#ffffff",
  "display": "standalone"
}
```

## Colors Used
- Navy Blue: `#1e3a5f` (primary)
- Teal: `#0d9488` (accent)
- White: `#ffffff` (background)

## Notes
- Small favicons (16-48px) use the dark/inverted version for better visibility
- Large icons (180-512px) use the light version for better detail
- Maskable icons have content within the safe zone (center 80%) for Android adaptive icons
