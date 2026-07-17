import { useEffect, useMemo, useState } from 'react'
import './HistoryScreen.css'

type DatabaseStatus = { configured: boolean; available: boolean; message: string | null }

type HistoryOrder = {
  id: number
  productionSequence: number | null
  productionState: number | null
  machineNotRunningTime: number | null
  startedAt: string | null
  finishedAt: string | null
  paperComposition: string | null
  fluteType: string | null
  paperWidth: number | null
  paper1: string | null
  paper2: string | null
  paper3: string | null
  paper4: string | null
  paper5: string | null
  productionListNumber: string | null
  levelSelector: number | null
  order1Id: number | null
  order1Product: string | null
  order1Client: string | null
  order1SheetQuantity: number | null
  order1SheetType: number | null
  order1M1: number | null
  order1M2: number | null
  order1M3: number | null
  order1M4: number | null
  order1M5: number | null
  order1SheetLength: number | null
  order1NumberOfCuts: number | null
  order1NumberOfCutsProduced: number | null
  order1PileQuantity: number | null
  order2Id: number | null
  order2Product: string | null
  order2Client: string | null
  order2SheetQuantity: number | null
  order2SheetType: number | null
  order2M1: number | null
  order2M2: number | null
  order2M3: number | null
  order2M4: number | null
  order2M5: number | null
  order2SheetLength: number | null
  order2NumberOfCuts: number | null
  order2NumberOfCutsProduced: number | null
  order2PileQuantity: number | null
}

type OrderPrefix = 'order1' | 'order2'

const levelNames: Record<number, string> = { 1: 'Somente pedido 1', 2: 'Somente pedido 2', 3: 'Pedidos 1 e 2' }
const sheetTypeNames: Record<number, string> = { 0: 'Sem vincos', 1: 'Dois vincos', 2: 'Quatro vincos' }
const productionStateNames: Record<number, string> = { 0: 'Programado', 1: 'Em produção', 4: 'Finalizado' }
const formatNumber = (value: number) => value.toLocaleString('pt-BR')
const formatDate = (value: string | null) => value ? new Date(value).toLocaleString('pt-BR') : '—'
const today = () => new Date().toISOString().slice(0, 10)
const measureCount = (sheetType: number) => sheetType >= 2 ? 5 : sheetType >= 1 ? 3 : 1

function DatabaseBanner({ status }: { status: DatabaseStatus | null }) {
  if (!status) return <div className="db-banner">Verificando banco de dados…</div>
  return <div className={`db-banner ${status.available ? 'ok' : ''}`}><b>{status.available ? 'Banco conectado' : 'Banco indisponível'}</b><span>{status.available ? 'Consulta de histórico habilitada' : status.message}</span></div>
}

function isOrderEnabled(row: HistoryOrder, prefix: OrderPrefix) {
  return prefix === 'order1' ? row.levelSelector !== 2 : row.levelSelector !== 1
}

function orderValue(row: HistoryOrder, prefix: OrderPrefix, suffix: string) {
  return row[`${prefix}${suffix}` as keyof HistoryOrder]
}

function OrderDetails({ row, prefix, title }: { row: HistoryOrder; prefix: OrderPrefix; title: string }) {
  if (!isOrderEnabled(row, prefix)) return <article className="history-order-card inactive"><div className="history-order-title"><div><span>{title}</span><b>Não utilizado neste registro</b></div></div></article>

  const sheetType = Number(orderValue(row, prefix, 'SheetType') ?? 0)
  const totalCuts = Number(orderValue(row, prefix, 'NumberOfCuts') ?? 0)
  const producedCuts = Number(orderValue(row, prefix, 'NumberOfCutsProduced') ?? 0)
  const measures = Array.from({ length: measureCount(sheetType) }, (_, index) => Number(orderValue(row, prefix, `M${index + 1}`) ?? 0))

  return <article className="history-order-card">
    <div className="history-order-title"><div><span>{title}</span><b>{String(orderValue(row, prefix, 'Product') ?? '') || 'Produto não informado'}</b><small>{String(orderValue(row, prefix, 'Client') ?? '') || 'Cliente não informado'}</small></div><strong>OF {orderValue(row, prefix, 'Id') ?? '—'}</strong></div>
    <div className="history-facts">
      <span>Tipo da chapa<b>{sheetTypeNames[sheetType] ?? `Tipo ${sheetType}`}</b></span>
      <span>Chapas por corte<b>{formatNumber(Number(orderValue(row, prefix, 'SheetQuantity') ?? 0))}</b></span>
      <span>Comprimento<b>{formatNumber(Number(orderValue(row, prefix, 'SheetLength') ?? 0))} mm</b></span>
      <span>Pilha programada<b>{formatNumber(Number(orderValue(row, prefix, 'PileQuantity') ?? 0))} chapas</b></span>
    </div>
    <div className="history-measures">{measures.map((measure, index) => <span key={index}>M{index + 1}<b>{formatNumber(measure)} mm</b></span>)}</div>
    <div className="history-cut-summary"><span>Cortes produzidos</span><b>{formatNumber(producedCuts)} / {formatNumber(totalCuts)}</b><small>{formatNumber(Math.max(0, totalCuts - producedCuts))} restantes</small></div>
  </article>
}

