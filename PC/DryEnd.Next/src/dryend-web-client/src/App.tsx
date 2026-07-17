import { HubConnectionBuilder } from '@microsoft/signalr'
import { useEffect, useMemo, useState } from 'react'
import './App.css'

type ToolReference = { index: number; enabled: boolean; positionReferenceMm: number }
type GeneratedOrder = {
  numberOfKnives: number; numberOfScorers: number; numberOfSheets: number
  order1Width: number; order2Width: number; orderTotalWidth: number
  firstKnifePosition: number; lastKnifePosition: number
  knives: ToolReference[]; scorers: ToolReference[]; orderNotOk: boolean
  knivesOutOfRange: number[]; scorersOutOfRange: number[]
}
type OrderChannel = {
  id: number; product: string; client: string; sheetType: number; sheetQuantity: number
  sheetLength: number; sheetMeasures: number[]; numberOfCuts: number
  numberOfCutsProduced: number; numberOfCutsRemaining: number; pileQuantity: number
  pileQuantityProduced: number; pileQuantityRemaining: number; pileCounter: number; scrapCounter: number
}
type OrderSnapshot = {
  startedAt: string; tableId: number; productionListNumber: number; levelSelector: number
  paperComposition: string; fluteType: string; paperWidth: number; paperLayers: string[]
  lineSpeed: number; linearMeters: number; linearMetersProduced: number; linearMetersRemaining: number
  scorerHeightMm: number; plcWatchDog: boolean; aocRequest: boolean; changeOrderRequest: boolean
  saveSqlFinished: boolean; saveSqlTimeout: boolean; invertOrderLevel: boolean; invertOrderSide: boolean
  order1: OrderChannel; order2: OrderChannel; generatedOrder: GeneratedOrder
}
type MonitorSnapshot = {
  state: string; data: { currentOrder: OrderSnapshot; nextOrder: OrderSnapshot; capturedAtUtc: string } | null
  lastSuccessfulReadUtc: string | null; lastError: string | null
}

const initialSnapshot: MonitorSnapshot = { state: 'Offline', data: null, lastSuccessfulReadUtc: null, lastError: null }
const levelNames: Record<number, string> = { 1: 'Superior', 2: 'Inferior', 3: 'Ambos' }
const formatNumber = (value: number, digits = 0) => value.toLocaleString('pt-BR', { maximumFractionDigits: digits })

function Progress({ value, maximum }: { value: number; maximum: number }) {
  const percent = maximum > 0 ? Math.min(100, Math.max(0, value / maximum * 100)) : 0
  return <div className="progress"><i style={{ width: `${percent}%` }} /></div>
}

function ChannelCard({ name, order }: { name: string; order: OrderChannel }) {
  return <article className="channel-card">
    <div className="card-heading"><div><p className="eyebrow">{name}</p><h3>{order.product || 'Sem produto'}</h3></div><span>#{order.id}</span></div>
    <p className="client">{order.client || 'Cliente não informado'}</p>
    <div className="metric-row"><span>Chapas</span><b>{order.sheetQuantity} × {order.sheetLength} mm</b></div>
    <div className="measure-list">{order.sheetMeasures.map((measure, index) => <span key={index}>M{index + 1}<b>{measure} mm</b></span>)}</div>
    <div className="counter-heading"><span>Produção</span><b>{formatNumber(order.numberOfCutsProduced)} / {formatNumber(order.numberOfCuts)}</b></div>
    <Progress value={order.numberOfCutsProduced} maximum={order.numberOfCuts} />
    <div className="pile-grid">
      <span>Pilha atual<b>{order.pileCounter} / {order.pileQuantity}</b></span>
      <span>Pilhas produzidas<b>{order.pileQuantityProduced}</b></span>
      <span>Refugo<b>{order.scrapCounter}</b></span>
    </div>
  </article>
}

function ToolTable({ title, tools, outOfRange }: { title: string; tools: ToolReference[]; outOfRange: number[] }) {
  const enabled = tools.filter(tool => tool.enabled)
  return <article className="tool-card"><div className="card-heading"><h3>{title}</h3><span>{enabled.length} ativas</span></div>
    {enabled.length === 0 ? <p className="empty">Nenhuma ferramenta habilitada</p> : <div className="tool-list">
      {enabled.map(tool => <div className={outOfRange.includes(tool.index) ? 'invalid' : ''} key={tool.index}>
        <span>{title.slice(0, -1)} {tool.index}</span><b>{formatNumber(tool.positionReferenceMm, 2)} mm</b><i>{outOfRange.includes(tool.index) ? 'Fora de faixa' : 'OK'}</i>
      </div>)}
    </div>}
  </article>
}

