import { useEffect, useMemo, useState, type FormEvent } from 'react'
import Icon from './components/Icon'
import OperationModal from './components/OperationModal'
import { apiRequest, getRows, getSwagger, shortValue } from './lib/http'
import { actionLabel, groupOperations, humanize, isSimpleList, operationsFrom, tagInfo, type ApiDocument, type ApiOperation } from './lib/openapi'
import './App.css'

type LoginResponse = { accessToken: string; tokenType?: string; expiresIn?: number }
type View = 'overview' | string

function App() {
  const [token, setToken] = useState<string | null>(null)
  const [email, setEmail] = useState('')
  const [spec, setSpec] = useState<ApiDocument | null>(null)
  const [specError, setSpecError] = useState('')
  const [specLoading, setSpecLoading] = useState(true)
  const [view, setView] = useState<View>('overview')
  const [sideOpen, setSideOpen] = useState(false)
  const [selectedOp, setSelectedOp] = useState<ApiOperation | null>(null)
  const [selectedRow, setSelectedRow] = useState<Record<string, unknown> | undefined>()
  const [refreshKey, setRefreshKey] = useState(0)
  const [notification, setNotification] = useState('')
  const [retryKey, setRetryKey] = useState(0)

  useEffect(() => {
    const abort = new AbortController()
    setSpecLoading(true)
    getSwagger(abort.signal).then(doc => {
      if (!abort.signal.aborted) { setSpec(doc); setSpecError(''); setSpecLoading(false) }
    }).catch(e => {
      if (!abort.signal.aborted) { setSpecError(e instanceof Error ? e.message : 'No se pudo leer Swagger.'); setSpecLoading(false) }
    })
    return () => abort.abort()
  }, [retryKey])

  const ops = useMemo(() => spec ? operationsFrom(spec) : [], [spec])
  const groups = useMemo(() => groupOperations(ops), [ops])
  const visibleTags = useMemo(() => Object.keys(groups).filter(t => t.toLowerCase() !== 'auth').sort((a,b) => tagInfo(a).label.localeCompare(tagInfo(b).label, 'es')), [groups])
  const navigate = (page: View) => { setView(page); setSideOpen(false) }
  const openOperation = (op: ApiOperation, row?: Record<string, unknown>) => { setSelectedRow(row); setSelectedOp(op) }
  const onSaved = () => { setRefreshKey(k => k + 1); setNotification('Operación completada en la API.'); window.setTimeout(() => setNotification(''), 4500) }
  const logout = () => { setToken(null); setEmail(''); setView('overview'); setSelectedOp(null); setSideOpen(false) }

  return <>
    {!token ? <Login onLogin={(access, user) => { setToken(access); setEmail(user) }} apiReady={!!spec} apiError={specError} onRetry={() => setRetryKey(n => n + 1)} /> :
      <div className="app-shell">
        {sideOpen && <div className="mobile-scrim" onClick={() => setSideOpen(false)}/>}
        <aside className={`sidebar ${sideOpen ? 'sidebar-open' : ''}`}>
          <div className="brand"><div className="brand-mark"><Icon name="paw" size={26}/></div><div><strong>VetCare<span className="brand-dot">.</span></strong><small>GESTIÓN VETERINARIA</small></div></div>
          <div className="sidebar-body">
            <div className="nav-caption">ESPACIO DE TRABAJO</div>
            <button className={`nav-item ${view === 'overview' ? 'nav-active' : ''}`} onClick={() => navigate('overview')}><Icon name="home" size={19}/><span>Panel general</span><Icon name="chevron" size={15} className="nav-arrow"/></button>
            <div className="nav-caption nav-caption-modules">MÓDULOS DEL SISTEMA <span>{visibleTags.length}</span></div>
            {visibleTags.map(tag => <button className={`nav-item ${view === tag ? 'nav-active' : ''}`} key={tag} onClick={() => navigate(tag)}><Icon name={tagInfo(tag).icon} size={19}/><span>{tagInfo(tag).label}</span><Icon name="chevron" size={15} className="nav-arrow"/></button>)}
            {!visibleTags.length && <div className="nav-empty">Esperando documentación de la API…</div>}
          </div>
          <div className="sidebar-bottom"><div className="sidebar-help"><Icon name="spark" size={21}/><strong>Todo en un solo lugar</strong><span>Gestión conectada con tu API real.</span><a href="http://localhost:5265/swagger" target="_blank" rel="noreferrer">Abrir Swagger <Icon name="external" size={14}/></a></div>
            <button className="logout" onClick={logout}><Icon name="logout" size={19}/> Cerrar sesión</button>
          </div>
        </aside>
        <div className="main-column">
          <header className="topbar"><button className="icon-button mobile-menu" aria-label="Abrir menú" onClick={() => setSideOpen(s => !s)}><Icon name="menu"/></button><div className="breadcrumb"><span>VetCare</span><Icon name="chevron" size={15}/><strong>{view === 'overview' ? 'Panel general' : tagInfo(view).label}</strong></div>
            <div className="topbar-right"><span className={`api-pill ${spec ? 'api-pill-ok' : 'api-pill-off'}`}><i/>{spec ? 'API conectada' : specLoading ? 'Verificando API' : 'Sin Swagger'}</span><span className="topbar-separator"/><div className="profile-avatar">{email.charAt(0).toUpperCase() || 'V'}</div><div className="profile-details"><strong>Administrador</strong><small title={email}>{email}</small></div></div>
          </header>
          <main className="content-area">
            {specError && <div className="api-warning"><Icon name="alert" size={21}/><div><strong>No se pudo leer el catálogo de la API</strong><span>{specError}. El backend debe estar ejecutándose en localhost:5265.</span></div><button className="btn btn-secondary" onClick={() => setRetryKey(n => n + 1)}><Icon name="refresh" size={16}/> Reintentar</button></div>}
            {view === 'overview' ? <Dashboard tags={visibleTags} ops={ops} loading={specLoading} select={navigate} /> : groups[view] ? <ModulePage key={view} tag={view} ops={groups[view]} token={token} onOpen={openOperation} refreshKey={refreshKey}/> : <div className="empty-panel"><Icon name="grid" size={38}/><h3>Módulo no disponible</h3><p>Actualiza la API para comprobar sus módulos disponibles.</p><button className="btn btn-secondary" onClick={() => navigate('overview')}>Volver al inicio</button></div>}
            <footer className="main-footer"><span>© {new Date().getFullYear()} VetCare · Sistema de gestión veterinaria</span><span>Frontend React · API .NET · PostgreSQL</span></footer>
          </main>
        </div>
      </div>}
    {selectedOp && spec && token && <OperationModal op={selectedOp} doc={spec} token={token} initialRow={selectedRow} onClose={() => { setSelectedOp(null); setSelectedRow(undefined) }} onSuccess={onSaved}/>}
    {notification && <div className="toast"><Icon name="checkCircle" size={20}/>{notification}<button aria-label="Cerrar aviso" onClick={() => setNotification('')}><Icon name="x" size={14}/></button></div>}
  </>
}