function HistoryRow({ row }: { row: HistoryOrder }) {
  const [expanded, setExpanded] = useState(false)
  const papers = [row.paper1, row.paper2, row.paper3, row.paper4, row.paper5]
  const state = row.productionState ?? 0
  const clients = [isOrderEnabled(row, 'order1') ? row.order1Client : null, isOrderEnabled(row, 'order2') ? row.order2Client : null].filter(Boolean).join(' / ')

  return <article className={`history-record ${expanded ? 'expanded' : ''}`}>
    <button className="history-record-summary" type="button" onClick={() => setExpanded(value => !value)} aria-expanded={expanded}>
      <span><b>{row.productionListNumber || 'Sem lista'}</b><small>ID {row.id} · Seq. {row.productionSequence ?? '—'}</small></span>
      <span><b>{formatDate(row.startedAt)}</b><small>Fim: {formatDate(row.finishedAt)}</small></span>
      <span><b>{row.paperWidth ?? 0} mm · Onda {row.fluteType || '—'}</b><small>{row.paperComposition || 'Composição não informada'}</small></span>
      <span><b>{levelNames[row.levelSelector ?? 0] ?? `Seletor ${row.levelSelector ?? '—'}`}</b><small>{clients || 'Clientes não informados'}</small></span>
      <span className={`history-state state-${state}`}><b>{productionStateNames[state] ?? `Estado ${state}`}</b><small>{expanded ? 'Ocultar detalhes' : 'Ver detalhes'}</small></span>
    </button>
    {expanded && <div className="history-record-details">
      <div className="history-paper-details">
        <span>Composição<b>{row.paperComposition || '—'}</b></span><span>Onda<b>{row.fluteType || '—'}</b></span><span>Largura<b>{formatNumber(row.paperWidth ?? 0)} mm</b></span><span>LevelSelector<b>{levelNames[row.levelSelector ?? 0] ?? row.levelSelector ?? '—'}</b></span><span>Tempo de máquina parada<b>{formatNumber(row.machineNotRunningTime ?? 0)}</b></span>
      </div>
      <div className="history-paper-layers">{papers.map((paper, index) => <span key={index}>Papel {index + 1}<b>{paper || '—'}</b></span>)}</div>
      <div className="history-orders"><OrderDetails row={row} prefix="order1" title="Pedido 1"/><OrderDetails row={row} prefix="order2" title="Pedido 2"/></div>
    </div>}
  </article>
}

export default function HistoryScreen() {
  const [status, setStatus] = useState<DatabaseStatus | null>(null)
  const [rows, setRows] = useState<HistoryOrder[]>([])
  const [mode, setMode] = useState('None')
  const [search, setSearch] = useState('')
  const [date, setDate] = useState(today())
  const [error, setError] = useState('')
  const totals = useMemo(() => rows.reduce((result, row) => {
    // EN: Both channels share the cut cycle; use one active channel to avoid double counting.
    // PT: Os dois pedidos compartilham o ciclo de corte; usa um canal ativo para não duplicar.
    const linearMeters = row.levelSelector === 2
      ? (row.order2SheetLength ?? 0) * (row.order2NumberOfCutsProduced ?? 0) / 1000
      : (row.order1SheetLength ?? 0) * (row.order1NumberOfCutsProduced ?? 0) / 1000
    return { linearMeters: result.linearMeters + linearMeters, squareMeters: result.squareMeters + linearMeters * ((row.paperWidth ?? 0) / 1000) }
  }, { linearMeters: 0, squareMeters: 0 }), [rows])

  const load = () => {
    setError('')
    fetch('/api/production-data/status').then(response => response.json()).then(setStatus)
    const params = new URLSearchParams({ mode, search })
    if (mode === 'None') params.set('date', date)
    fetch(`/api/production-data/history?${params}`).then(async response => {
      if (!response.ok) throw new Error((await response.json()).detail)
      return response.json()
    }).then(setRows).catch(reason => setError(reason.message))
  }

  // EN: Load the legacy history default view only when the screen opens.
  // PT: Carrega a visualização padrão do histórico somente ao abrir a tela.
  // oxlint-disable-next-line react-hooks/exhaustive-deps
  useEffect(load, [])

  return <><div className="screen-title"><div><p className="eyebrow">Consulta</p><h2>Histórico de produção</h2></div></div><DatabaseBanner status={status}/>
    <div className="filters"><select value={mode} onChange={event => setMode(event.target.value)}><option value="None">Data</option><option value="Client">Cliente</option><option value="Composition">Composição</option><option value="ProductionList">Lista</option><option value="WorkOrder">OF</option><option value="Product">Produto</option></select>{mode === 'None' ? <input type="date" value={date} onChange={event => setDate(event.target.value)}/> : <input placeholder="Pesquisar" value={search} onChange={event => setSearch(event.target.value)}/>}<button className="primary" onClick={load}>Pesquisar</button></div>
    {error && <div className="form-error">{error}</div>}
    <section className="history-totals"><span>Registros encontrados<b>{formatNumber(rows.length)}</b></span><span>Metros lineares produzidos<b>{formatNumber(Math.round(totals.linearMeters))} m</b></span><span>Área produzida<b>{formatNumber(Math.round(totals.squareMeters))} m²</b></span></section>
    <div className="history-records"><div className="history-record-head"><span>Lista / sequência</span><span>Início / fim</span><span>Papel</span><span>LevelSelector / clientes</span><span>Estado</span></div>{rows.map(row => <HistoryRow row={row} key={row.id}/>)}{rows.length === 0 && <div className="empty-row">Nenhum registro encontrado.</div>}</div>
  </>
}
