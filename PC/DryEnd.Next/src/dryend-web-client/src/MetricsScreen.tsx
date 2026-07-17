import { useEffect, useMemo, useState } from 'react'
import './MetricsScreen.css'

type MetricView = 'speed' | 'hourlyAverage' | 'hourlyStopped' | 'stopDurations'

type SpeedRecord = {
  dateTime: string
  machineSpeed: number
}

type StopEvent = {
  start: Date
  end: Date
  durationMinutes: number
}

const STOP_SPEED_THRESHOLD_MPM = 10
const MAX_STOPS_TO_SHOW = 60
const HOURS = Array.from({ length: 24 }, (_, index) => index)

const today = () => {
  const now = new Date()
  const local = new Date(now.getTime() - now.getTimezoneOffset() * 60_000)
  return local.toISOString().slice(0, 10)
}

const formatNumber = (value: number, digits = 0) =>
  value.toLocaleString('pt-BR', {
    minimumFractionDigits: digits,
    maximumFractionDigits: digits,
  })

const formatTime = (date: Date) =>
  date.toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' })

function buildStopEvents(rows: SpeedRecord[]): StopEvent[] {
  const stops: StopEvent[] = []
  let current: StopEvent | null = null

  rows.forEach((row) => {
    const dateTime = new Date(row.dateTime)
    if (row.machineSpeed < STOP_SPEED_THRESHOLD_MPM) {
      if (current === null) {
        current = { start: dateTime, end: dateTime, durationMinutes: 1 }
      } else {
        current.end = dateTime
        current.durationMinutes += 1
      }
      return
    }

    if (current !== null) {
      stops.push(current)
      current = null
    }
  })

  if (current !== null) stops.push(current)
  return stops
}

function toPolyline(values: number[], maximum: number): string {
  if (values.length === 0) return ''
  if (values.length === 1) return `0,${100 - (values[0] / maximum) * 90}`
  return values
    .map((value, index) => `${(index / (values.length - 1)) * 100},${100 - (value / maximum) * 90}`)
    .join(' ')
}

function ChartGrid({ maximum, unit }: { maximum: number; unit: string }) {
  return (
    <div className="metrics-y-axis" aria-hidden="true">
      {[maximum, maximum * 0.75, maximum * 0.5, maximum * 0.25, 0].map((value, index) => (
        <span key={index}>{formatNumber(value)} {unit}</span>
      ))}
    </div>
  )
}

function SpeedLineChart({ rows }: { rows: SpeedRecord[] }) {
  const speeds = rows.map((row) => row.machineSpeed)
  const average = speeds.length ? speeds.reduce((sum, value) => sum + value, 0) / speeds.length : 0
  const maximum = Math.max(1, ...speeds)
  const averageY = 100 - (average / maximum) * 90
  const labelIndexes = rows.length
    ? [0, Math.floor((rows.length - 1) / 4), Math.floor((rows.length - 1) / 2), Math.floor(((rows.length - 1) * 3) / 4), rows.length - 1]
    : []

  return (
    <div className="metrics-chart-frame">
      <ChartGrid maximum={maximum} unit="" />
      <div className="metrics-plot">
        <svg viewBox="0 0 100 100" preserveAspectRatio="none" role="img" aria-label="Velocidade da linha ao longo do dia">
          <g className="metrics-grid-lines">
            {[10, 32.5, 55, 77.5, 100].map((y) => <line key={y} x1="0" x2="100" y1={y} y2={y} />)}
          </g>
          <line className="metrics-average-line" x1="0" x2="100" y1={averageY} y2={averageY} />
          <line className="metrics-maximum-line" x1="0" x2="100" y1="10" y2="10" />
          <polyline className="metrics-speed-line" points={toPolyline(speeds, maximum)} />
        </svg>
        <div className="metrics-time-axis">
          {labelIndexes.map((index, labelIndex) => (
            <span key={`${index}-${labelIndex}`}>{formatTime(new Date(rows[index].dateTime))}</span>
          ))}
        </div>
      </div>
    </div>
  )
}