function Login({ onLogin, apiReady, apiError, onRetry }: { onLogin: (access: string, email: string) => void; apiReady: boolean; apiError: string; onRetry: () => void }) {
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [showPassword, setShowPassword] = useState(false)
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)
  const submit = async (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault(); setError(''); setLoading(true)
    try {
      const result = await apiRequest('/api/auth/login', { method: 'POST', body: { email: email.trim(), password } }) as LoginResponse
      if (!result?.accessToken) throw new Error('La API no devolvió un token válido.')
      setPassword('')
      onLogin(result.accessToken, email.trim())
    } catch (err) { setError(err instanceof Error ? err.message : 'No se pudo iniciar sesión.') }
    finally { setLoading(false) }
  }
  return <div className="login-shell">
    <section className="login-art"><div className="login-art-top"><div className="brand-mark"><Icon name="paw" size={26}/></div><strong>VetCare<span>.</span></strong></div>
      <div className="art-orbit orbit-one"/><div className="art-orbit orbit-two"/><div className="art-blob blob-one"/><div className="art-blob blob-two"/>
      <div className="art-center"><div className="art-feature"><div className="art-feature-icon"><Icon name="heart" size={46}/></div><div className="art-cross">+</div><div className="art-paw"><Icon name="paw" size={29}/></div></div><span className="art-kicker">CUIDADO QUE CONECTA</span><h2>Una mejor forma<br/>de <em>cuidar</em> cada vida.</h2><p>Pacientes, citas y gestión veterinaria en una experiencia más simple, humana y organizada.</p></div>
      <div className="art-bottom"><span>Software de gestión veterinaria</span><div className="art-dots"><i/><i/><i/></div></div>
    </section>
    <section className="login-form-area"><div className="login-mobile-brand"><div className="brand-mark"><Icon name="paw" size={22}/></div><strong>VetCare.</strong></div><div className="login-form-wrap"><div className="login-overline"><span className="tiny-line"/> BIENVENIDO A VETCARE</div><h1>Qué gusto tenerte<br/><span>de vuelta.</span></h1><p className="login-subtitle">Accede a tu espacio de trabajo para seguir cuidando lo que más importa.</p>
      <form className="login-form" onSubmit={submit}><label htmlFor="login-email">Correo electrónico</label><div className="input-box"><Icon name="users" size={19}/><input id="login-email" type="email" required autoComplete="username" placeholder="nombre@veterinaria.com" value={email} onChange={e => setEmail(e.target.value)}/></div>
        <div className="login-password-heading"><label htmlFor="login-password">Contraseña</label></div><div className="input-box"><Icon name="shield" size={19}/><input id="login-password" type={showPassword ? 'text' : 'password'} required autoComplete="current-password" placeholder="Ingresa tu contraseña" value={password} onChange={e => setPassword(e.target.value)}/><button type="button" aria-label={showPassword ? 'Ocultar contraseña' : 'Mostrar contraseña'} onClick={() => setShowPassword(v => !v)}><Icon name="eye" size={18}/></button></div>
        {error && <div className="login-error" role="alert"><Icon name="alert" size={18}/>{error}</div>}
        <button className="btn-login" type="submit" disabled={loading}>{loading ? 'Validando credenciales...' : <>Entrar a VetCare <Icon name="arrow" size={19}/></>}</button>
      </form>
      <div className={`login-connection ${apiReady ? 'connection-ok' : ''}`}><span className="connection-point"/><span>{apiReady ? 'API de VetCare disponible' : apiError ? 'No se pudo leer Swagger. Verifica el backend.' : 'Comprobando conexión con la API...'}</span>{apiError && <button onClick={onRetry} type="button">Reintentar</button>}</div>
    </div><p className="login-credit">© {new Date().getFullYear()} VetCare · Desarrollado para gestionar con cariño.</p></section>
  </div>
}

