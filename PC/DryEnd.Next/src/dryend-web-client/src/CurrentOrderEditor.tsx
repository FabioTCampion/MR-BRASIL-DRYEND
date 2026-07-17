import { useState } from 'react'
import './CurrentOrderEditor.css'

type EditableOrderChannel = {
  id: number
  product: string
  client: string
  sheetType: number
  sheetQuantity: number
  sheetLength: number
  sheetMeasures: number[]
  numberOfCuts: number
  numberOfCutsProduced: number
  numberOfCutsRemaining: number
  pileQuantity: number
  pileQuantityProduced: number
  pileQuantityRemaining: number
  pileCounter: number
  scrapCounter: number
}

type EditablePlcOrder = {
  startedAt: string
  tableId: number
  productionListNumber: number
  levelSelector: number
  paperComposition: string
  fluteType: string
  paperWidth: number
  paperLayers: string[]
  lineSpeed: number
  linearMeters: number
  linearMetersProduced: number
  linearMetersRemaining: number
  scorerHeightMm: number
  invertOrderLevel: boolean
  invertOrderSide: boolean
  order1: EditableOrderChannel
  order2: EditableOrderChannel
}

type Props = {
  order: EditablePlcOrder
  onCancel: () => void
  onSaved: () => void
}

function NumberInput({ label, value, onChange, step = 1, disabled = false }: {
  label: string
  value: number
  onChange: (value: number) => void
  step?: number
  disabled?: boolean
}) {
  return <label>{label}<input type="number" value={value} step={step} disabled={disabled} onChange={(event) => onChange(Number(event.target.value))} /></label>
}

function TextInput({ label, value, onChange }: {
  label: string
  value: string
  onChange: (value: string) => void
}) {
  return <label>{label}<input value={value} onChange={(event) => onChange(event.target.value)} /></label>
}

