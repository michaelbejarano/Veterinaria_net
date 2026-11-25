import type { ReactNode } from 'react'

type IconName = 'paw'|'home'|'calendar'|'users'|'heart'|'activity'|'clipboard'|'wallet'|'receipt'|'package'|'truck'|'shield'|'chart'|'grid'|'search'|'chevron'|'arrow'|'plus'|'refresh'|'logout'|'x'|'check'|'alert'|'external'|'menu'|'clock'|'filter'|'eye'|'play'|'code'|'spark'|'database'|'server'|'moon'|'settings'|'checkCircle'|'info'

const d: Record<IconName, ReactNode> = {
  paw: <><circle cx="11" cy="5.5" r="1"/><circle cx="17" cy="7" r="1"/><circle cx="6" cy="9" r="1"/><circle cx="20" cy="12" r="1"/><path d="M12 12c-3.5 0-6 3.1-6 5.2a2.6 2.6 0 0 0 3.7 2.3 5.5 5.5 0 0 1 4.6 0A2.6 2.6 0 0 0 18 17.2C18 15.1 15.5 12 12 12Z"/></>,
  home: <><path d="m3 10 9-7 9 7v10H3z"/><path d="M9 20v-7h6v7"/></>,
  calendar: <><rect x="3" y="5" width="18" height="16" rx="2"/><path d="M7 3v4M17 3v4M3 10h18M8 14h3M8 18h3"/></>,
  users: <><circle cx="9" cy="8" r="3"/><path d="M3 21v-2a6 6 0 0 1 12 0v2M16 5a3 3 0 0 1 0 6M17 15a5 5 0 0 1 4 5v1"/></>,
  heart: <><path d="M20 5.6a5.5 5.5 0 0 0-8 0 5.5 5.5 0 0 0-8 8l8 7 8-7a5.5 5.5 0 0 0 0-8Z"/><path d="M5 13h4l2-3 2 6 2-3h4"/></>,
  activity: <path d="M2 12h4l3-7 5 14 3-7h5"/>,
  clipboard: <><rect x="5" y="4" width="14" height="18" rx="2"/><rect x="9" y="2" width="6" height="5" rx="1"/><path d="M9 12h6M9 16h5"/></>,
  wallet: <><rect x="3" y="6" width="18" height="15" rx="2"/><path d="M3 9h18M4 6V4h14"/><circle cx="17" cy="15" r="1"/></>,
  receipt: <><path d="M5 3h14v18l-3-2-4 2-4-2-3 2zM9 8h6M9 12h6M9 16h3"/></>,
  package: <><path d="m12 2 9 5-9 5-9-5 9-5ZM3 7v10l9 5 9-5V7M12 12v10M7 5l9 5"/></>,
  truck: <><path d="M3 6h11v11H3zM14 10h4l3 4v3h-7"/><circle cx="7" cy="18" r="2"/><circle cx="18" cy="18" r="2"/></>,
  shield: <><path d="m12 2 8 3v6c0 5-3.2 8.5-8 11-4.8-2.5-8-6-8-11V5z"/><path d="m9 12 2 2 4-4"/></>,
  chart: <><path d="M3 21V3M3 21h18M7 17v-5M12 17V7M17 17v-8"/></>,
  grid: <><rect x="3" y="3" width="7" height="7" rx="1"/><rect x="14" y="3" width="7" height="7" rx="1"/><rect x="3" y="14" width="7" height="7" rx="1"/><rect x="14" y="14" width="7" height="7" rx="1"/></>,
  search: <><circle cx="11" cy="11" r="7"/><path d="m16 16 5 5"/></>,
  chevron: <path d="m9 18 6-6-6-6"/>,
  arrow: <><path d="M4 12h16m-7-7 7 7-7 7"/></>,
  plus: <path d="M12 5v14M5 12h14"/>,
  refresh: <><path d="M20 11a8 8 0 0 0-14-5L3 9M3 3v6h6M4 13a8 8 0 0 0 14 5l3-3M21 21v-6h-6"/></>,
  logout: <><path d="M9 4H5a2 2 0 0 0-2 2v12a2 2 0 0 0 2 2h4M13 16l4-4-4-4M9 12h8"/></>,
  x: <path d="M5 5l14 14M19 5 5 19"/>,
  check: <path d="m5 12 5 5L20 7"/>,
  alert: <><path d="m12 2 10 18H2z"/><path d="M12 9v4M12 17h.01"/></>,
  external: <><path d="M13 4h7v7M20 4l-9 9"/><path d="M20 14v5a1 1 0 0 1-1 1H5a1 1 0 0 1-1-1V5a1 1 0 0 1 1-1h6"/></>,
  menu: <path d="M4 7h16M4 12h16M4 17h16"/>,
  clock: <><circle cx="12" cy="12" r="9"/><path d="M12 7v5l4 2"/></>,
  filter: <><path d="M3 5h18l-7 8v6l-4 2v-8z"/></>,
  eye: <><path d="M2 12s4-7 10-7 10 7 10 7-4 7-10 7S2 12 2 12Z"/><circle cx="12" cy="12" r="3"/></>,
  play: <path d="m7 4 13 8-13 8z"/>,
  code: <path d="m8 7-5 5 5 5M16 7l5 5-5 5M14 4l-4 16"/>,
  spark: <><path d="m12 2 2.1 7.9L22 12l-7.9 2.1L12 22l-2.1-7.9L2 12l7.9-2.1z"/></>,
  database: <><ellipse cx="12" cy="5" rx="9" ry="3"/><path d="M3 5v14c0 4 18 4 18 0V5M3 12c0 4 18 4 18 0"/></>,
  server: <><rect x="3" y="3" width="18" height="8" rx="2"/><rect x="3" y="13" width="18" height="8" rx="2"/><path d="M7 7h.01M7 17h.01M11 7h7M11 17h7"/></>,
  moon: <path d="M20 16A9 9 0 0 1 8 4a9 9 0 1 0 12 12Z"/>,
  settings: <><circle cx="12" cy="12" r="3"/><path d="M10 2h4l1 3 3 1 3-1 2 4-2 2v3l2 2-2 4-3-1-3 1-1 3h-4l-1-3-3-1-3 1-2-4 2-2v-3L1 9l2-4 3 1 3-1z"/></>,
  checkCircle: <><circle cx="12" cy="12" r="9"/><path d="m8 12 3 3 5-6"/></>,
  info: <><circle cx="12" cy="12" r="9"/><path d="M12 11v6M12 7h.01"/></>,
}

export default function Icon({ name, size = 20, className = '' }: { name: string; size?: number; className?: string }) {
  return <svg className={className} width={size} height={size} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.75" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">{d[name as IconName] || d.grid}</svg>
}
