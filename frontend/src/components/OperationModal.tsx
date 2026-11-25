import { useEffect, useMemo, useState, type FormEvent } from 'react'
import Icon from './Icon'
import { apiRequest, getRows } from '../lib/http'
import { actionLabel, humanize, requestSchema, resolveSchema, type ApiDocument, type ApiOperation, type JsonSchema } from '../lib/openapi'

type FlatField = { key: string; label: string; schema: JsonSchema; required: boolean; group: 'body' | 'path' | 'query' }
type Choice = { id: string; name: string }

function choicesFrom(data: unknown): Choice[] {
  const unique = new Map<string, string>()
  for (const row of getRows(data)) {
    const key = row.id ?? row.Id ?? row.speciesId ?? row.breedId
    const label = row.name ?? row.Name ?? row.nombre ?? row.speciesName ?? row.breedName ?? row.description
    if (key !== undefined && key !== null) unique.set(String(key), String(label ?? `ID ${key}`))
  }
  return Array.from(unique, ([id, name]) => ({ id, name }))
}

function ownerChoicesFrom(data: unknown): Choice[] {
  const unique = new Map<string, string>()
  for (const row of getRows(data)) {
    const key = row.ownerId ?? row.OwnerId
    const label = row.ownerName ?? row.OwnerName
    if (key !== undefined && key !== null) unique.set(String(key), String(label ?? `Propietario ID ${key}`))
  }
  return Array.from(unique, ([id, name]) => ({ id, name }))
}


function fieldsOf(schema: JsonSchema, doc: ApiDocument, prefix: string, group: FlatField['group'], depth = 0, required = false): FlatField[] {
  const real = resolveSchema(schema, doc)
  if (real.type === 'object' && real.properties && depth < 4) {
    return Object.entries(real.properties)
      .filter(([, value]) => !resolveSchema(value, doc).readOnly)
      .flatMap(([name, value]) => fieldsOf(value, doc, prefix ? `${prefix}.${name}` : name, group, depth + 1, required || Boolean(real.required?.includes(name))))
  }
  return [{ key: prefix, label: humanize(prefix.split('.').pop() || prefix), schema: real, group, required }]
}

const PET_LABELS: Record<string, string> = {
  ownerid: 'Propietario (ID)',
  speciesid: 'Especie',
  breedid: 'Raza',
  name: 'Nombre de la mascota',
  sexo: 'Sexo',
  sex: 'Sexo',
  birthdate: 'Fecha de nacimiento',
  notes: 'Observaciones',
}

function friendlyFieldLabel(field: FlatField, pets: boolean) {
  return pets ? PET_LABELS[field.key.replace(/[^a-z]/gi, '').toLowerCase()] || field.label : field.label
}

function parseValue(value: string, schema: JsonSchema): unknown {
  if (schema.type === 'boolean') return value === 'true'
  if (schema.type === 'integer' || schema.type === 'number') {
    const parsed = Number(value)
    if (!Number.isFinite(parsed)) throw new Error('Introduce un número válido.')
    return parsed
  }
  if (schema.type === 'array' || schema.type === 'object') {
    try { return JSON.parse(value) }
    catch { throw new Error('Un campo JSON no tiene un formato válido.') }
  }
  if (schema.format === 'date-time' && value) {
    const date = new Date(value)
    if (Number.isNaN(date.getTime())) throw new Error('Fecha u hora inválida.')
    return date.toISOString()
  }
  return value
}

function addNested(out: Record<string, unknown>, key: string, value: unknown) {
  const pieces = key.split('.')
  let cursor = out
  for (const part of pieces.slice(0, -1)) {
    if (typeof cursor[part] !== 'object' || cursor[part] === null) cursor[part] = {}
    cursor = cursor[part] as Record<string, unknown>
  }
  cursor[pieces[pieces.length - 1]] = value
}

function fieldType(schema: JsonSchema) {
  if (schema.enum) return 'enum'
  if (schema.type === 'boolean') return 'boolean'
  if (schema.type === 'object' || schema.type === 'array') return 'json'
  if (schema.format === 'date-time') return 'datetime-local'
  if (schema.format === 'date') return 'date'
  if (schema.format === 'email') return 'email'
  if (schema.format === 'password') return 'password'
  if (schema.type === 'integer' || schema.type === 'number') return 'number'
  return 'text'
}

