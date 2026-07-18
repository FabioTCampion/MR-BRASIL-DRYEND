import { HubConnectionBuilder } from "@microsoft/signalr";
import { lazy, Suspense, useEffect, useMemo, useState } from "react";
import "./App.css";
import "./SideMenu.css";
import "./QueueSequence.css";
import CurrentOrderEditor from "./CurrentOrderEditor";
import HistoryScreen from "./HistoryScreen";
import SideMenu, { type NavigationItem } from "./SideMenu";
import TrimboxImportScreen from "./TrimboxImportScreen";
import { AuthProvider, roleLabel, useAuth, UsersScreen } from "./Auth";
import { isOrderChannelEnabled, LevelSelectorMark, OrderLevelMark } from "./OrderLevel";

type Page =
  "production" | "orders" | "history" | "graphs" | "diagnostics" | "users";
type ToolReference = {
  index: number;
  enabled: boolean;
  positionReferenceMm: number;
};
type GeneratedOrder = {
  numberOfKnives: number;
  numberOfScorers: number;
  numberOfSheets: number;
  order1Width: number;
  order2Width: number;
  orderTotalWidth: number;
  firstKnifePosition: number;
  lastKnifePosition: number;
  knives: ToolReference[];
  scorers: ToolReference[];
  orderNotOk: boolean;
  knivesOutOfRange: number[];
  scorersOutOfRange: number[];
};
type OrderChannel = {
  id: number;
  product: string;
  client: string;
  sheetType: number;
  sheetQuantity: number;
  sheetLength: number;
  sheetMeasures: number[];
  numberOfCuts: number;
  numberOfCutsProduced: number;
  numberOfCutsRemaining: number;
  pileQuantity: number;
  pileQuantityProduced: number;
  pileQuantityRemaining: number;
  pileCounter: number;
  scrapCounter: number;
};
type PlcOrder = {
  startedAt: string;
  tableId: number;
  productionListNumber: number;
  levelSelector: number;
  paperComposition: string;
  fluteType: string;
  paperWidth: number;
  paperLayers: string[];
  lineSpeed: number;
  linearMeters: number;
  linearMetersProduced: number;
  linearMetersRemaining: number;
  scorerHeightMm: number;
  plcWatchDog: boolean;
  aocRequest: boolean;
  changeOrderRequest: boolean;
  saveSqlFinished: boolean;
  saveSqlTimeout: boolean;
  invertOrderLevel: boolean;
  invertOrderSide: boolean;
  order1: OrderChannel;
  order2: OrderChannel;
  generatedOrder: GeneratedOrder;
};
type MonitorSnapshot = {
  state: string;
  data: {
    currentOrder: PlcOrder;
    nextOrder: PlcOrder;
    capturedAtUtc: string;
  } | null;
  lastSuccessfulReadUtc: string | null;
  lastError: string | null;
};
type NextOrderDifference = { field: string; systemValue: string; plcValue: string };
type NextOrderComparison = { available: boolean; divergent: boolean; tableId: number; differences: NextOrderDifference[]; error: string | null };
type DatabaseStatus = {
  configured: boolean;
  available: boolean;
  message: string | null;
};
type DbOrder = {
  [key: string]: string | number | null | undefined;
  id: number;
  productionSequence: number | null;
  productionState: number | null;
  historySavedAt: string | null;
  startedAt: string | null;
  finishedAt: string | null;
  paperComposition: string;
  fluteType: string;
  paperWidth: number;
  paper1: string;
  paper2: string;
  paper3: string;
  paper4: string;
  paper5: string;
  productionListNumber: string;
  levelSelector: number;
  order1Id: number | null;
  order1Product: string;
  order1Client: string;
  order1SheetQuantity: number;
  order1SheetType: number;
  order1M1: number;
  order1M2: number;
  order1M3: number;
  order1M4: number;
  order1M5: number;
  order1SheetLength: number;
  order1NumberOfCuts: number;
  order1NumberOfCutsProduced: number | null;
  order1PileQuantity: number;
  order2Id: number | null;
  order2Product: string;
  order2Client: string;
  order2SheetQuantity: number | null;
  order2SheetType: number | null;
  order2M1: number | null;
  order2M2: number | null;
  order2M3: number | null;
  order2M4: number | null;
  order2M5: number | null;
  order2SheetLength: number | null;
  order2NumberOfCuts: number | null;
  order2NumberOfCutsProduced: number | null;
  order2PileQuantity: number | null;
};

const initialSnapshot: MonitorSnapshot = {
  state: "Offline",
  data: null,
  lastSuccessfulReadUtc: null,
  lastError: null,
};
const LazyMetricsScreen = lazy(() => import("./MetricsScreen"));
function MetricsScreen() {
  return (
    <Suspense
      fallback={
        <section className="loading">
          <b>Carregando métricas interativas…</b>
        </section>
      }
    >
      <LazyMetricsScreen />
    </Suspense>
  );
}
const emptyOrder: DbOrder = {
  id: 0,
  productionSequence: 0,
  productionState: 0,
  historySavedAt: null,
  startedAt: null,
  finishedAt: null,
  paperComposition: "",
  fluteType: "B",
  paperWidth: 1800,
  paper1: "N/A",
  paper2: "N/A",
  paper3: "N/A",
  paper4: "N/A",
  paper5: "N/A",
  productionListNumber: "",
  levelSelector: 1,
  order1Id: null,
  order1Product: "CX",
  order1Client: "",
  order1SheetQuantity: 1,
  order1SheetType: 0,
  order1M1: 0,
  order1M2: 0,
  order1M3: 0,
  order1M4: 0,
  order1M5: 0,
  order1SheetLength: 450,
  order1NumberOfCuts: 1,
  order1NumberOfCutsProduced: 0,
  order1PileQuantity: 500,
  order2Id: null,
  order2Product: "",
  order2Client: "",
  order2SheetQuantity: null,
  order2SheetType: null,
  order2M1: null,
  order2M2: null,
  order2M3: null,
  order2M4: null,
  order2M5: null,
  order2SheetLength: null,
  order2NumberOfCuts: null,
  order2NumberOfCutsProduced: null,
  order2PileQuantity: null,
};
const sheetTypeNames: Record<number, string> = {
  0: "Sem vincos",
  1: "Dois vincos",
  2: "Quatro vincos",
};
const navigationItems: readonly NavigationItem<Page>[] = [
  { id: "production", label: "Produção", icon: "production" },
  { id: "orders", label: "Pedidos", icon: "orders" },
  { id: "history", label: "Histórico", icon: "history" },
  { id: "graphs", label: "Gráficos", icon: "graphs" },
  { id: "diagnostics", label: "Diagnóstico", icon: "diagnostics" },
  { id: "users", label: "Usuários", icon: "users" },
];
const formatNumber = (value: number, digits = 0) =>
  value.toLocaleString("pt-BR", { maximumFractionDigits: digits });
const measureCount = (sheetType: number) =>
  sheetType >= 2 ? 5 : sheetType >= 1 ? 3 : 1;
const activeMeasures = (sheetType: number, measures: number[]) =>
  measures.slice(0, measureCount(sheetType));
const measuresText = (sheetType: number, measures: number[]) =>
  activeMeasures(sheetType, measures).join(" × ");

