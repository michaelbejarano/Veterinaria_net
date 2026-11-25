export type JsonSchema = {
  $ref?: string
  type?: string
  format?: string
  title?: string
  description?: string
  nullable?: boolean
  required?: string[]
  properties?: Record<string, JsonSchema>
  items?: JsonSchema
  enum?: Array<string | number | boolean>
  default?: unknown
  example?: unknown
  allOf?: JsonSchema[]
  oneOf?: JsonSchema[]
  anyOf?: JsonSchema[]
  additionalProperties?: boolean | JsonSchema
  minLength?: number
  maxLength?: number
  minimum?: number
  maximum?: number
  readOnly?: boolean
}

export type Parameter = {
  name: string
  in: 'path' | 'query' | 'header' | string
  required?: boolean
  description?: string
  schema?: JsonSchema
}

export type RawOperation = {
  tags?: string[]
  summary?: string
  description?: string
  operationId?: string
  parameters?: Parameter[]
  requestBody?: {
    required?: boolean
    content?: Record<string, { schema?: JsonSchema }>
  }
  responses?: Record<string, { content?: Record<string, { schema?: JsonSchema }> }>
  security?: unknown
  deprecated?: boolean
}

export type ApiDocument = {
  openapi?: string
  info?: { title?: string; version?: string }
  tags?: Array<{ name: string; description?: string }>
  paths?: Record<string, Record<string, RawOperation | Parameter[] | unknown>>
  components?: { schemas?: Record<string, JsonSchema> }
}

export type ApiOperation = RawOperation & {
  id: string
  method: string
  path: string
  tag: string
  parameters: Parameter[]
}

const METHODS = ['get', 'post', 'put', 'patch', 'delete']

export function operationsFrom(doc: ApiDocument): ApiOperation[] {
  const result: ApiOperation[] = []
  for (const [path, route] of Object.entries(doc.paths || {})) {
    const shared = (route.parameters || []) as Parameter[]
    for (const method of METHODS) {
      const source = route[method] as RawOperation | undefined
      if (!source || typeof source !== 'object') continue
      result.push({
        ...source,
        id: `${method}:${path}`,
        method: method.toUpperCase(),
        path,
        tag: source.tags?.[0] || path.split('/')[2] || 'General',
        parameters: [...shared, ...(source.parameters || [])],
      })
    }
  }
  return result
}

export function resolveSchema(schema: JsonSchema | undefined, doc: ApiDocument, depth = 0): JsonSchema {
  if (!schema || depth > 9) return schema || {}
  if (schema.$ref) {
    const id = schema.$ref.split('/').pop() || ''
    const target = doc.components?.schemas?.[id]
    if (!target) return { ...schema, type: 'object' }
    return { ...resolveSchema(target, doc, depth + 1), ...Object.fromEntries(Object.entries(schema).filter(([key]) => key !== '$ref')) }
  }
  const combined = schema.allOf || schema.oneOf || schema.anyOf
  if (combined?.length) {
    let merged: JsonSchema = { ...schema }
    for (const entry of (schema.allOf || combined.slice(0, 1))) {
      const part = resolveSchema(entry, doc, depth + 1)
      merged = {
        ...part,
        ...merged,
        type: merged.type || part.type,
        properties: { ...part.properties, ...merged.properties },
        required: [...new Set([...(part.required || []), ...(merged.required || [])])],
      }
    }
    return merged
  }
  return schema
}

export function requestSchema(op: ApiOperation, doc: ApiDocument): JsonSchema | undefined {
  const media = op.requestBody?.content
  if (!media) return undefined
  const item = media['application/json'] || media['application/*+json'] || Object.values(media)[0]
  return item?.schema ? resolveSchema(item.schema, doc) : undefined
}

export function humanize(s: string): string {
  return s.replace(/([a-z])([A-Z])/g, '$1 $2').replace(/[_-]/g, ' ').replace(/\b\w/g, x => x.toUpperCase()).trim()
}

const NAMES: Record<string, { label: string; singular: string; icon: string }> = {
  Auth: { label: 'Autenticación', singular: 'Acceso', icon: 'shield' },
  Appointments: { label: 'Citas', singular: 'Cita', icon: 'calendar' },
  Pets: { label: 'Mascotas', singular: 'Mascota', icon: 'paw' },
  Pet: { label: 'Mascotas', singular: 'Mascota', icon: 'paw' },
  Customers: { label: 'Clientes', singular: 'Cliente', icon: 'users' },
  Clients: { label: 'Clientes', singular: 'Cliente', icon: 'users' },
  Owners: { label: 'Propietarios', singular: 'Propietario', icon: 'users' },
  Employees: { label: 'Empleados', singular: 'Empleado', icon: 'users' },
  Staff: { label: 'Personal', singular: 'Empleado', icon: 'users' },
  Veterinarians: { label: 'Veterinarios', singular: 'Veterinario', icon: 'heart' },
  Services: { label: 'Servicios', singular: 'Servicio', icon: 'activity' },
  MedicalRecords: { label: 'Historial clínico', singular: 'Registro', icon: 'clipboard' },
  Consultations: { label: 'Consultas', singular: 'Consulta', icon: 'heart' },
  CashRegisters: { label: 'Cajas', singular: 'Caja', icon: 'wallet' },
  Sales: { label: 'Ventas', singular: 'Venta', icon: 'receipt' },
  Purchases: { label: 'Compras', singular: 'Compra', icon: 'package' },
  Products: { label: 'Productos', singular: 'Producto', icon: 'package' },
  Inventory: { label: 'Inventario', singular: 'Artículo', icon: 'package' },
  Suppliers: { label: 'Proveedores', singular: 'Proveedor', icon: 'truck' },
  Users: { label: 'Usuarios', singular: 'Usuario', icon: 'users' },
  Roles: { label: 'Roles', singular: 'Rol', icon: 'shield' },
  Payments: { label: 'Pagos', singular: 'Pago', icon: 'wallet' },
  Invoices: { label: 'Comprobantes', singular: 'Comprobante', icon: 'receipt' },
  Reports: { label: 'Reportes', singular: 'Reporte', icon: 'chart' },
  Expenses: { label: 'Egresos', singular: 'Egreso', icon: 'wallet' },
  Income: { label: 'Ingresos', singular: 'Ingreso', icon: 'wallet' },
}

export function tagInfo(tag: string) {
  const normalized = Object.keys(NAMES).find(key => key.toLowerCase() === tag.toLowerCase())
  return normalized ? NAMES[normalized] : { label: humanize(tag), singular: humanize(tag), icon: 'grid' }
}

export function actionLabel(op: ApiOperation): string {
  const summary = op.summary?.trim()
  if (summary) return summary
  const segment = op.path.split('/').filter(Boolean).pop() || ''
  const hints: Record<string, string> = { GET: 'Consultar', POST: 'Crear / ejecutar', PUT: 'Actualizar', PATCH: 'Modificar', DELETE: 'Eliminar' }
  return `${hints[op.method] || op.method} ${segment.startsWith('{') ? 'por identificador' : humanize(segment)}`
}

export function isSimpleList(op: ApiOperation): boolean {
  return op.method === 'GET' && !op.path.includes('{') && !op.parameters.some(p => p.required && p.in === 'query')
}

export function groupOperations(ops: ApiOperation[]): Record<string, ApiOperation[]> {
  const groups: Record<string, ApiOperation[]> = {}
  for (const op of ops) (groups[op.tag] ||= []).push(op)
  return groups
}