export default function CurrentOrderEditor({ order, onCancel, onSaved }: Props) {
  const [draft, setDraft] = useState<EditablePlcOrder>(() => structuredClone(order))
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')

  const setRoot = (key: keyof EditablePlcOrder, value: unknown) =>
    setDraft((current) => ({ ...current, [key]: value } as EditablePlcOrder))

  const setChannel = (
    channel: 'order1' | 'order2',
    key: keyof EditableOrderChannel,
    value: unknown,
  ) => setDraft((current) => ({
    ...current,
    [channel]: { ...current[channel], [key]: value },
  }))

  const setMeasure = (channel: 'order1' | 'order2', index: number, value: number) => {
    const measures = [...draft[channel].sheetMeasures]
    measures[index] = value
    setChannel(channel, 'sheetMeasures', measures)
  }

  const setPaper = (index: number, value: string) => {
    const papers = [...draft.paperLayers]
    papers[index] = value
    setRoot('paperLayers', papers)
  }

  const save = async () => {
    if (!window.confirm('Confirma a escrita direta do pedido atual no PLC?')) return

    setSaving(true)
    setError('')
    try {
      const response = await fetch('/api/plc/current-order', {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(draft),
      })
      if (!response.ok) {
        const body = await response.json().catch(() => null) as { detail?: string; error?: string } | null
        throw new Error(body?.detail ?? body?.error ?? 'Falha ao escrever o pedido atual no PLC.')
      }
      await response.json()
      onSaved()
    } catch (exception) {
      setError(exception instanceof Error ? exception.message : 'Falha ao escrever o pedido atual no PLC.')
    } finally {
      setSaving(false)
    }
  }

  const channelEditor = (channel: 'order1' | 'order2', title: string) => {
    const value = draft[channel]
    return <fieldset className="plc-channel-editor">
      <legend>{title}</legend>
      <div className="plc-editor-grid">
        <NumberInput label="OF" value={value.id} onChange={(next) => setChannel(channel, 'id', next)} />
        <TextInput label="Produto" value={value.product} onChange={(next) => setChannel(channel, 'product', next)} />
        <TextInput label="Cliente" value={value.client} onChange={(next) => setChannel(channel, 'client', next)} />
        <label>Tipo da chapa<select value={value.sheetType} onChange={(event) => setChannel(channel, 'sheetType', Number(event.target.value))}><option value={0}>Sem vincos</option><option value={1}>Dois vincos</option><option value={2}>Quatro vincos</option></select></label>
        <NumberInput label="Chapas por corte" value={value.sheetQuantity} onChange={(next) => setChannel(channel, 'sheetQuantity', next)} />
        <NumberInput label="Comprimento (mm)" value={value.sheetLength} onChange={(next) => setChannel(channel, 'sheetLength', next)} />
      </div>
      <div className="plc-measure-grid">{value.sheetMeasures.map((measure, index) => <NumberInput key={index} label={`M${index + 1} (mm)`} value={measure} onChange={(next) => setMeasure(channel, index, next)} />)}</div>
      <div className="plc-editor-grid counters">
        <NumberInput label="Total de cortes" value={value.numberOfCuts} onChange={(next) => setChannel(channel, 'numberOfCuts', next)} />
        <NumberInput label="Cortes produzidos" value={value.numberOfCutsProduced} onChange={(next) => setChannel(channel, 'numberOfCutsProduced', next)} />
        <NumberInput label="Cortes restantes" value={value.numberOfCutsRemaining} onChange={(next) => setChannel(channel, 'numberOfCutsRemaining', next)} />
        <NumberInput label="Tamanho da pilha" value={value.pileQuantity} onChange={(next) => setChannel(channel, 'pileQuantity', next)} />
        <NumberInput label="Produzido na pilha" value={value.pileQuantityProduced} onChange={(next) => setChannel(channel, 'pileQuantityProduced', next)} />
        <NumberInput label="Restante na pilha" value={value.pileQuantityRemaining} onChange={(next) => setChannel(channel, 'pileQuantityRemaining', next)} />
        <NumberInput label="Contador de pilhas" value={value.pileCounter} onChange={(next) => setChannel(channel, 'pileCounter', next)} />
        <NumberInput label="Refugo" value={value.scrapCounter} onChange={(next) => setChannel(channel, 'scrapCounter', next)} />
      </div>
    </fieldset>
  }

  return <div className="plc-editor-backdrop" role="dialog" aria-modal="true" aria-label="Editar pedido atual">
    <div className="plc-editor-modal">
      <div className="plc-editor-heading"><div><p>Edição ADS</p><h2>Editar pedido atual</h2><span>Os valores serão escritos diretamente em <b>.currentOrder</b> no PLC.</span></div><button type="button" onClick={onCancel}>Fechar</button></div>
      {error && <div className="plc-editor-error" role="alert">{error}</div>}
      <section className="plc-editor-section">
        <h3>Identificação e papel</h3>
        <div className="plc-editor-grid">
          <TextInput label="Início" value={draft.startedAt} onChange={(value) => setRoot('startedAt', value)} />
          <NumberInput label="Table ID" value={draft.tableId} onChange={(value) => setRoot('tableId', value)} />
          <NumberInput label="Lista de produção" value={draft.productionListNumber} onChange={(value) => setRoot('productionListNumber', value)} />
          <label>Seleção de pedidos (LevelSelector)<select value={draft.levelSelector} onChange={(event) => setRoot('levelSelector', Number(event.target.value))}><option value={1}>Somente pedido 1</option><option value={2}>Somente pedido 2</option><option value={3}>Pedidos 1 e 2</option></select></label>
          <TextInput label="Composição" value={draft.paperComposition} onChange={(value) => setRoot('paperComposition', value)} />
          <TextInput label="Onda" value={draft.fluteType} onChange={(value) => setRoot('fluteType', value)} />
          <NumberInput label="Largura do papel (mm)" value={draft.paperWidth} onChange={(value) => setRoot('paperWidth', value)} />
          <NumberInput label="Altura do vinco (mm)" value={draft.scorerHeightMm} step={0.01} onChange={(value) => setRoot('scorerHeightMm', value)} />
          <NumberInput label="Velocidade atual (somente leitura)" value={draft.lineSpeed} step={0.1} disabled onChange={() => undefined} />
        </div>
        <div className="plc-paper-grid">{draft.paperLayers.map((paper, index) => <TextInput key={index} label={`Papel ${index + 1}`} value={paper} onChange={(value) => setPaper(index, value)} />)}</div>
        <div className="plc-editor-grid meters">
          <NumberInput label="Metragem total" value={draft.linearMeters} step={0.1} onChange={(value) => setRoot('linearMeters', value)} />
          <NumberInput label="Metragem produzida" value={draft.linearMetersProduced} step={0.1} onChange={(value) => setRoot('linearMetersProduced', value)} />
          <NumberInput label="Metragem restante" value={draft.linearMetersRemaining} step={0.1} onChange={(value) => setRoot('linearMetersRemaining', value)} />
          <label className="plc-checkbox"><input type="checkbox" checked={draft.invertOrderLevel} onChange={(event) => setRoot('invertOrderLevel', event.target.checked)} />Inverter nível</label>
          <label className="plc-checkbox"><input type="checkbox" checked={draft.invertOrderSide} onChange={(event) => setRoot('invertOrderSide', event.target.checked)} />Inverter lado</label>
        </div>
      </section>
      <div className="plc-channel-grid">{channelEditor('order1', 'Pedido 1')}{channelEditor('order2', 'Pedido 2')}</div>
      <div className="plc-editor-actions"><button type="button" onClick={onCancel} disabled={saving}>Cancelar</button><button type="button" className="write" onClick={() => void save()} disabled={saving}>{saving ? 'Escrevendo no PLC…' : 'Gravar no PLC'}</button></div>
    </div>
  </div>
}