function usePlcMonitor() {
  const [snapshot, setSnapshot] = useState(initialSnapshot);
  useEffect(() => {
    const controller = new AbortController();
    fetch("/api/diagnostics", { signal: controller.signal })
      .then((r) => r.json())
      .then(setSnapshot)
      .catch(() => undefined);
    const connection = new HubConnectionBuilder()
      .withUrl("/hubs/diagnostics")
      .withAutomaticReconnect()
      .build();
    connection.on("diagnosticsUpdated", setSnapshot);
    connection.start().catch(() => undefined);
    return () => {
      controller.abort();
      void connection.stop();
    };
  }, []);
  return snapshot;
}

function Progress({ value, maximum }: { value: number; maximum: number }) {
  const percent =
    maximum > 0 ? Math.min(100, Math.max(0, (value / maximum) * 100)) : 0;
  return (
    <div className="progress">
      <i style={{ width: `${percent}%` }} />
    </div>
  );
}

function OrderIdentitySummary({
  name,
  order,
  levelSelector,
  channel,
}: {
  name: string;
  order: OrderChannel;
  levelSelector: number;
  channel: 1 | 2;
}) {
  const linearMeters = (order.numberOfCuts * order.sheetLength) / 1000;
  return (
    <div className="order-identity-summary">
      <span className="order-title-with-level">
        {name} · OF {order.id || "—"}
        <OrderLevelMark levelSelector={levelSelector} channel={channel}/>
      </span>
      <b>{order.client || "Cliente não informado"}</b>
      <strong>{order.product || "Produto não informado"}</strong>
      <div className="order-identity-facts">
        <span>
          Caixa
          <b>
            {order.sheetQuantity} ×{" "}
            {measuresText(order.sheetType, order.sheetMeasures)}
          </b>
        </span>
        <span>
          Comprimento<b>{formatNumber(order.sheetLength)} mm</b>
        </span>
        <span>
          Número de cortes<b>{formatNumber(order.numberOfCuts)}</b>
        </span>
        <span>
          Quantidade da pilha<b>{formatNumber(order.pileQuantity)}</b>
        </span>
        <span>
          Metro linear<b>{formatNumber(linearMeters, 1)} m</b>
        </span>
      </div>
    </div>
  );
}

function PaperCompositionSummary({ order }: { order: PlcOrder }) {
  return (
    <div className="paper-composition-summary">
      <span>Composição</span>
      <strong>{order.paperComposition || "Não informada"}</strong>
      <div className="paper-composition-meta">
        <b>Onda {order.fluteType || "—"}</b>
        <b>Largura: {formatNumber(order.paperWidth)} mm</b>
      </div>
      <div className="paper-composition-layers">
        {Array.from({ length: 5 }, (_, index) => (
          <small
            key={index}
            className={order.paperLayers[index] ? "" : "empty"}
          >
            {order.paperLayers[index] || `PAPER ${index + 1}`}
          </small>
        ))}
      </div>
    </div>
  );
}

function ChannelCard({ name, order, levelSelector, channel }: { name: string; order: OrderChannel; levelSelector: number; channel: 1 | 2 }) {
  const visibleMeasures = activeMeasures(order.sheetType, order.sheetMeasures);
  return (
    <article className="channel-card">
      <OrderIdentitySummary name={name} order={order} levelSelector={levelSelector} channel={channel}/>
      <div className="channel-facts">
        <span>
          Tipo da caixa
          <b>{sheetTypeNames[order.sheetType] ?? `Tipo ${order.sheetType}`}</b>
        </span>
        {visibleMeasures.map((m, i) => (
          <span key={i}>
            M{i + 1}
            <b>{m} mm</b>
          </span>
        ))}
      </div>
      <div className="counter-heading">
        <span>
          Produção de cortes
          <small>{formatNumber(order.numberOfCutsRemaining)} restantes</small>
        </span>
        <b>
          {formatNumber(order.numberOfCutsProduced)} /{" "}
          {formatNumber(order.numberOfCuts)}
        </b>
      </div>
      <Progress
        value={order.numberOfCutsProduced}
        maximum={order.numberOfCuts}
      />
      <div className="pile-grid">
        <span>
          Chapas na pilha
          <b>
            {order.pileQuantityProduced} / {order.pileQuantity}
          </b>
        </span>
        <span>
          Saldo da pilha<b>{order.pileQuantityRemaining}</b>
        </span>
        <span>
          Pilhas concluídas<b>{order.pileCounter}</b>
        </span>
        <span>
          Refugo<b>{order.scrapCounter}</b>
        </span>
      </div>
    </article>
  );
}

function OrderCommandPreview({
  title,
  order,
}: {
  title: string;
  order: PlcOrder;
}) {
  const channels = [
    ...(isOrderChannelEnabled(order.levelSelector, 1)
      ? [{ name: "Pedido 1", data: order.order1, channel: 1 as const }]
      : []),
    ...(isOrderChannelEnabled(order.levelSelector, 2)
      ? [{ name: "Pedido 2", data: order.order2, channel: 2 as const }]
      : []),
  ];
  return (
    <article className="order-command-preview">
      <header>
        <div>
          <span>{title}</span>
          <b>Lista {order.productionListNumber || "—"}</b>
        </div>
        <strong>Table ID {order.tableId || "—"}</strong>
      </header>
      <div className="order-command-paper">
        <span>
          Papel
          <b>
            {order.paperWidth} mm · Onda {order.fluteType || "—"}
          </b>
        </span>
        <small>{order.paperComposition || "Composição não informada"}</small>
      </div>
      <div className="order-command-channels">
        {channels.map((channel) => (
          <div key={channel.name}>
            <span>
              {channel.name} · OF {channel.data.id || "—"}
            </span>
            <OrderLevelMark levelSelector={order.levelSelector} channel={channel.channel}/>
            <b>{channel.data.client || "Cliente não informado"}</b>
            <strong>{channel.data.product || "Produto não informado"}</strong>
            <small>
              {channel.data.sheetQuantity} ×{" "}
              {measuresText(channel.data.sheetType, channel.data.sheetMeasures)}{" "}
              × {channel.data.sheetLength} mm
            </small>
            <small>
              {formatNumber(channel.data.numberOfCuts)} cortes · pilha{" "}
              {formatNumber(channel.data.pileQuantity)}
            </small>
          </div>
        ))}
      </div>
    </article>
  );
}