function Dashboard({ tags, ops, loading, select }: { tags: string[]; ops: ApiOperation[]; loading: boolean; select: (v: string) => void }) {
  const getCount = ops.filter(op => op.method === 'GET').length
  const editCount = ops.length - getCount
  const cards = [
    { title: 'Módulos disponibles', val: tags.length, icon: 'grid', hint: 'Detectados en Swagger' },
    { title: 'Consultas disponibles', val: getCount, icon: 'search', hint: 'Operaciones de lectura' },
    { title: 'Acciones disponibles', val: editCount, icon: 'activity', hint: 'Operaciones de gestión' },
    { title: 'Conexión', val: loading ? '...' : ops.length ? 'Activa' : 'Revisar', icon: 'server', hint: 'API local .NET' },
  ]
  return <>
    <div className="page-topline"><div><div className="eyebrow">TU ESPACIO DE TRABAJO</div><h1>Panel general <span className="wave">✳</span></h1><p>Todo lo que necesitas para cuidar y administrar, en un solo lugar.</p></div><span className="today-pill"><Icon name="calendar" size={16}/>{new Intl.DateTimeFormat('es-PE',{dateStyle:'long'}).format(new Date())}</span></div>
    <section className="hero-banner"><div className="hero-content"><span className="hero-chip"><Icon name="spark" size={14}/> TU CLÍNICA, MÁS ORGANIZADA</span><h2>Cuidamos de ellos.<br/><em>Nosotros te ayudamos.</em></h2><p>Gestiona tu veterinaria con la tranquilidad de tener toda la información conectada al mismo sistema.</p><button className="hero-cta" onClick={() => tags.length && select(tags[0])} disabled={!tags.length}>Explorar módulos <Icon name="arrow" size={18}/></button></div><div className="hero-graphic"><div className="hero-circle hero-circle-a"/><div className="hero-circle hero-circle-b"/><div className="hero-paw"><Icon name="paw" size={105}/></div><span className="hero-plus plus-1">+</span><span className="hero-plus plus-2">+</span><div className="hero-small-card"><Icon name="heart" size={21}/><span>Conecta. Cuida. Crece.</span></div></div></section>
    <section className="stat-grid">{cards.map((card, index) => <article className="stat-card" key={card.title}><div className="stat-top"><span className={`stat-icon stat-${index}`}><Icon name={card.icon} size={22}/></span><Icon name="arrow" className="stat-direction" size={17}/></div><strong>{card.val}</strong><span className="stat-title">{card.title}</span><small>{card.hint}</small></article>)}</section>
    <div className="section-title"><div><h2>Accesos rápidos</h2><p>Abre los módulos publicados por el backend.</p></div><span className="mini-tag">{tags.length} módulos</span></div>
    {tags.length ? <div className="modules-grid">{tags.map((tag, i) => <button className="module-tile" onClick={() => select(tag)} key={tag}><span className={`module-symbol symbol-${i%5}`}><Icon name={tagInfo(tag).icon} size={25}/></span><span><strong>{tagInfo(tag).label}</strong><small>{ops.filter(op => op.tag === tag).length} operaciones de API</small></span><Icon name="arrow" size={18} className="tile-arrow"/></button>)}</div> : <div className="empty-panel"><Icon name="server" size={32}/><h3>Esperando módulos de la API</h3><p>Comprueba que tu backend esté iniciado y disponible.</p></div>}
    <div className="how-it-works"><div className="how-icon"><Icon name="info" size={22}/></div><div><strong>Datos reales, sin información de ejemplo.</strong><p>Los módulos, operaciones y formularios se obtienen de Swagger. La información de los registros aparece cuando consultas cada módulo.</p></div></div>
  </>
}

