/**
 * Time Zone Utilities
 *
 * List of commonly used time zones for exercise scheduling.
 * Uses IANA time zone identifiers compatible with .NET TimeZoneInfo.
 */

export interface TimeZoneOption {
  id: string
  label: string
  region: string
}

/**
 * North America time zones (US, Canada, Mexico)
 */
const NORTH_AMERICA_TIMEZONES: TimeZoneOption[] = [
  { id: 'America/New_York', label: 'Eastern (EST/EDT)', region: 'North America' },
  { id: 'America/Chicago', label: 'Central (CST/CDT)', region: 'North America' },
  { id: 'America/Denver', label: 'Mountain (MST/MDT)', region: 'North America' },
  { id: 'America/Phoenix', label: 'Arizona (MST, no DST)', region: 'North America' },
  { id: 'America/Los_Angeles', label: 'Pacific (PST/PDT)', region: 'North America' },
  { id: 'America/Anchorage', label: 'Alaska (AKST/AKDT)', region: 'North America' },
  { id: 'Pacific/Honolulu', label: 'Hawaii (HST, no DST)', region: 'North America' },
  { id: 'America/Puerto_Rico', label: 'Atlantic (AST, no DST)', region: 'North America' },
  { id: 'America/Mexico_City', label: 'Mexico City (CST/CDT)', region: 'North America' },
  { id: 'America/Tijuana', label: 'Tijuana (PST/PDT)', region: 'North America' },
]

/**
 * South/Central America time zones
 */
const SOUTH_AMERICA_TIMEZONES: TimeZoneOption[] = [
  { id: 'America/Sao_Paulo', label: 'São Paulo (BRT)', region: 'South America' },
  { id: 'America/Argentina/Buenos_Aires', label: 'Buenos Aires (ART)', region: 'South America' },
  { id: 'America/Santiago', label: 'Santiago (CLT/CLST)', region: 'South America' },
  { id: 'America/Bogota', label: 'Bogotá (COT)', region: 'South America' },
  { id: 'America/Lima', label: 'Lima (PET)', region: 'South America' },
  { id: 'America/Caracas', label: 'Caracas (VET)', region: 'South America' },
]

/**
 * European time zones
 */
const EUROPE_TIMEZONES: TimeZoneOption[] = [
  { id: 'Europe/London', label: 'London (GMT/BST)', region: 'Western Europe' },
  { id: 'Europe/Dublin', label: 'Dublin (GMT/IST)', region: 'Western Europe' },
  { id: 'Europe/Lisbon', label: 'Lisbon (WET/WEST)', region: 'Western Europe' },
  { id: 'Europe/Paris', label: 'Paris (CET/CEST)', region: 'Western Europe' },
  { id: 'Europe/Berlin', label: 'Berlin (CET/CEST)', region: 'Central Europe' },
  { id: 'Europe/Rome', label: 'Rome (CET/CEST)', region: 'Central Europe' },
  { id: 'Europe/Amsterdam', label: 'Amsterdam (CET/CEST)', region: 'Central Europe' },
  { id: 'Europe/Brussels', label: 'Brussels (CET/CEST)', region: 'Central Europe' },
  { id: 'Europe/Warsaw', label: 'Warsaw (CET/CEST)', region: 'Eastern Europe' },
  { id: 'Europe/Prague', label: 'Prague (CET/CEST)', region: 'Eastern Europe' },
  { id: 'Europe/Budapest', label: 'Budapest (CET/CEST)', region: 'Eastern Europe' },
  { id: 'Europe/Athens', label: 'Athens (EET/EEST)', region: 'Eastern Europe' },
  { id: 'Europe/Bucharest', label: 'Bucharest (EET/EEST)', region: 'Eastern Europe' },
  { id: 'Europe/Sofia', label: 'Sofia (EET/EEST)', region: 'Eastern Europe' },
  { id: 'Europe/Helsinki', label: 'Helsinki (EET/EEST)', region: 'Eastern Europe' },
  { id: 'Europe/Kyiv', label: 'Kyiv (EET/EEST)', region: 'Eastern Europe' },
  { id: 'Europe/Moscow', label: 'Moscow (MSK)', region: 'Russia' },
  { id: 'Europe/Istanbul', label: 'Istanbul (TRT)', region: 'Turkey' },
]

/**
 * Middle East time zones
 */
const MIDDLE_EAST_TIMEZONES: TimeZoneOption[] = [
  { id: 'Asia/Jerusalem', label: 'Jerusalem (IST/IDT)', region: 'Middle East' },
  { id: 'Asia/Beirut', label: 'Beirut (EET/EEST)', region: 'Middle East' },
  { id: 'Asia/Amman', label: 'Amman (EET/EEST)', region: 'Middle East' },
  { id: 'Asia/Damascus', label: 'Damascus (EET/EEST)', region: 'Middle East' },
  { id: 'Asia/Baghdad', label: 'Baghdad (AST)', region: 'Middle East' },
  { id: 'Asia/Riyadh', label: 'Riyadh (AST)', region: 'Middle East' },
  { id: 'Asia/Kuwait', label: 'Kuwait (AST)', region: 'Middle East' },
  { id: 'Asia/Qatar', label: 'Qatar (AST)', region: 'Middle East' },
  { id: 'Asia/Dubai', label: 'Dubai (GST)', region: 'Middle East' },
  { id: 'Asia/Muscat', label: 'Muscat (GST)', region: 'Middle East' },
  { id: 'Asia/Tehran', label: 'Tehran (IRST/IRDT)', region: 'Middle East' },
]