function ProductionScreen({
  current,
  next,
}: {
  current: PlcOrder;
  next: PlcOrder;
}) {
  const { user } = useAuth();
  const canCommand = user.role !== "Observer";
  const canEdit = user.role === "Supervisor" || user.role === "Administrator";
  const [editing, setEditing] = useState(false);
  const [orderCommand, setOrderCommand] = useState<
    "manual" | "automatic" | null
  >(null);
  const [orderCommandConfirmation, setOrderCommandConfirmation] = useState<
    "manual" | "automatic" | null
  >(null);
  const [orderCommandError, setOrderCommandError] = useState("");
  const [nextComparison, setNextComparison] = useState<NextOrderComparison | null>(null);
  const [rewritingNextOrder, setRewritingNextOrder] = useState(false);
  const loadNextComparison = async () => {
    if (next.tableId <= 0) {
      setNextComparison(null);
      return;
    }
    try {
      const response = await fetch("/api/plc/next-order/comparison");
      if (response.ok) setNextComparison(await response.json());
    } catch {
      // The connection banner already reports transient server failures.
    }
  };
  useEffect(() => {
    void loadNextComparison();
    const timer = window.setInterval(loadNextComparison, 5_000);
    return () => window.clearInterval(timer);
  }, [next.tableId]);
  const rewriteNextOrder = async () => {
    if (!nextComparison?.divergent || !window.confirm("Reescrever o próximo pedido completo no CLP com os valores programados no sistema? As alterações feitas diretamente no HMI da máquina serão substituídas.")) return;
    setRewritingNextOrder(true);
    setOrderCommandError("");
    try {
      const response = await fetch(`/api/plc/next-order/${next.tableId}?overwrite=false`, { method: "POST" });
      const body = await response.json().catch(() => ({}));
      if (!response.ok) throw new Error(body.error ?? body.detail ?? "Não foi possível reescrever o próximo pedido.");
      await loadNextComparison();
    } catch (exception) {
      setOrderCommandError(exception instanceof Error ? exception.message : "Não foi possível reescrever o próximo pedido.");
    } finally {
      setRewritingNextOrder(false);
    }
  };
  const progressPercent =
    current.linearMeters > 0
      ? Math.min(
          100,
          Math.max(
            0,
            (current.linearMetersProduced / current.linearMeters) * 100,
          ),
        )
      : 0;
  const minutesRemaining =
    current.lineSpeed > 0 && current.linearMetersRemaining > 0
      ? current.linearMetersRemaining / current.lineSpeed
      : null;
  const estimatedFinish =
    minutesRemaining === null
      ? null
      : new Date(Date.now() + minutesRemaining * 60_000);
  const durationText =
    minutesRemaining === null
      ? "Indisponível"
      : minutesRemaining < 1
        ? "< 1 min"
        : `${Math.floor(minutesRemaining / 60)}h ${Math.ceil(minutesRemaining % 60)}min`;
  const lineState = current.lineSpeed > 0 ? "Produzindo" : "Linha parada";
  const activePaperLayers = current.paperLayers.filter((paper) =>
    paper?.trim(),
  );
  const requestOrderChange = async (mode: "manual" | "automatic") => {
    const automatic = mode === "automatic";
    setOrderCommandConfirmation(null);
    setOrderCommand(mode);
    setOrderCommandError("");
    try {
      const endpoint = automatic
        ? "/api/plc/current-order/automatic-change-request"
        : "/api/plc/current-order/change-request";
      const response = await fetch(endpoint, { method: "POST" });
      if (!response.ok) {
        const body = await response.json().catch(() => null);
        throw new Error(
          body?.detail ??
            body?.error ??
            "O PLC não confirmou o comando de troca.",
        );
      }
    } catch (exception) {
      setOrderCommandError(
        exception instanceof Error
          ? exception.message
          : "Falha ao enviar o comando ao PLC.",
      );
    } finally {
      setOrderCommand(null);
    }
  };
  return (
    <>
      <div className="screen-title">
        <div>
          <p className="eyebrow">Tela principal</p>
          <h2>Produção atual</h2>
          <span
            className={`production-state ${current.lineSpeed > 0 ? "running" : "stopped"}`}
          >
            <i />
            {lineState}
          </span>
        </div>
        <div className="legacy-actions">
          {canEdit && (
            <button className="edit-current" onClick={() => setEditing(true)}>
              Editar pedido atual
            </button>
          )}
          {canCommand && (
            <button
              disabled={orderCommand !== null || current.changeOrderRequest}
              onClick={() => setOrderCommandConfirmation("manual")}
            >
              {orderCommand === "manual"
                ? "Enviando…"
                : current.changeOrderRequest
                  ? "Troca solicitada"
                  : "Trocar pedido"}
            </button>
          )}
          {canCommand && (
            <button
              disabled={orderCommand !== null || current.aocRequest}
              onClick={() => setOrderCommandConfirmation("automatic")}
            >
              {orderCommand === "automatic"
                ? "Enviando…"
                : current.aocRequest
                  ? "Troca automática solicitada"
                  : "Troca automática"}
            </button>
          )}
        </div>
      </div>
      {orderCommandError && (
        <div className="form-error">{orderCommandError}</div>
      )}
      <section className="production-overview">
        <div
          className={`speed-focus ${current.lineSpeed > 0 ? "running" : "stopped"}`}
        >
          <span>Velocidade da máquina</span>
          <strong>
            {formatNumber(current.lineSpeed, 1)}
            <small>m/min</small>
          </strong>
          <p>{lineState}</p>
        </div>
        <div className="order-focus">
          <div>
            <span>Lista em produção</span>
            <strong>{current.productionListNumber || "—"}</strong>
            <small>
              Table ID {current.tableId || "—"}
            </small>
            <LevelSelectorMark value={current.levelSelector}/>
          </div>
          <div className="paper-highlight">
            <PaperCompositionSummary order={current} />
          </div>
        </div>
        <div className="eta-focus">
          <span>Tempo estimado</span>
          <strong>{durationText}</strong>
          <small>
            {estimatedFinish
              ? `Conclusão estimada às ${estimatedFinish.toLocaleTimeString("pt-BR", { hour: "2-digit", minute: "2-digit" })}`
              : "Aguardando a linha entrar em movimento"}
          </small>
          <small>Início: {current.startedAt || "—"}</small>
        </div>
      </section>
      <section className="production-progress-card">
        <div className="progress-heading">
          <div>
            <p className="eyebrow">Avanço do pedido</p>
            <b>{formatNumber(progressPercent, 1)}%</b>
          </div>
          <span>
            {formatNumber(current.linearMetersProduced, 1)} de{" "}
            {formatNumber(current.linearMeters, 1)} metros
          </span>
        </div>
        <div className="production-progress-track">
          <i style={{ width: `${progressPercent}%` }} />
        </div>
        <div className="meter-summary">
          <span>
            Metragem total<b>{formatNumber(current.linearMeters, 1)} m</b>
          </span>
          <span>
            Produzida<b>{formatNumber(current.linearMetersProduced, 1)} m</b>
          </span>
          <span>
            Restante<b>{formatNumber(current.linearMetersRemaining, 1)} m</b>
          </span>
          <span>
            Previsão<b>{durationText}</b>
          </span>
        </div>
      </section>
      <section className="production-details">
        <div>
          <p className="eyebrow">Composição do papel</p>
          <div className="paper-layers">
            {activePaperLayers.length ? (
              activePaperLayers.map((paper, index) => (
                <span key={index}>
                  Papel {index + 1}
                  <b>{paper}</b>
                </span>
              ))
            ) : (
              <span>
                Papel<b>Não informado</b>
              </span>
            )}
          </div>
        </div>
        <div className="production-statuses">
          <p className="eyebrow">Status do ciclo</p>
          <div>
            <span className={current.plcWatchDog ? "ok" : "warning"}>
              PLC {current.plcWatchDog ? "comunicando" : "sem watchdog"}
            </span>
            <span
              className={current.generatedOrder.orderNotOk ? "warning" : "ok"}
            >
              Posições{" "}
              {current.generatedOrder.orderNotOk ? "a verificar" : "válidas"}
            </span>
            <span className={current.changeOrderRequest ? "active" : ""}>
              Troca {current.changeOrderRequest ? "solicitada" : "inativa"}
            </span>
            <span className={current.aocRequest ? "active" : ""}>
              AOC {current.aocRequest ? "solicitado" : "inativo"}
            </span>
            <span
              className={
                current.saveSqlTimeout
                  ? "warning"
                  : current.saveSqlFinished
                    ? "ok"
                    : ""
              }
            >
              Histórico{" "}
              {current.saveSqlTimeout
                ? "timeout"
                : current.saveSqlFinished
                  ? "salvo"
                  : "aguardando"}
            </span>
          </div>
        </div>
      </section>
      <section
        className={`channels ${current.levelSelector === 3 ? "" : "single"}`}
      >
        {isOrderChannelEnabled(current.levelSelector, 1) && (
          <ChannelCard name="Pedido 1" order={current.order1} levelSelector={current.levelSelector} channel={1}/>
        )}{" "}
        {isOrderChannelEnabled(current.levelSelector, 2) && (
          <ChannelCard name="Pedido 2" order={current.order2} levelSelector={current.levelSelector} channel={2}/>
        )}
      </section>
      <section className="next-order-section">
        <div className="next-order-heading">
          <div>
            <p className="eyebrow">Sequência de produção</p>
            <h3>Próximo pedido no PLC</h3>
          </div>
          <span>Table ID {next.tableId || "—"}</span>
        </div>
        <div className="next-order-strip">
          <div className="next-order-identity">
            <span>Lista</span>
            <b>{next.productionListNumber || "—"}</b>
            <LevelSelectorMark value={next.levelSelector}/>
          </div>
          <div className="next-paper">
            <PaperCompositionSummary order={next} />
          </div>
          {isOrderChannelEnabled(next.levelSelector, 1) && (
            <div className="next-order-card">
              <OrderIdentitySummary name="Pedido 1" order={next.order1} levelSelector={next.levelSelector} channel={1}/>
            </div>
          )}
          {isOrderChannelEnabled(next.levelSelector, 2) && (
            <div className="next-order-card">
              <OrderIdentitySummary name="Pedido 2" order={next.order2} levelSelector={next.levelSelector} channel={2}/>
            </div>
          )}
        </div>
        {nextComparison?.divergent && <div className="next-order-divergence">
          <div className="next-order-divergence-heading"><div><b>Pedido divergente do sistema</b><span>{nextComparison.error ?? `${nextComparison.differences.length} campo(s) diferente(s) entre o sistema e o CLP.`}</span></div>{canEdit && <button type="button" disabled={rewritingNextOrder || current.changeOrderRequest || current.aocRequest} onClick={rewriteNextOrder}>{rewritingNextOrder ? "Reescrevendo…" : "Reescrever no CLP"}</button>}</div>
          {nextComparison.differences.length > 0 && <div className="next-order-differences"><div className="next-order-difference-head"><span>Campo</span><span>Programado no sistema</span><span>Atual no CLP</span></div>{nextComparison.differences.map(difference => <div className="next-order-difference-row" key={difference.field}><b>{difference.field}</b><span>{difference.systemValue || "—"}</span><span>{difference.plcValue || "—"}</span></div>)}</div>}
        </div>}
      </section>
      {orderCommandConfirmation && (
        <div
          className="order-command-backdrop"
          role="dialog"
          aria-modal="true"
          aria-labelledby="order-command-title"
        >
          <div className="order-command-dialog">
            <p className="eyebrow">Confirmação do comando</p>
            <h3 id="order-command-title">
              {orderCommandConfirmation === "automatic"
                ? "Executar troca automática?"
                : "Trocar para o próximo pedido?"}
            </h3>
            <p>
              {orderCommandConfirmation === "automatic"
                ? "O PLC iniciará a sequência automática de troca do pedido."
                : "O pedido atual será substituído pelo próximo pedido carregado no PLC."}
            </p>
            <div className="order-command-comparison">
              <OrderCommandPreview title="Pedido atual" order={current} />
              <OrderCommandPreview title="Próximo pedido" order={next} />
            </div>
            <div className="order-command-actions">
              <button
                type="button"
                onClick={() => setOrderCommandConfirmation(null)}
              >
                Cancelar
              </button>
              <button
                type="button"
                className="primary"
                onClick={() => requestOrderChange(orderCommandConfirmation)}
              >
                Confirmar comando
              </button>
            </div>
          </div>
        </div>
      )}
      {editing && (
        <CurrentOrderEditor
          order={current}
          onCancel={() => setEditing(false)}
          onSaved={() => setEditing(false)}
        />
      )}
    </>
  );
}

