type NavigationIconName = 'production' | 'orders' | 'history' | 'graphs' | 'diagnostics'

export type NavigationItem<T extends string> = {
  id: T
  label: string
  icon: NavigationIconName
}

type SideMenuProps<T extends string> = {
  currentPage: T
  items: readonly NavigationItem<T>[]
  collapsed: boolean
  online: boolean
  statusLabel: string
  onSelect: (page: T) => void
  onToggle: () => void
}

function NavigationIcon({ name }: { name: NavigationIconName }) {
  if (name === 'production') {
    return <svg viewBox="0 0 24 24" aria-hidden="true"><path d="M4 18V8l8-4 8 4v10"/><path d="M8 18v-5h8v5M3 20h18"/></svg>
  }

  if (name === 'orders') {
    return <svg viewBox="0 0 24 24" aria-hidden="true"><path d="M7 4h10v3H7zM5 6h14v14H5zM8 11h8M8 15h6"/></svg>
  }

  if (name === 'history') {
    return <svg viewBox="0 0 24 24" aria-hidden="true"><path d="M4 12a8 8 0 1 0 2.3-5.7L4 8.6"/><path d="M4 4v4.6h4.6M12 8v5l3 2"/></svg>
  }

  if (name === 'graphs') {
    return <svg viewBox="0 0 24 24" aria-hidden="true"><path d="M4 19V5M4 19h16M7 15l4-4 3 2 5-6"/></svg>
  }

  return <svg viewBox="0 0 24 24" aria-hidden="true"><path d="M12 3v3M12 18v3M3 12h3M18 12h3M5.6 5.6l2.1 2.1M16.3 16.3l2.1 2.1M18.4 5.6l-2.1 2.1M7.7 16.3l-2.1 2.1"/><circle cx="12" cy="12" r="4"/></svg>
}

export default function SideMenu<T extends string>({
  currentPage,
  items,
  collapsed,
  online,
  statusLabel,
  onSelect,
  onToggle,
}: SideMenuProps<T>) {
  const toggleLabel = collapsed ? 'Expandir menu' : 'Recolher menu'

  return <aside className={`side-menu ${collapsed ? 'collapsed' : ''}`} aria-label="Navegação principal">
    <div className="side-menu-heading">
      <div className="brand">
        <i aria-hidden="true">MR</i>
        <div className="brand-copy"><b>Dry End</b><span>Production HMI</span></div>
      </div>
      <button
        type="button"
        className="side-menu-toggle"
        aria-label={toggleLabel}
        aria-expanded={!collapsed}
        title={toggleLabel}
        onClick={onToggle}
      >
        <svg viewBox="0 0 24 24" aria-hidden="true"><path d="m14 7-5 5 5 5"/></svg>
      </button>
    </div>

    <nav aria-label="Telas da aplicação">
      {items.map(item => <button
        type="button"
        key={item.id}
        className={currentPage === item.id ? 'active' : ''}
        aria-current={currentPage === item.id ? 'page' : undefined}
        aria-label={item.label}
        title={collapsed ? item.label : undefined}
        data-tooltip={collapsed ? item.label : undefined}
        onClick={() => onSelect(item.id)}
      >
        <i className="navigation-icon"><NavigationIcon name={item.icon}/></i>
        <span className="menu-label">{item.label}</span>
      </button>)}
    </nav>

    <div className="side-footer" title={`PLC: ${statusLabel}`}>
      <span className={`status ${online ? 'online' : ''}`}><i/><span className="status-label">{statusLabel}</span></span>
      <span className="runtime-label">PLC Runtime 1 · 851</span>
    </div>
  </aside>
}