function OrderDiagnostics({ order }: { order: OrderSnapshot }) {
  const generated = order.generatedOrder
  const checks = useMemo(() => [
    { label: 'Quantidade de facas', ok: generated.knives.filter(tool => tool.enabled).length === generated.numberOfKnives },
    { label: 'Quantidade de vincos', ok: generated.scorers.filter(tool => tool.enabled).length === generated.numberOfScorers },
    { label: 'Largura gerada', ok: Math.abs(generated.order1Width + generated.order2Width - generated.orderTotalWidth) < 0.1 },
    { label: 'Status do pedido', ok: !generated.orderNotOk },
  ], [generated])

  return <>
    <section className="order-banner">
      <div><p className="eyebrow">Lista de produção</p><strong>{order.productionListNumber}</strong><span>Table ID {order.tableId}</span></div>
      <div><span>Papel</span><b>{order.paperWidth} mm · Onda {order.fluteType || '—'}</b><small>{order.paperComposition}</small></div>
      <div><span>Nível</span><b>{levelNames[order.levelSelector] ?? order.levelSelector}</b><small>{order.startedAt || 'Sem horário de início'}</small></div>
      <div><span>Produção linear</span><b>{formatNumber(order.linearMetersProduced, 1)} m</b><small>{formatNumber(order.linearMetersRemaining, 1)} m restantes</small></div>
    </section>
    <section className="channels"><ChannelCard name="Pedido 1 · Superior" order={order.order1} /><ChannelCard name="Pedido 2 · Inferior" order={order.order2} /></section>
    <section className="generated-summary">
      <div><span>Largura pedido 1</span><b>{formatNumber(generated.order1Width, 2)} mm</b></div>
      <div><span>Largura pedido 2</span><b>{formatNumber(generated.order2Width, 2)} mm</b></div>
      <div><span>Largura total</span><b>{formatNumber(generated.orderTotalWidth, 2)} mm</b></div>
      <div><span>Primeira / última faca</span><b>{formatNumber(generated.firstKnifePosition, 2)} / {formatNumber(generated.lastKnifePosition, 2)}</b></div>
    </section>
    <section className="checks">{checks.map(check => <span className={check.ok ? 'valid' : 'invalid'} key={check.label}><i />{check.label}<b>{check.ok ? 'OK' : 'Verificar'}</b></span>)}</section>
    <section className="tools"><ToolTable title="Facas" tools={generated.knives} outOfRange={generated.knivesOutOfRange} /><ToolTable title="Vincos" tools={generated.scorers} outOfRange={generated.scorersOutOfRange} /></section>
  </>
}

function App() {
  const [snapshot, setSnapshot] = useState(initialSnapshot)
  const [selectedOrder, setSelectedOrder] = useState<'current' | 'next'>('current')
  useEffect(() => {
    const controller = new AbortController()
    fetch('/api/diagnostics', { signal: controller.signal }).then(response => response.json()).then(setSnapshot).catch(() => undefined)
    const connection = new HubConnectionBuilder().withUrl('/hubs/diagnostics').withAutomaticReconnect().build()
    connection.on('diagnosticsUpdated', setSnapshot); connection.start().catch(() => undefined)
    return () => { controller.abort(); void connection.stop() }
  }, [])
  const online = snapshot.state === 'Online'
  const order = selectedOrder === 'current' ? snapshot.data?.currentOrder : snapshot.data?.nextOrder
  return <main>
    <header><div><p className="eyebrow">MR Brasil · Dry End</p><h1>Diagnóstico de pedidos</h1></div><span className={`status ${online ? 'online' : ''}`}><i />{snapshot.state}</span></header>
    <nav><button className={selectedOrder === 'current' ? 'active' : ''} onClick={() => setSelectedOrder('current')}>Pedido atual</button><button className={selectedOrder === 'next' ? 'active' : ''} onClick={() => setSelectedOrder('next')}>Próximo pedido</button><span>ADS · {snapshot.lastSuccessfulReadUtc ? new Date(snapshot.lastSuccessfulReadUtc).toLocaleTimeString('pt-BR') : 'aguardando'}</span></nav>
    {order ? <OrderDiagnostics order={order} /> : <section className="loading"><b>Aguardando dados válidos do PLC</b><span>{snapshot.lastError}</span></section>}
    {snapshot.lastError && <aside><b>Diagnóstico de conexão</b><span>{snapshot.lastError}</span></aside>}
    <footer>Diagnóstico em leitura · Estado do runtime TwinCAT não é alterado</footer>
  </main>
}
export default App