function DatabaseBanner({ status }: { status: DatabaseStatus | null }) {
  if (!status)
    return <div className="db-banner">Verificando banco de dados…</div>;
  return (
    <div className={`db-banner ${status.available ? "ok" : ""}`}>
      <b>{status.available ? "Banco conectado" : "Banco indisponível"}</b>
      <span>
        {status.available ? "Leitura e gravação habilitadas" : status.message}
      </span>
    </div>
  );
}

function NumberField({
  label,
  value,
  onChange,
  min,
}: {
  label: string;
  value: number | null | undefined;
  onChange: (value: number) => void;
  min?: number;
}) {
  return (
    <label>
      {label}
      <input
        type="number"
        value={value ?? 0}
        min={min}
        onChange={(e) => onChange(Number(e.target.value))}
      />
    </label>
  );
}
function TextField({
  label,
  value,
  onChange,
}: {
  label: string;
  value: string | null | undefined;
  onChange: (value: string) => void;
}) {
  return (
    <label>
      {label}
      <input value={value ?? ""} onChange={(e) => onChange(e.target.value)} />
    </label>
  );
}
function SheetTypeField({
  value,
  onChange,
}: {
  value: number | null | undefined;
  onChange: (value: number) => void;
}) {
  return (
    <label>
      Tipo da chapa
      <select
        value={value ?? 0}
        onChange={(e) => onChange(Number(e.target.value))}
      >
        <option value={0}>Sem vincos</option>
        <option value={1}>Dois vincos</option>
        <option value={2}>Quatro vincos</option>
      </select>
    </label>
  );
}