function HourlyBarChart({ values, unit, valueDigits = 0 }: { values: number[]; unit: string; valueDigits?: number }) {
  const maximum = Math.max(1, ...values)
  return (
    <div className="metrics-chart-frame metrics-bar-frame">
      <ChartGrid maximum={maximum} unit="" />
      <div className="metrics-bars" role="img" aria-label={`Indicadores por hora em ${unit}`}>
        {values.map((value, hour) => (
          <div className="metrics-bar-column" key={hour} title={`${hour.toString().padStart(2, '0')}:00 · ${formatNumber(value, valueDigits)} ${unit}`}>
            <span className="metrics-bar-value">{value > 0 ? formatNumber(value, valueDigits) : ''}</span>
            <i style={{ height: `${Math.max(value > 0 ? 2 : 0, (value / maximum) * 100)}%` }} />
            <small>{hour % 2 === 0 ? hour.toString().padStart(2, '0') : ''}</small>
          </div>
        ))}
      </div>
    </div>
  )
}

function StopDurationChart({ stops }: { stops: StopEvent[] }) {
  const maximum = Math.max(1, ...stops.map((stop) => stop.durationMinutes))
  return (
    <div className="metrics-chart-frame metrics-stop-frame">
      <ChartGrid maximum={maximum} unit="min" />
      <div className="metrics-stop-scroll">
        <div className="metrics-stop-bars" style={{ minWidth: `${Math.max(760, stops.length * 38)}px` }}>
          {stops.map((stop, index) => {
            // EN: The legacy logger stores one sample per minute, so the displayed end is one minute after the last stopped sample.
            // PT: O registrador legado grava uma amostra por minuto; por isso o fim exibido ocorre um minuto após a última amostra parada.
            const displayedEnd = new Date(stop.end.getTime() + 60_000)
            return (
              <div
                className="metrics-stop-column"
                key={`${stop.start.toISOString()}-${index}`}
                title={`Início: ${formatTime(stop.start)}\nFim: ${formatTime(displayedEnd)}\nDuração: ${stop.durationMinutes} min`}
              >
                <b>{stop.durationMinutes}</b>
                <i style={{ height: `${(stop.durationMinutes / maximum) * 100}%` }} />
                <small>Nº {index + 1}</small>
              </div>
            )
          })}
        </div>
      </div>
    </div>
  )
}