function ModulePage({ tag, ops, token, onOpen, refreshKey }: { tag: string; ops: ApiOperation[]; token: string; onOpen: (op: ApiOperation, row?: Record<string, unknown>) => void; refreshKey: number }) {
  const listOps = useMemo(() => ops.filter(isSimpleList), [ops])
  const [selectedList, setSelectedList] = useState(listOps[0]?.id || '')
  const [tab, setTab] = useState<'records'|'operations'>(listOps.length ? 'records' : 'operations')
  const [rows, setRows] = useState<Record<string, unknown>[]>([])
  const [rawResult, setRawResult] = useState<unknown>(null)
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)
  const [search, setSearch] = useState('')
  const [page, setPage] = useState(0)
  const [detail, setDetail] = useState<Record<string, unknown> | null>(null)
  const [counter, setCounter] = useState(0)
  const [filter, setFilter] = useState('all')
  const activeList = listOps.find(op => op.id === selectedList) || listOps[0]

  useEffect(() => {
    if (!activeList || tab !== 'records') return
    const controller = new AbortController()
    setLoading(true); setError(''); setPage(0)
    apiRequest(activeList.path, { token, signal: controller.signal }).then(data => {
      if (!controller.signal.aborted) { setRawResult(data); setRows(getRows(data)); setLoading(false) }
    }).catch(e => { if (!controller.signal.aborted) { setRows([]); setRawResult(null); setError(e instanceof Error ? e.message : 'Error de consulta.'); setLoading(false) } })
    return () => controller.abort()
  }, [activeList?.id, activeList?.path, tab, token, refreshKey, counter])

  const displayRows = useMemo(() => rows.filter(row => Object.values(row).some(value => { try { return JSON.stringify(value).toLowerCase().includes(search.toLowerCase()) } catch { return false } })), [rows, search])
  const pageSize = 9
  const maxPage = Math.max(0, Math.ceil(displayRows.length / pageSize) - 1)
  const columns = useMemo(() => {
    const all = [...new Set(rows.slice(0, 10).flatMap(row => Object.keys(row)))]
    const keys = all.filter(x => !Array.isArray(rows[0]?.[x]) && typeof rows[0]?.[x] !== 'object')
    const prioritized = ['id', 'name', 'nombre', 'fullName', 'email', 'status', 'createdAt', 'startAt', 'total', 'price']
    return [...keys].sort((a,b) => prioritized.indexOf(a) === -1 ? (prioritized.indexOf(b) === -1 ? 0 : 1) : prioritized.indexOf(b) === -1 ? -1 : prioritized.indexOf(a)-prioritized.indexOf(b)).slice(0, 6)
  }, [rows])
  const filteredOps = filter === 'all' ? ops : ops.filter(op => op.method === filter)
  const info = tagInfo(tag)

  return <>
    <div className="page-topline module-heading"><div><div className="eyebrow">MÓDULOS / {tag.toUpperCase()}</div><div className="module-title-row"><span className="title-icon"><Icon name={info.icon} size={29}/></span><h1>{info.label}</h1></div><p>Consulta datos y ejecuta operaciones disponibles en tu API.</p></div><button className="btn btn-secondary" onClick={() => setCounter(c => c+1)} disabled={!activeList || loading}><Icon name="refresh" size={17}/> Actualizar</button></div>
    <div className="module-meta"><span><Icon name="server" size={16}/> {ops.length} endpoints publicados</span><span><Icon name="shield" size={16}/> Acceso protegido por la API</span><span><Icon name="database" size={16}/> Información del servidor</span></div>
    <div className="tabs"><button className={tab === 'records' ? 'tab-active' : ''} onClick={() => setTab('records')} disabled={!listOps.length}><Icon name="grid" size={17}/> Registros</button><button className={tab === 'operations' ? 'tab-active' : ''} onClick={() => setTab('operations')}><Icon name="code" size={17}/> Operaciones <span>{ops.length}</span></button></div>
    {tab === 'records' ? <section className="data-card"><div className="data-head"><div><h2>Listado de {info.label.toLowerCase()}</h2><p>Datos consultados directamente desde el backend.</p></div><div className="data-tools">{listOps.length > 1 && <select className="endpoint-select" aria-label="Elegir consulta" value={activeList?.id} onChange={e => { setSelectedList(e.target.value); setSearch(''); setPage(0) }}>{listOps.map(op => <option value={op.id} key={op.id}>{op.path}</option>)}</select>}<div className="search-box"><Icon name="search" size={18}/><input value={search} onChange={e => { setSearch(e.target.value); setPage(0) }} placeholder="Buscar en registros..." aria-label="Buscar registros"/></div></div></div>
      {activeList && <div className="endpoint-note"><span className="method method-get">GET</span><code>{activeList.path}</code><span className="live-dot"><i/> Datos en vivo</span></div>}
      {loading ? <div className="table-wait"><div className="spinner"/><strong>Consultando la API...</strong><span>Estamos solicitando los registros al servidor.</span></div> : error ? <div className="table-wait table-fail"><Icon name="alert" size={32}/><strong>No se pudieron consultar los datos</strong><p>{error}</p><button className="btn btn-secondary" onClick={() => setCounter(c => c+1)}><Icon name="refresh" size={17}/> Volver a intentar</button></div> : rows.length === 0 ? <div className="table-wait"><Icon name="clipboard" size={33}/><strong>Aún no hay registros para mostrar</strong><span>La API respondió correctamente, pero esta consulta no devolvió elementos.</span></div> : <>
        <div className="table-scroll"><table className="data-table"><thead><tr>{columns.map(key => <th key={key}>{humanize(key)}</th>)}<th className="actions-header">Detalle</th></tr></thead><tbody>{displayRows.slice(page * pageSize, (page + 1) * pageSize).map((row, index) => <tr key={`${page}-${index}`}><>{columns.map(key => <td key={key} title={typeof row[key] === 'string' ? String(row[key]) : undefined}>{key.toLowerCase().includes('status') || key.toLowerCase().includes('estado') ? <span className="status-cell"><i/>{shortValue(row[key])}</span> : shortValue(row[key])}</td>)}</><td><button className="table-detail-btn" onClick={() => setDetail(row)} title="Ver todos los campos" aria-label="Ver detalle"><Icon name="eye" size={18}/></button></td></tr>)}</tbody></table></div>
        <div className="table-footer"><span>{displayRows.length} registros mostrados{typeof rawResult === 'object' && rawResult !== null && !Array.isArray(rawResult) && 'totalCount' in rawResult ? ` · Total reportado: ${String((rawResult as Record<string, unknown>).totalCount)}` : ''}</span><div><button disabled={page === 0} onClick={() => setPage(p => p-1)}>Anterior</button><span>{Math.min(page+1, maxPage+1)} / {maxPage+1}</span><button disabled={page >= maxPage} onClick={() => setPage(p => p+1)}>Siguiente</button></div></div>
      </>}
      {ops.some(op => op.method === 'POST') && <div className="new-record-footer"><div><strong>¿Necesitas registrar información?</strong><span>Abre la operación del servidor correspondiente.</span></div><button className="btn btn-primary" onClick={() => { setTab('operations'); setFilter('POST') }}><Icon name="plus" size={17}/> Crear / registrar</button></div>}
    </section> : <section className="operations-section"><div className="operations-heading"><div><h2>Operaciones disponibles</h2><p>Acciones obtenidas de Swagger. El backend valida permisos y datos.</p></div><div className="method-filters">{['all','GET','POST','PUT','PATCH','DELETE'].filter(method => method === 'all' || ops.some(o => o.method === method)).map(method => <button className={filter === method ? 'filter-active' : ''} onClick={() => setFilter(method)} key={method}>{method === 'all' ? 'Todas' : method}</button>)}</div></div><div className="operation-grid">{filteredOps.map(op => <button className="operation-card" key={op.id} onClick={() => onOpen(op)}><div className="operation-icon"><Icon name={op.method === 'GET' ? 'search' : op.method === 'DELETE' ? 'alert' : op.method === 'POST' ? 'plus' : 'clipboard'} size={21}/></div><span className="operation-content"><strong>{actionLabel(op)}</strong><span><span className={`method method-${op.method.toLowerCase()}`}>{op.method}</span><code>{op.path}</code></span></span><Icon name="arrow" size={18} className="operation-arrow"/></button>)}</div><div className="how-it-works slim"><Icon name="shield" size={21}/><div><strong>Solo se ejecutan cuando tú lo indicas.</strong><p>Las acciones POST, PUT, PATCH y DELETE nunca se envían automáticamente.</p></div></div></section>}
    {detail && <div className="modal-backdrop" onMouseDown={e => { if (e.target === e.currentTarget) setDetail(null) }}><section className="detail-modal" role="dialog" aria-modal="true" aria-label="Detalle de registro"><header className="modal-head"><div><div className="eyebrow">INFORMACIÓN DEL BACKEND</div><h2>Detalle del registro</h2><p>Todos los datos entregados por la API.</p></div><button className="icon-button" onClick={() => setDetail(null)} aria-label="Cerrar"><Icon name="x"/></button></header><div className="detail-fields">{Object.entries(detail).map(([key,val]) => <div key={key}><span>{humanize(key)}</span><strong>{val !== null && typeof val === 'object' ? JSON.stringify(val, null, 2) : shortValue(val)}</strong></div>)}</div><div className="detail-actions"><button className="btn btn-ghost" onClick={() => setDetail(null)}>Cerrar</button>{(tag.toLowerCase() === 'pets' ? ops.filter(op => ['PUT','PATCH'].includes(op.method) && op.path.includes('{id}')) : ops.filter(op => op.method !== 'GET' && op.parameters.some(p => p.in === 'path'))) .map(action => <button key={action.id} className={action.method === 'PATCH' ? 'btn btn-secondary' : 'btn btn-primary'} onClick={() => { const row = detail; setDetail(null); onOpen(action, row) }}>{tag.toLowerCase() === 'pets' ? (action.method === 'PUT' ? 'Editar mascota' : 'Desactivar mascota') : actionLabel(action)} <Icon name="arrow" size={16}/></button>)}</div></section></div>}
  </>
}

export default App