function OrderForm({
  order,
  onChange,
  onSave,
  onCancel,
}: {
  order: DbOrder;
  onChange: (order: DbOrder) => void;
  onSave: () => void;
  onCancel: () => void;
}) {
  const set = (key: string, value: string | number | null) =>
    onChange({ ...order, [key]: value });
  const setSheetType = (prefix: "order1" | "order2", value: number) => {
    const next = { ...order, [`${prefix}SheetType`]: value };
    for (let i = measureCount(value) + 1; i <= 5; i++)
      next[`${prefix}M${i}`] = 0;
    onChange(next);
  };
  const measures = (prefix: "order1" | "order2", sheetType: number) =>
    Array.from(
      { length: measureCount(sheetType) },
      (_, index) => index + 1,
    ).map((i) => (
      <NumberField
        key={i}
        label={`M${i}`}
        value={order[`${prefix}M${i}`] as number | null}
        onChange={(v) => set(`${prefix}M${i}`, v)}
      />
    ));
  return (
    <div className="order-editor">
      <div className="form-grid">
        <TextField
          label="Lista de produção"
          value={order.productionListNumber}
          onChange={(v) => set("productionListNumber", v)}
        />
        <NumberField
          label="Sequência"
          value={order.productionSequence}
          onChange={(v) => set("productionSequence", v)}
        />
        <TextField
          label="Composição"
          value={order.paperComposition}
          onChange={(v) => set("paperComposition", v)}
        />
        <TextField
          label="Onda"
          value={order.fluteType}
          onChange={(v) => set("fluteType", v)}
        />
        <NumberField
          label="Largura do papel"
          value={order.paperWidth}
          onChange={(v) => set("paperWidth", v)}
        />
        <label>
          Seletor de nível
          <select
            value={order.levelSelector}
            onChange={(e) => set("levelSelector", Number(e.target.value))}
          >
            <option value={1}>Apenas superior</option>
            <option value={2}>Apenas inferior</option>
            <option value={3}>Ambos</option>
          </select>
        </label>
      </div>
      <div className="paper-grid">
        {[1, 2, 3, 4, 5].map((i) => (
          <TextField
            key={i}
            label={`Papel ${i}`}
            value={order[`paper${i}`] as string}
            onChange={(v) => set(`paper${i}`, v)}
          />
        ))}
      </div>
      <div className="form-orders">
        {isOrderChannelEnabled(order.levelSelector, 1) && (
          <fieldset>
            <legend className="order-title-with-level">Pedido 1 <OrderLevelMark levelSelector={order.levelSelector} channel={1}/></legend>
            <div className="form-grid">
              <NumberField
                label="OF"
                value={order.order1Id}
                onChange={(v) => set("order1Id", v)}
              />
              <TextField
                label="Produto"
                value={order.order1Product}
                onChange={(v) => set("order1Product", v)}
              />
              <TextField
                label="Cliente"
                value={order.order1Client}
                onChange={(v) => set("order1Client", v)}
              />
              <SheetTypeField
                value={order.order1SheetType}
                onChange={(v) => setSheetType("order1", v)}
              />
              <NumberField
                label="Quantidade de chapas"
                value={order.order1SheetQuantity}
                onChange={(v) => set("order1SheetQuantity", v)}
              />
              <NumberField
                label="Comprimento"
                value={order.order1SheetLength}
                min={450}
                onChange={(v) => set("order1SheetLength", v)}
              />
              <NumberField
                label="Cortes"
                value={order.order1NumberOfCuts}
                min={1}
                onChange={(v) => set("order1NumberOfCuts", v)}
              />
              <NumberField
                label="Tamanho da pilha"
                value={order.order1PileQuantity}
                min={1}
                onChange={(v) => set("order1PileQuantity", v)}
              />
            </div>
            <div className="measure-fields">
              {measures("order1", order.order1SheetType ?? 0)}
            </div>
          </fieldset>
        )}
        {isOrderChannelEnabled(order.levelSelector, 2) && (
          <fieldset>
            <legend className="order-title-with-level">Pedido 2 <OrderLevelMark levelSelector={order.levelSelector} channel={2}/></legend>
            <div className="form-grid">
              <NumberField
                label="OF"
                value={order.order2Id}
                onChange={(v) => set("order2Id", v)}
              />
              <TextField
                label="Produto"
                value={order.order2Product}
                onChange={(v) => set("order2Product", v)}
              />
              <TextField
                label="Cliente"
                value={order.order2Client}
                onChange={(v) => set("order2Client", v)}
              />
              <SheetTypeField
                value={order.order2SheetType}
                onChange={(v) => setSheetType("order2", v)}
              />
              <NumberField
                label="Quantidade de chapas"
                value={order.order2SheetQuantity}
                onChange={(v) => set("order2SheetQuantity", v)}
              />
              <NumberField
                label="Comprimento"
                value={order.order2SheetLength}
                min={450}
                onChange={(v) => set("order2SheetLength", v)}
              />
              <NumberField
                label="Cortes"
                value={order.order2NumberOfCuts}
                min={1}
                onChange={(v) => set("order2NumberOfCuts", v)}
              />
              <NumberField
                label="Tamanho da pilha"
                value={order.order2PileQuantity}
                min={1}
                onChange={(v) => set("order2PileQuantity", v)}
              />
            </div>
            <div className="measure-fields">
              {measures("order2", order.order2SheetType ?? 0)}
            </div>
          </fieldset>
        )}
      </div>
      <div className="form-actions">
        <button onClick={onCancel}>Cancelar</button>
        <button className="primary" onClick={onSave}>
          Salvar pedido
        </button>
      </div>
    </div>
  );
}

function DatabaseOrderSummary({
  order,
  prefix,
}: {
  order: DbOrder;
  prefix: "order1" | "order2";
}) {
  const isSecond = prefix === "order2";
  const channel = isSecond ? 2 : 1;
  if (!isOrderChannelEnabled(order.levelSelector, channel))
    return <span className="disabled-order">Não utilizado</span>;
  const type = Number(order[`${prefix}SheetType`] ?? 0),
    measures = [1, 2, 3, 4, 5].map((i) => Number(order[`${prefix}M${i}`] ?? 0));
  return (
    <span className="database-order">
      <OrderLevelMark levelSelector={order.levelSelector} channel={channel}/>
      <b>
        {String(order[`${prefix}Client`] ?? "") || "Sem cliente"} ·{" "}
        {String(order[`${prefix}Product`] ?? "") || "Sem produto"}
      </b>
      <small>
        OF {order[`${prefix}Id`] ?? "—"} ·{" "}
        {sheetTypeNames[type] ?? `Tipo ${type}`}
      </small>
      <small>
        {order[`${prefix}SheetQuantity`] ?? 0} × {measuresText(type, measures)}{" "}
        × {order[`${prefix}SheetLength`] ?? 0} mm
      </small>
      <small>
        {formatNumber(Number(order[`${prefix}NumberOfCuts`] ?? 0))} cortes ·
        pilha {order[`${prefix}PileQuantity`] ?? 0}
      </small>
    </span>
  );
}

function DatabaseOrderDetails({
  order,
  prefix,
  title,
}: {
  order: DbOrder;
  prefix: "order1" | "order2";
  title: string;
}) {
  const type = Number(order[`${prefix}SheetType`] ?? 0);
  const channel = prefix === "order1" ? 1 : 2;
  const measures = [1, 2, 3, 4, 5].map((index) =>
    Number(order[`${prefix}M${index}`] ?? 0),
  );
  return (
    <article className="order-channel-details">
      <div className="order-detail-heading">
        <div>
          <span className="order-title-with-level">{title}<OrderLevelMark levelSelector={order.levelSelector} channel={channel}/></span>
          <b>{String(order[`${prefix}Product`] ?? "") || "Sem produto"}</b>
        </div>
        <small>Cnj / OF {order[`${prefix}Id`] ?? "—"}</small>
      </div>
      <p>{String(order[`${prefix}Client`] ?? "") || "Cliente não informado"}</p>
      <div className="order-detail-facts">
        <span>
          Tipo da chapa<b>{sheetTypeNames[type] ?? `Tipo ${type}`}</b>
        </span>
        <span>
          Quantidade
          <b>
            {formatNumber(Number(order[`${prefix}SheetQuantity`] ?? 0))}{" "}
            chapa(s)
          </b>
        </span>
        <span>
          Comprimento
          <b>{formatNumber(Number(order[`${prefix}SheetLength`] ?? 0))} mm</b>
        </span>
        <span>
          Cortes
          <b>{formatNumber(Number(order[`${prefix}NumberOfCuts`] ?? 0))}</b>
        </span>
        <span>
          Tamanho da pilha
          <b>{formatNumber(Number(order[`${prefix}PileQuantity`] ?? 0))}</b>
        </span>
      </div>
      <div className="order-detail-measures">
        {activeMeasures(type, measures).map((measure, index) => (
          <span key={index}>
            M{index + 1}
            <b>{formatNumber(measure)} mm</b>
          </span>
        ))}
      </div>
    </article>
  );
}