export default function MetricsScreen() {
  const [date, setDate] = useState(today())
  const [rows, setRows] = useState<SpeedRecord[]>([])
  const [view, setView] = useState<MetricView>('speed')
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')

  const load = async () => {
    setLoading(true)
    setError('')
    try {
      const response = await fetch(`/api/production-data/machine-speed?date=${encodeURIComponent(date)}`)
      if (!response.ok) {
        const body = await response.json().catch(() => null) as { detail?: string; error?: string } | null
        throw new Error(body?.detail ?? body?.error ?? 'Não foi possível carregar as métricas.')
      }
      const data = await response.json() as SpeedRecord[]
      setRows(data.slice().sort((left, right) => new Date(left.dateTime).getTime() - new Date(right.dateTime).getTime()))
    } catch (exception) {
      setRows([])
      setError(exception instanceof Error ? exception.message : 'Não foi possível carregar as métricas.')
    } finally {
      setLoading(false)
    }
  }

  // EN: Load the selected production day when the metrics page opens.
  // PT: Carrega o dia de produção selecionado quando a página de métricas é aberta.
  // oxlint-disable-next-line react-hooks/exhaustive-deps
  useEffect(() => { void load() }, [])

  const summary = useMemo(() => {
    const speeds = rows.map((row) => row.machineSpeed)
    const average = speeds.length ? speeds.reduce((sum, speed) => sum + speed, 0) / speeds.length : 0
    const stoppedSamples = rows.filter((row) => row.machineSpeed < STOP_SPEED_THRESHOLD_MPM).length
    const stops = buildStopEvents(rows)
    return {
      average,
      maximum: Math.max(0, ...speeds),
      stoppedSamples,
      stops,
    }
  }, [rows])

  const hourlyAverage = useMemo(() => HOURS.map((hour) => {
    const samples = rows.filter((row) => new Date(row.dateTime).getHours() === hour)
    return samples.length ? samples.reduce((sum, row) => sum + row.machineSpeed, 0) / samples.length : 0
  }), [rows])

  const hourlyStopped = useMemo(() => HOURS.map((hour) =>
    rows.filter((row) => new Date(row.dateTime).getHours() === hour && row.machineSpeed < STOP_SPEED_THRESHOLD_MPM).length
  ), [rows])

  const visibleStops = useMemo(() => {
    if (summary.stops.length <= MAX_STOPS_TO_SHOW) return summary.stops
    return summary.stops
      .slice()
      .sort((left, right) => right.durationMinutes - left.durationMinutes)
      .slice(0, MAX_STOPS_TO_SHOW)
      .sort((left, right) => left.start.getTime() - right.start.getTime())
  }, [summary.stops])

  const viewTitle: Record<MetricView, string> = {
    speed: 'Velocidade da linha',
    hourlyAverage: 'Velocidade média por hora',
    hourlyStopped: 'Tempo de máquina parada por hora',
    stopDurations: 'Paradas: quantidade e duração',
  }

  return (
    <section className="metrics-screen">
      <div className="metrics-heading">
        <div>
          <p className="metrics-eyebrow">Produção</p>
          <h2>Métricas da máquina</h2>
          <span>Indicadores calculados a partir do histórico de velocidade.</span>
        </div>
        <div className="metrics-date-action">
          <label htmlFor="metrics-date">Data</label>
          <input id="metrics-date" type="date" value={date} onChange={(event) => setDate(event.target.value)} />
          <button type="button" onClick={() => void load()} disabled={loading}>{loading ? 'Carregando…' : 'Carregar'}</button>
        </div>
      </div>

      {error && <div className="metrics-error" role="alert">{error}</div>}

      <div className="metrics-summary">
        <div><span>Velocidade média</span><b>{formatNumber(summary.average, 1)} m/min</b></div>
        <div><span>Velocidade máxima</span><b>{formatNumber(summary.maximum)} m/min</b></div>
        <div><span>Tempo parado</span><b>{formatNumber(summary.stoppedSamples)} min</b><small>Abaixo de {STOP_SPEED_THRESHOLD_MPM} m/min</small></div>
        <div><span>Quantidade de paradas</span><b>{formatNumber(summary.stops.length)}</b><small>{formatNumber(rows.length)} amostras no dia</small></div>
      </div>

      <div className="metrics-workspace">
        <nav className="metrics-navigation" aria-label="Tipos de gráfico">
          <button className={view === 'speed' ? 'active' : ''} onClick={() => setView('speed')}>Velocidade da linha</button>
          <button className={view === 'hourlyAverage' ? 'active' : ''} onClick={() => setView('hourlyAverage')}>Velocidade média H/H</button>
          <button className={view === 'hourlyStopped' ? 'active' : ''} onClick={() => setView('hourlyStopped')}>Tempo de parada H/H</button>
          <button className={view === 'stopDurations' ? 'active' : ''} onClick={() => setView('stopDurations')}>Tempo de cada parada</button>
        </nav>

        <article className="metrics-chart-card">
          <div className="metrics-chart-title">
            <div><h3>{viewTitle[view]}</h3><span>{new Date(`${date}T12:00:00`).toLocaleDateString('pt-BR')}</span></div>
            {view === 'speed' && <div className="metrics-legend"><i className="speed" />Velocidade<i className="average" />Média<i className="maximum" />Máximo</div>}
          </div>
          {loading ? <div className="metrics-empty">Carregando dados…</div> : rows.length === 0 ? <div className="metrics-empty">Sem dados para o período selecionado.</div> : (
            <>
              {view === 'speed' && <SpeedLineChart rows={rows} />}
              {view === 'hourlyAverage' && <HourlyBarChart values={hourlyAverage} unit="m/min" valueDigits={1} />}
              {view === 'hourlyStopped' && <HourlyBarChart values={hourlyStopped} unit="min" />}
              {view === 'stopDurations' && (visibleStops.length ? <StopDurationChart stops={visibleStops} /> : <div className="metrics-empty">Nenhuma parada encontrada.</div>)}
            </>
          )}
          {view === 'stopDurations' && summary.stops.length > MAX_STOPS_TO_SHOW && (
            <p className="metrics-note">Exibindo as {MAX_STOPS_TO_SHOW} paradas de maior duração, em ordem cronológica.</p>
          )}
        </article>
      </div>
    </section>
  )
}
