import { HubConnectionBuilder } from '@microsoft/signalr'
import { useEffect, useState } from 'react'
import './App.css'

type OrderSnapshot = {
  tableId: number
  productionListNumber: number
  levelSelector: number
  fluteType: string
  lineSpeed: number
  plcWatchDog: boolean
}

type MonitorSnapshot = {
  state: string
  data: { currentOrder: OrderSnapshot; nextOrder: OrderSnapshot; capturedAtUtc: string } | null
  lastSuccessfulReadUtc: string | null
  lastError: string | null
}

const initialSnapshot: MonitorSnapshot = {
  state: 'Offline', data: null, lastSuccessfulReadUtc: null, lastError: null,
}

function OrderCard({ title, order }: { title: string; order?: OrderSnapshot }) {
  return <article className="card">
    <p className="eyebrow">{title}</p>
    <strong>{order?.productionListNumber ?? '—'}</strong>
    <dl>
      <div><dt>Table ID</dt><dd>{order?.tableId ?? '—'}</dd></div>
      <div><dt>Nível</dt><dd>{order?.levelSelector ?? '—'}</dd></div>
      <div><dt>Onda</dt><dd>{order?.fluteType || '—'}</dd></div>
      <div><dt>Velocidade</dt><dd>{order ? `${order.lineSpeed.toFixed(1)} m/min` : '—'}</dd></div>
    </dl>
  </article>
}

function App() {
  const [snapshot, setSnapshot] = useState(initialSnapshot)

  useEffect(() => {
    const controller = new AbortController()
    fetch('/api/diagnostics', { signal: controller.signal })
      .then((response) => response.json())
      .then(setSnapshot)
      .catch(() => undefined)

    const connection = new HubConnectionBuilder().withUrl('/hubs/diagnostics').withAutomaticReconnect().build()
    connection.on('diagnosticsUpdated', setSnapshot)
    connection.start().catch(() => undefined)
    return () => { controller.abort(); void connection.stop() }
  }, [])

  const online = snapshot.state === 'Online'
  return <main>
    <header>
      <div><p className="eyebrow">MR Brasil · Dry End</p><h1>Production overview</h1></div>
      <span className={`status ${online ? 'online' : ''}`}><i />{snapshot.state}</span>
    </header>

    <section className="summary">
      <div><span>PLC watchdog</span><b>{snapshot.data?.currentOrder.plcWatchDog ? 'Ativo' : 'Inativo'}</b></div>
      <div><span>Última leitura</span><b>{snapshot.lastSuccessfulReadUtc ? new Date(snapshot.lastSuccessfulReadUtc).toLocaleTimeString() : 'Aguardando'}</b></div>
      <div><span>Comunicação</span><b>ADS · Runtime 1</b></div>
    </section>

    <section className="orders">
      <OrderCard title="Pedido em produção" order={snapshot.data?.currentOrder} />
      <OrderCard title="Próximo pedido" order={snapshot.data?.nextOrder} />
    </section>

    {snapshot.lastError && <aside><b>Diagnóstico de conexão</b><span>{snapshot.lastError}</span></aside>}
    <footer>Monitoramento somente nesta etapa · Estado do runtime TwinCAT não é alterado</footer>
  </main>
}

export default App