function DatabaseOrderDetailsPanel({ order }: { order: DbOrder }) {
  const papers = [
    order.paper1,
    order.paper2,
    order.paper3,
    order.paper4,
    order.paper5,
  ];
  return (
    <div className="order-row-details" id={`order-details-${order.id}`}>
      <div className="order-paper-details">
        <div className="order-detail-facts">
          <span>
            Lista / boletim<b>{order.productionListNumber || "—"}</b>
          </span>
          <span>
            Sequência<b>{order.productionSequence ?? "—"}</b>
          </span>
          <span>
            Composição<b>{order.paperComposition || "—"}</b>
          </span>
          <span>
            Onda<b>{order.fluteType || "—"}</b>
          </span>
          <span>
            Largura do papel<b>{formatNumber(order.paperWidth)} mm</b>
          </span>
          <span>
            Seletor de nível
            <LevelSelectorMark value={order.levelSelector}/>
          </span>
        </div>
        <div className="order-paper-layers">
          {papers.map((paper, index) => (
            <span key={index}>
              Papel {index + 1}
              <b>{paper || "Não utilizado"}</b>
            </span>
          ))}
        </div>
      </div>
      <div
        className={`order-channel-details-grid ${order.levelSelector === 3 ? "" : "single"}`}
      >
        {isOrderChannelEnabled(order.levelSelector, 1) && (
          <DatabaseOrderDetails
            order={order}
            prefix="order1"
            title="Pedido 1"
          />
        )}
        {isOrderChannelEnabled(order.levelSelector, 2) && (
          <DatabaseOrderDetails
            order={order}
            prefix="order2"
            title="Pedido 2"
          />
        )}
      </div>
    </div>
  );
}