/**
 * Asia-Pacific time zones
 */
const ASIA_PACIFIC_TIMEZONES: TimeZoneOption[] = [
  { id: 'Asia/Karachi', label: 'Karachi (PKT)', region: 'South Asia' },
  { id: 'Asia/Kolkata', label: 'India (IST)', region: 'South Asia' },
  { id: 'Asia/Dhaka', label: 'Dhaka (BST)', region: 'South Asia' },
  { id: 'Asia/Bangkok', label: 'Bangkok (ICT)', region: 'Southeast Asia' },
  { id: 'Asia/Jakarta', label: 'Jakarta (WIB)', region: 'Southeast Asia' },
  { id: 'Asia/Singapore', label: 'Singapore (SGT)', region: 'Southeast Asia' },
  { id: 'Asia/Kuala_Lumpur', label: 'Kuala Lumpur (MYT)', region: 'Southeast Asia' },
  { id: 'Asia/Manila', label: 'Manila (PHT)', region: 'Southeast Asia' },
  { id: 'Asia/Hong_Kong', label: 'Hong Kong (HKT)', region: 'East Asia' },
  { id: 'Asia/Shanghai', label: 'China (CST)', region: 'East Asia' },
  { id: 'Asia/Taipei', label: 'Taipei (CST)', region: 'East Asia' },
  { id: 'Asia/Seoul', label: 'Seoul (KST)', region: 'East Asia' },
  { id: 'Asia/Tokyo', label: 'Tokyo (JST)', region: 'East Asia' },
]

/**
 * Australia & Pacific time zones
 */
const AUSTRALIA_PACIFIC_TIMEZONES: TimeZoneOption[] = [
  { id: 'Australia/Perth', label: 'Perth (AWST)', region: 'Australia' },
  { id: 'Australia/Darwin', label: 'Darwin (ACST, no DST)', region: 'Australia' },
  { id: 'Australia/Adelaide', label: 'Adelaide (ACST/ACDT)', region: 'Australia' },
  { id: 'Australia/Brisbane', label: 'Brisbane (AEST, no DST)', region: 'Australia' },
  { id: 'Australia/Sydney', label: 'Sydney (AEST/AEDT)', region: 'Australia' },
  { id: 'Australia/Melbourne', label: 'Melbourne (AEST/AEDT)', region: 'Australia' },
  { id: 'Pacific/Auckland', label: 'Auckland (NZST/NZDT)', region: 'Pacific' },
  { id: 'Pacific/Fiji', label: 'Fiji (FJT)', region: 'Pacific' },
]

/**
 * UTC reference
 */
const UTC_TIMEZONE: TimeZoneOption[] = [
  { id: 'UTC', label: 'UTC / GMT (Coordinated Universal Time)', region: 'International' },
]

/**
 * All available time zones grouped for display
 * Order: US/Canada first (primary market), then Americas, Europe, Middle East, Asia-Pacific, Australia, UTC last
 */
export const TIME_ZONES: TimeZoneOption[] = [
  ...NORTH_AMERICA_TIMEZONES,
  ...SOUTH_AMERICA_TIMEZONES,
  ...EUROPE_TIMEZONES,
  ...MIDDLE_EAST_TIMEZONES,
  ...ASIA_PACIFIC_TIMEZONES,
  ...AUSTRALIA_PACIFIC_TIMEZONES,
  ...UTC_TIMEZONE,
]

/**
 * Get display label for a timezone ID
 */
export const getTimeZoneLabel = (timeZoneId: string): string => {
  const zone = TIME_ZONES.find(tz => tz.id === timeZoneId)
  return zone?.label ?? timeZoneId
}

/**
 * Get the user's browser timezone
 */
export const getBrowserTimeZone = (): string => {
  try {
    return Intl.DateTimeFormat().resolvedOptions().timeZone
  } catch {
    return 'America/New_York' // Fallback for older browsers
  }
}

/**
 * Check if a timezone ID is valid (exists in our list)
 */
export const isValidTimeZone = (timeZoneId: string): boolean => {
  return TIME_ZONES.some(tz => tz.id === timeZoneId)
}

/**
 * Get timezone option by ID, or return a custom option for unknown zones
 */
export const getTimeZoneOption = (timeZoneId: string): TimeZoneOption => {
  const found = TIME_ZONES.find(tz => tz.id === timeZoneId)
  if (found) return found

  // Return custom option for unrecognized timezone (may be valid IANA zone not in our list)
  return {
    id: timeZoneId,
    label: timeZoneId,
    region: 'Other',
  }
}