export default function OperationModal({ op, doc, token, onClose, onSuccess, initialRow }: {
  op: ApiOperation
  doc: ApiDocument
  token: string
  onClose: () => void
  onSuccess: () => void
  initialRow?: Record<string, unknown>
}) {
  const [values, setValues] = useState<Record<string, string>>({})
  const [rawBody, setRawBody] = useState('{}')
  const [result, setResult] = useState<unknown>(null)
  const [ran, setRan] = useState(false)
  const [error, setError] = useState('')
  const [pending, setPending] = useState(false)
  const [jsonMode, setJsonMode] = useState(false)
  const [confirmDelete, setConfirmDelete] = useState(false)
  const [species, setSpecies] = useState<Choice[]>([])
  const [breeds, setBreeds] = useState<Choice[]>([])
  const [knownOwners, setKnownOwners] = useState<Choice[]>([])
  const [speciesNotice, setSpeciesNotice] = useState('')
  const [breedsLoading, setBreedsLoading] = useState(false)
  const isPetEditor = op.tag.toLowerCase() === 'pets' && ['POST', 'PUT'].includes(op.method)
  const needsConfirmation = op.method === 'DELETE' || /\/(deactivate|disable|cancel|close)(\b|$)/i.test(op.path)
  const speciesId = values['body.speciesId'] ?? values['body.SpeciesId'] ?? ''

  const schema = useMemo(() => requestSchema(op, doc), [op, doc])
  const params = useMemo(() => op.parameters.filter(p => p.in === 'path' || p.in === 'query'), [op])
  const formFields = useMemo(() => {
    const parameterFields: FlatField[] = params.map(p => ({
      key: `${p.in}.${p.name}`, label: humanize(p.name), group: p.in as 'path'|'query', required: Boolean(p.required), schema: resolveSchema(p.schema || { type: 'string' }, doc),
    }))
    return [...parameterFields, ...(schema ? fieldsOf(schema, doc, '', 'body') : [])]
  }, [params, schema, doc])
  const bodyFields = formFields.filter(f => f.group === 'body')
  const hasBody = Boolean(op.requestBody)

  useEffect(() => {
    if (!isPetEditor) return
    let live = true
    Promise.allSettled([
      apiRequest('/api/species', { token }),
      apiRequest('/api/pets', { token }),
    ]).then(([speciesResult, petsResult]) => {
      if (!live) return
      if (speciesResult.status === 'fulfilled') {
        setSpecies(choicesFrom(speciesResult.value))
        setSpeciesNotice('')
      } else {
        setSpeciesNotice('No se pudo cargar la lista de especies; puedes introducir el ID manualmente.')
      }
      if (petsResult.status === 'fulfilled') setKnownOwners(ownerChoicesFrom(petsResult.value))
    })
    return () => { live = false }
  }, [isPetEditor, token])

  useEffect(() => {
    if (!isPetEditor || !speciesId) { setBreeds([]); return }
    let live = true
    setBreedsLoading(true)
    apiRequest(`/api/species/${encodeURIComponent(speciesId)}/breeds`, { token })
      .then(data => { if (live) setBreeds(choicesFrom(data)) })
      .catch(() => { if (live) setBreeds([]) })
      .finally(() => { if (live) setBreedsLoading(false) })
    return () => { live = false }
  }, [isPetEditor, speciesId, token])


  useEffect(() => {
    const start: Record<string, string> = {}
    for (const p of params.filter(x => x.in === 'path')) {
      const value = initialRow?.[p.name] ?? initialRow?.id ?? initialRow?.Id
      if (value !== null && value !== undefined) start[`path.${p.name}`] = String(value)
    }
    if (initialRow && op.method === 'PUT') {
      for (const field of bodyFields) {
        const key = field.key
        const candidate = initialRow[key] ?? initialRow[key.charAt(0).toUpperCase() + key.slice(1)]
        if (candidate !== undefined && candidate !== null && typeof candidate !== 'object') {
          const text = String(candidate)
          start[`body.${key}`] = field.schema.format === 'date' ? text.slice(0, 10) : text
        }
      }
    }
    setValues(start)
    setError('')
    setResult(null)
    setRan(false)
  }, [op, initialRow, params, bodyFields.length])

  async function submit(event: FormEvent) {
    event.preventDefault()
    setError('')
    setRan(false)
    if (needsConfirmation && !confirmDelete) {
      setError('Confirma que deseas eliminar el registro antes de continuar.')
      return
    }
    setPending(true)
    try {
      let path = op.path
      const query = new URLSearchParams()
      for (const p of params) {
        const raw = values[`${p.in}.${p.name}`]?.trim() || ''
        if (p.required && !raw) throw new Error(`El campo «${humanize(p.name)}» es obligatorio.`)
        if (!raw) continue
        if (p.in === 'path') path = path.replace(`{${p.name}}`, encodeURIComponent(raw))
        if (p.in === 'query') query.set(p.name, raw)
      }
      if (path.includes('{')) throw new Error('Completa todos los identificadores de la ruta.')
      const suffix = query.toString()
      if (suffix) path += `?${suffix}`
      let body: unknown = undefined
      if (hasBody) {
        if (jsonMode || !bodyFields.length) {
          try { body = JSON.parse(rawBody) }
          catch { throw new Error('El contenido JSON no es válido.') }
        } else {
          const built: Record<string, unknown> = {}
          for (const field of bodyFields) {
            const raw = values[`body.${field.key}`] ?? ''
            if (raw === '' && field.required) throw new Error(`El campo «${friendlyFieldLabel(field, isPetEditor)}» es obligatorio.`)
            if (raw === '') continue
            addNested(built, field.key, parseValue(raw, field.schema))
          }
          body = built
        }
      }
      const received = await apiRequest(path, { method: op.method, token, body })
      setResult(received)
      setRan(true)
      if (op.method !== 'GET') onSuccess()
    } catch (e) {
      setError(e instanceof Error ? e.message : 'No se pudo completar la operación.')
    } finally { setPending(false) }
  }

  return (
    <div className="modal-backdrop" onMouseDown={e => { if (e.target === e.currentTarget) onClose() }}>
      <section className="modal" role="dialog" aria-modal="true" aria-label={actionLabel(op)}>
        <header className="modal-head">
          <div><div className="eyebrow">{op.tag.toLowerCase() === 'pets' ? 'GESTION DE MASCOTAS' : 'OPERACION DE LA API'}</div><h2>{op.tag.toLowerCase() === 'pets' && op.method === 'POST' ? 'Registrar mascota' : op.tag.toLowerCase() === 'pets' && op.method === 'PUT' ? 'Editar mascota' : op.tag.toLowerCase() === 'pets' && /deactivate/.test(op.path) ? 'Desactivar mascota' : actionLabel(op)}</h2><p><span className={`method method-${op.method.toLowerCase()}`}>{op.method}</span> <code>{op.path}</code></p></div>
          <button className="icon-button" onClick={onClose} aria-label="Cerrar"><Icon name="x"/></button>
        </header>
        <div className="modal-scroll">
          {op.description && <p className="op-description">{op.description}</p>}
          {isPetEditor && <div className="pet-form-tip"><Icon name="paw" size={19}/> Especies y razas se cargan desde la API. Los propietarios sugeridos son los que ya aparecen asociados a mascotas; si falta uno, introduce su ID.</div>}
          <form id="operation-form" onSubmit={submit}>
            {params.length > 0 && <div className="form-section"><h3>Parámetros</h3><p>Completa los filtros o identificadores que solicita el servidor.</p>
              <div className="field-grid">{formFields.filter(f => f.group !== 'body').map(f => renderField(f))}</div>
            </div>}
            {hasBody && <div className="form-section"><div className="form-section-title"><div><h3>Datos de la solicitud</h3><p>Los campos se obtienen del esquema de Swagger.</p></div><button type="button" className="text-button" onClick={() => setJsonMode(v => !v)}><Icon name="code" size={16}/>{jsonMode ? 'Usar formulario' : 'Editar JSON'}</button></div>
              {(jsonMode || !bodyFields.length) ? <label className="field"><span>Cuerpo JSON</span><textarea className="json-editor" spellCheck={false} value={rawBody} onChange={e => setRawBody(e.target.value)}/></label> : <div className="field-grid">{bodyFields.map(renderField)}</div>}
            </div>}
            {!params.length && !hasBody && <div className="inline-notice"><Icon name="info" size={18}/>Esta operación no necesita parámetros. Presiona el botón inferior para ejecutarla.</div>}
            {needsConfirmation && <label className="confirm-delete"><input type="checkbox" checked={confirmDelete} onChange={e => setConfirmDelete(e.target.checked)}/>Confirmo que quiero ejecutar esta accion sobre el registro indicado.</label>}
            {error && <div className="form-error" role="alert"><Icon name="alert" size={18}/><span>{error}</span></div>}
            {ran && <div className="result-box"><div className="result-head"><Icon name="checkCircle" size={18}/>Operación completada</div><pre>{result == null ? 'La API respondió correctamente, sin contenido.' : JSON.stringify(result, null, 2)}</pre></div>}
          </form>
        </div>
        <footer className="modal-footer"><button className="btn btn-ghost" type="button" onClick={onClose}>Cerrar</button><button form="operation-form" className="btn btn-primary" type="submit" disabled={pending || (needsConfirmation && !confirmDelete)}><Icon name={pending ? 'refresh' : 'play'} size={17}/>{pending ? 'Procesando...' : isPetEditor ? (op.method === 'POST' ? 'Guardar mascota' : 'Actualizar mascota') : /deactivate/.test(op.path) ? 'Desactivar registro' : op.method === 'GET' ? 'Consultar' : 'Ejecutar operación'}</button></footer>
      </section>
    </div>
  )

  function renderField(field: FlatField) {
    const id = `${field.group}.${field.key}`
    const value = values[id] ?? ''
    const kind = fieldType(field.schema)
    const set = (v: string) => setValues(current => ({ ...current, [id]: v }))
    return <label className={`field ${kind === 'json' ? 'field-wide' : ''}`} key={id}>
      <span>{friendlyFieldLabel(field, isPetEditor)}{field.required && <b className="required"> *</b>}</span>
      {isPetEditor && /^speciesId$/i.test(field.key) && species.length ?
        <select value={value} required={field.required} onChange={e => { setValues(current => ({ ...current, [id]: e.target.value, 'body.breedId': '', 'body.BreedId': '' })) }}>
          <option value="">Seleccionar especie...</option>
          {species.map(item => <option key={item.id} value={item.id}>{item.name}</option>)}
        </select> : isPetEditor && /^breedId$/i.test(field.key) && (breeds.length || breedsLoading) ?
        <select value={value} required={field.required} disabled={!speciesId || breedsLoading} onChange={e => set(e.target.value)}>
          <option value="">{breedsLoading ? 'Cargando razas...' : 'Seleccionar raza...'}</option>
          {breeds.map(item => <option key={item.id} value={item.id}>{item.name}</option>)}
        </select> : isPetEditor && /^ownerId$/i.test(field.key) ?
        <>
          <input type="number" min="1" step="1" list="vetcare-owner-choices" required={field.required} placeholder="ID del propietario" value={value} onChange={e => set(e.target.value)}/>
          <datalist id="vetcare-owner-choices">{knownOwners.map(item => <option key={item.id} value={item.id} label={item.name}/>)}</datalist>
          <small>{knownOwners.length ? `Propietarios conocidos: ${knownOwners.map(o => `${o.name} (ID ${o.id})`).join(', ')}` : 'Introduce el ID existente del propietario. Esta API no publica un catalogo de propietarios.'}</small>
        </> : kind === 'enum' || kind === 'boolean' ?
        <select value={value} onChange={e => set(e.target.value)} required={field.required}>
          <option value="">Seleccionar...</option>
          {(kind === 'boolean' ? ['true','false'] : field.schema.enum!.map(String)).map(option => <option key={option} value={option}>{option === 'true' ? 'Sí' : option === 'false' ? 'No' : option}</option>)}
        </select> : kind === 'json' ?
        <textarea rows={3} spellCheck={false} placeholder={field.schema.type === 'array' ? '[]' : '{}'} value={value} required={field.required} onChange={e => set(e.target.value)}/> :
        <input type={kind} value={value} required={field.required} step={field.schema.type === 'integer' ? '1' : field.schema.type === 'number' ? 'any' : undefined} placeholder={field.schema.example != null ? String(field.schema.example) : `Escribir ${friendlyFieldLabel(field, isPetEditor).toLowerCase()}`} onChange={e => set(e.target.value)} />}
      {isPetEditor && /^speciesId$/i.test(field.key) && speciesNotice && <small>{speciesNotice}</small>}
      {field.schema.description && <small>{field.schema.description}</small>}
    </label>
  }
}