function OrdersScreen({ changeInProgress, nextOrderTableId }: { changeInProgress: boolean; nextOrderTableId: number }) {
  const { user } = useAuth();
  const canManageOrders =
    user.role === "Supervisor" || user.role === "Administrator";
  const canReorderOrders = user.role !== "Observer";
  const [status, setStatus] = useState<DatabaseStatus | null>(null),
    [orders, setOrders] = useState<DbOrder[]>([]),
    [editing, setEditing] = useState<DbOrder | null>(null),
    [importing, setImporting] = useState(false),
    [clearing, setClearing] = useState(false),
    [savingSequence, setSavingSequence] = useState(false),
    [swappingOrderId, setSwappingOrderId] = useState<number | null>(null),
    [sequenceDirty, setSequenceDirty] = useState(false),
    [draggingOrderId, setDraggingOrderId] = useState<number | null>(null),
    [error, setError] = useState(""),
    [searchQuery, setSearchQuery] = useState(""),
    [expandedOrderIds, setExpandedOrderIds] = useState<Set<number>>(
      () => new Set(),
    );
  const load = () => {
    fetch("/api/production-data/status")
      .then((r) => r.json())
      .then(setStatus);
    fetch("/api/production-data/orders")
      .then(async (r) => {
        if (!r.ok) throw new Error((await r.json()).detail);
        return r.json();
      })
      .then((loadedOrders: DbOrder[]) => {
        setOrders(loadedOrders);
        setSequenceDirty(false);
      })
      .catch((e) => setError(e.message));
  };
  // EN: Load the legacy order queue only when the screen opens.
  // PT: Carrega a fila de pedidos legada somente ao abrir a tela.
  // oxlint-disable-next-line react-hooks/exhaustive-deps
  useEffect(load, [changeInProgress]);
  const save = async () => {
    if (!editing) return;
    setError("");
    const method = editing.id ? "PUT" : "POST",
      url = editing.id
        ? `/api/production-data/orders/${editing.id}`
        : "/api/production-data/orders";
    const response = await fetch(url, {
      method,
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(editing),
    });
    if (!response.ok) {
      const body = await response.json();
      setError(body.error ?? body.detail ?? "Falha ao salvar");
      return;
    }
    setEditing(null);
    load();
  };
  const remove = async (id: number) => {
    if (!confirm("Deseja excluir este pedido da fila?")) return;
    setError("");
    const response = await fetch(`/api/production-data/orders/${id}`, {
      method: "DELETE",
    });
    if (!response.ok) {
      const body = await response.json();
      setError(body.error ?? body.detail ?? "Falha ao excluir");
      return;
    }
    load();
  };
  const swapLevels = async (order: DbOrder) => {
    if (!confirm(`Inverter os níveis da lista ${order.productionListNumber}? Os dados do Pedido 1 irão para o Pedido 2 (Superior), e os dados do Pedido 2 irão para o Pedido 1 (Inferior).`)) return;
    setSwappingOrderId(order.id);
    setError("");
    try {
      const response = await fetch(`/api/production-data/orders/${order.id}/swap-levels`, { method: "PUT" });
      const body = await response.json().catch(() => ({}));
      if (!response.ok) throw new Error(body.error ?? body.detail ?? "Falha ao inverter os níveis");
      if (!body.swapped) throw new Error("O pedido não está mais programado com os dois níveis.");
      load();
    } catch (exception) {
      setError(exception instanceof Error ? exception.message : "Falha ao inverter os níveis");
    } finally {
      setSwappingOrderId(null);
    }
  };
  const clearAll = async () => {
    if (
      !confirm(
        `Deseja remover os pedidos pendentes? O pedido atual, o próximo pedido já carregado no PLC e o histórico serão mantidos.`,
      )
    )
      return;
    setClearing(true);
    setError("");
    try {
      const response = await fetch("/api/production-data/orders", {
        method: "DELETE",
      });
      if (!response.ok) {
        const body = await response.json();
        throw new Error(body.error ?? body.detail ?? "Falha ao limpar a lista");
      }
      load();
    } catch (exception) {
      setError(
        exception instanceof Error
          ? exception.message
          : "Falha ao limpar a lista",
      );
    } finally {
      setClearing(false);
    }
  };
  const reorder = (sourceId: number, targetIndex: number) => {
    if (!canReorderOrders || changeInProgress || searchQuery.trim()) return;
    setOrders((previous) => {
      const sourceIndex = previous.findIndex((order) => order.id === sourceId);
      if (sourceIndex < 0 || targetIndex < 0 || targetIndex >= previous.length || sourceIndex === targetIndex)
        return previous;
      const reordered = [...previous];
      const [moved] = reordered.splice(sourceIndex, 1);
      reordered.splice(targetIndex, 0, moved);
      setSequenceDirty(true);
      return reordered.map((order, index) => ({ ...order, productionSequence: index + 1 }));
    });
  };
  const moveOrder = (id: number, direction: -1 | 1) => {
    const index = orders.findIndex((order) => order.id === id);
    reorder(id, index + direction);
  };
  const swapOrders = (sourceId: number, targetId: number) => {
    if (!canReorderOrders || changeInProgress || searchQuery.trim() || sourceId === targetId) return;
    setOrders((previous) => {
      const sourceIndex = previous.findIndex((order) => order.id === sourceId);
      const targetIndex = previous.findIndex((order) => order.id === targetId);
      if (sourceIndex < 0 || targetIndex < 0) return previous;
      const swapped = [...previous];
      [swapped[sourceIndex], swapped[targetIndex]] = [swapped[targetIndex], swapped[sourceIndex]];
      setSequenceDirty(true);
      return swapped.map((order, index) => ({ ...order, productionSequence: index + 1 }));
    });
  };
  const saveSequence = async () => {
    setSavingSequence(true);
    setError("");
    try {
      const response = await fetch("/api/production-data/orders/sequence", {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ orderedIds: orders.map((order) => order.id) }),
      });
      const body = await response.json().catch(() => ({}));
      if (!response.ok) throw new Error(body.error ?? body.detail ?? "Falha ao salvar a sequência");
      load();
    } catch (exception) {
      setError(exception instanceof Error ? exception.message : "Falha ao salvar a sequência");
    } finally {
      setSavingSequence(false);
    }
  };
  // EN: Search only the queue already loaded in memory, matching partial text without case sensitivity.
  // PT: Pesquisa apenas a fila já carregada em memória, aceitando trechos sem diferenciar maiúsculas e minúsculas.
  const filteredOrders = useMemo(() => {
    const query = searchQuery.trim().toLocaleLowerCase("pt-BR");
    if (!query) return orders;
    return orders.filter((order) =>
      [
        order.productionListNumber,
        order.productionSequence,
        order.order1Id,
        order.order2Id,
        order.order1Client,
        order.order2Client,
        order.order1Product,
        order.order2Product,
        order.paperComposition,
      ].some((value) =>
        String(value ?? "")
          .toLocaleLowerCase("pt-BR")
          .includes(query),
      ),
    );
  }, [orders, searchQuery]);
  const toggleDetails = (id: number) =>
    setExpandedOrderIds((previous) => {
      const next = new Set(previous);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  return (
    <>
      <div className="screen-title">
        <div>
          <p className="eyebrow">Cadastro</p>
          <h2>Pedidos de produção</h2>
        </div>
        {canManageOrders && (
          <div className="orders-toolbar">
            <button
              disabled={!status?.available}
              onClick={() => setImporting(true)}
            >
              Importar Trimbox
            </button>
            <button
              className="danger"
              disabled={!status?.available || orders.length === 0 || clearing}
              onClick={clearAll}
            >
              {clearing ? "Limpando…" : "Limpar lista"}
            </button>
            <button
              className="primary"
              disabled={!status?.available}
              onClick={() => setEditing({ ...emptyOrder })}
            >
              Novo pedido
            </button>
          </div>
        )}
      </div>
      <DatabaseBanner status={status} />
      {error && <div className="form-error">{error}</div>}
      {changeInProgress && (
        <div className="queue-change-lock">
          Troca automática em andamento. Aguarde para reorganizar a fila.
        </div>
      )}
      {sequenceDirty && !changeInProgress && (
        <div className="queue-sequence-actions">
          <span><b>Sequência alterada</b> Confira a nova ordem antes de salvar.</span>
          <div>
            <button disabled={savingSequence} onClick={load}>Cancelar</button>
            <button className="primary" disabled={savingSequence} onClick={saveSequence}>
              {savingSequence ? "Salvando…" : "Salvar sequência"}
            </button>
          </div>
        </div>
      )}
      {editing ? (
        <OrderForm
          order={editing}
          onChange={setEditing}
          onSave={save}
          onCancel={() => setEditing(null)}
        />
      ) : (
        <>
          <div className="orders-search">
            <label htmlFor="orders-search-input">
              Buscar na fila
              <input
                id="orders-search-input"
                type="search"
                value={searchQuery}
                onChange={(event) => setSearchQuery(event.target.value)}
                placeholder="Lista, Cnj, cliente, produto, composição ou sequência"
              />
            </label>
            <span>
              {filteredOrders.length} de {orders.length} pedido(s)
            </span>
          </div>
          <div className="data-table orders">
            <div className="table-head">
              <span>Seq.</span>
              <span>Lista / Papel</span>
              <span>Pedido 1</span>
              <span>Pedido 2</span>
              <span>Seletor de nível</span>
              <span>Ações</span>
            </div>
            {filteredOrders.map((order) => {
              const expanded = expandedOrderIds.has(order.id);
              return (
                <div
                  className={`orders-record ${expanded ? "expanded" : ""} ${draggingOrderId === order.id ? "dragging" : ""} ${nextOrderTableId > 0 && nextOrderTableId === order.id ? "next-order-programmed" : ""} ${order.historySavedAt ? "recovered-history-order" : ""}`}
                  key={order.id}
                  draggable={canReorderOrders && !changeInProgress && !searchQuery.trim()}
                  onDragStart={() => setDraggingOrderId(order.id)}
                  onDragEnd={() => setDraggingOrderId(null)}
                  onDragOver={(event) => {
                    if (draggingOrderId !== null) event.preventDefault();
                  }}
                  onDrop={(event) => {
                    event.preventDefault();
                    if (draggingOrderId !== null) swapOrders(draggingOrderId, order.id);
                    setDraggingOrderId(null);
                  }}
                >
                  <div className="table-row">
                    <span>{order.productionSequence ?? "—"}</span>
                    <span className="database-order">
                      <b>{order.productionListNumber}</b>
                      {nextOrderTableId > 0 && nextOrderTableId === order.id && <small className="next-order-programmed-mark">Próximo no PLC</small>}
                      {order.historySavedAt && <small className="recovered-history-mark">Recuperado do histórico</small>}
                      <small>
                        {order.paperWidth} mm · Onda {order.fluteType}
                      </small>
                      <small>{order.paperComposition}</small>
                      <small>
                        {[
                          order.paper1,
                          order.paper2,
                          order.paper3,
                          order.paper4,
                          order.paper5,
                        ]
                          .filter(Boolean)
                          .join(" / ")}
                      </small>
                    </span>
                    <DatabaseOrderSummary order={order} prefix="order1" />
                    <DatabaseOrderSummary order={order} prefix="order2" />
                    <span><LevelSelectorMark value={order.levelSelector}/></span>
                    <span className="row-actions">
                      {canReorderOrders && (
                        <span className="sequence-buttons">
                          <button
                            title="Subir pedido"
                            aria-label={`Subir pedido ${order.productionListNumber}`}
                            disabled={changeInProgress || Boolean(searchQuery.trim()) || orders[0]?.id === order.id}
                            onClick={() => moveOrder(order.id, -1)}
                          >↑</button>
                          <button
                            title="Descer pedido"
                            aria-label={`Descer pedido ${order.productionListNumber}`}
                            disabled={changeInProgress || Boolean(searchQuery.trim()) || orders.at(-1)?.id === order.id}
                            onClick={() => moveOrder(order.id, 1)}
                          >↓</button>
                        </span>
                      )}
                      <button
                        aria-expanded={expanded}
                        aria-controls={`order-details-${order.id}`}
                        onClick={() => toggleDetails(order.id)}
                      >
                        {expanded ? "Recolher" : "Ver detalhes"}
                      </button>
                      {canManageOrders && (
                        <button onClick={() => setEditing({ ...order })}>
                          Editar
                        </button>
                      )}
                      {canManageOrders && order.levelSelector === 3 && (
                        <button
                          disabled={changeInProgress || sequenceDirty || swappingOrderId === order.id}
                          onClick={() => swapLevels(order)}
                        >
                          {swappingOrderId === order.id ? "Invertendo…" : "Inverter níveis"}
                        </button>
                      )}
                      {canManageOrders && (
                        <button onClick={() => remove(order.id)}>
                          Excluir
                        </button>
                      )}
                    </span>
                  </div>
                  {expanded && <DatabaseOrderDetailsPanel order={order} />}
                </div>
              );
            })}
            {orders.length === 0 ? (
              <div className="empty-row">Nenhum pedido disponível.</div>
            ) : (
              filteredOrders.length === 0 && (
                <div className="empty-row search-empty">
                  <b>Nenhum pedido encontrado.</b>
                  <span>
                    Tente buscar por outro termo ou limpe o campo de busca.
                  </span>
                </div>
              )
            )}
          </div>
        </>
      )}
      {importing && (
        <div
          className="trimbox-modal-backdrop"
          role="dialog"
          aria-modal="true"
          aria-label="Importar pedidos Trimbox"
        >
          <div className="trimbox-modal">
            <TrimboxImportScreen
              onClose={() => setImporting(false)}
              onImported={load}
            />
          </div>
        </div>
      )}
    </>
  );
}

function DiagnosticsScreen({ order }: { order: PlcOrder }) {
  const g = order.generatedOrder,
    tools = (title: string, items: ToolReference[], bad: number[]) => (
      <article className="tool-card">
        <div className="card-heading">
          <h3>{title}</h3>
          <span>{items.filter((x) => x.enabled).length} ativas</span>
        </div>
        <div className="tool-list">
          {items
            .filter((x) => x.enabled)
            .map((x) => (
              <div
                className={bad.includes(x.index) ? "invalid" : ""}
                key={x.index}
              >
                <span>
                  {title.slice(0, -1)} {x.index}
                </span>
                <b>{formatNumber(x.positionReferenceMm, 2)} mm</b>
                <i>{bad.includes(x.index) ? "Fora de faixa" : "OK"}</i>
              </div>
            ))}
        </div>
      </article>
    );
  return (
    <>
      <div className="screen-title">
        <div>
          <p className="eyebrow">Suporte técnico</p>
          <h2>Diagnóstico das posições geradas</h2>
        </div>
      </div>
      <section className="generated-summary">
        <div>
          <span>Largura pedido 1</span>
          <b>{g.order1Width} mm</b>
        </div>
        <div>
          <span>Largura pedido 2</span>
          <b>{g.order2Width} mm</b>
        </div>
        <div>
          <span>Largura total</span>
          <b>{g.orderTotalWidth} mm</b>
        </div>
        <div>
          <span>Status</span>
          <b>{g.orderNotOk ? "Verificar" : "Pedido válido"}</b>
        </div>
      </section>
      <section className="tools">
        {tools("Facas", g.knives, g.knivesOutOfRange)}
        {tools("Vincos", g.scorers, g.scorersOutOfRange)}
      </section>
    </>
  );
}

function AuthenticatedApp() {
  const { user, logout } = useAuth();
  const snapshot = usePlcMonitor(),
    [page, setPage] = useState<Page>("production"),
    [sidebarCollapsed, setSidebarCollapsed] = useState(() => {
      const storedValue = window.localStorage.getItem(
        "dry-end-sidebar-collapsed",
      );
      return storedValue === null
        ? window.matchMedia("(max-width: 720px)").matches
        : storedValue === "true";
    }),
    current = snapshot.data?.currentOrder,
    next = snapshot.data?.nextOrder,
    online = snapshot.state === "Online";
  const toggleSidebar = () =>
    setSidebarCollapsed((previous) => {
      const nextValue = !previous;
      window.localStorage.setItem(
        "dry-end-sidebar-collapsed",
        String(nextValue),
      );
      return nextValue;
    });
  const collapseSidebar = () => {
    if (sidebarCollapsed) return;
    window.localStorage.setItem("dry-end-sidebar-collapsed", "true");
    setSidebarCollapsed(true);
  };

  useEffect(() => {
    const collapseOnEscape = (event: KeyboardEvent) => {
      if (event.key === "Escape") collapseSidebar();
    };
    window.addEventListener("keydown", collapseOnEscape);
    return () => window.removeEventListener("keydown", collapseOnEscape);
  }, [sidebarCollapsed]);

  return (
    <div className={`app-shell ${sidebarCollapsed ? "sidebar-collapsed" : ""}`}>
      <SideMenu
        currentPage={page}
        items={navigationItems.filter(
          (item) => item.id !== "users" || user.role === "Administrator",
        )}
        collapsed={sidebarCollapsed}
        online={online}
        statusLabel={snapshot.state}
        onSelect={setPage}
        onToggle={toggleSidebar}
      />
      <main onPointerDown={collapseSidebar}>
        <header className="top-bar">
          <div>
            <span>Velocidade da linha</span>
            <b>
              {current ? `${formatNumber(current.lineSpeed, 1)} m/min` : "—"}
            </b>
          </div>
          <div>
            <span>Pedido atual</span>
            <b>{current?.productionListNumber ?? "—"}</b>
          </div>
          <div>
            <span>Data e hora</span>
            <b>{new Date().toLocaleString("pt-BR")}</b>
          </div>
          <div className="current-user">
            <span>{roleLabel[user.role]}</span>
            <b>{user.displayName}</b>
            <button onClick={() => void logout()}>Sair</button>
          </div>
        </header>
        <div className="screen-content">
          {page === "production" &&
            (!current || !next ? (
              <section className="loading">
                <b>Aguardando dados válidos do PLC</b>
                <span>{snapshot.lastError}</span>
              </section>
            ) : (
              <ProductionScreen current={current} next={next} />
            ))}{" "}
          {page === "orders" && <OrdersScreen changeInProgress={current?.changeOrderRequest ?? false} nextOrderTableId={next?.tableId ?? 0} />}{" "}
          {page === "history" && <HistoryScreen />}{" "}
          {page === "graphs" && <MetricsScreen />}{" "}
          {page === "users" && user.role === "Administrator" && <UsersScreen />}{" "}
          {page === "diagnostics" &&
            (!current ? (
              <section className="loading">
                <b>Aguardando dados válidos do PLC</b>
                <span>{snapshot.lastError}</span>
              </section>
            ) : (
              <DiagnosticsScreen order={current} />
            ))}
        </div>
      </main>
    </div>
  );
}
function App() {
  return (
    <AuthProvider>
      <AuthenticatedApp />
    </AuthProvider>
  );
}
export default App;
