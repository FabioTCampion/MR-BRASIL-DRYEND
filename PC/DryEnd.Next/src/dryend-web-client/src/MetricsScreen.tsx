import { useEffect, useMemo, useState } from "react";
import InteractiveChart, {
  type InteractiveChartOption,
} from "./InteractiveChart";
import { useAuth } from "./Auth";
import "./MetricsScreen.css";

type MetricView =
  "speed" | "hourlyAverage" | "hourlyStopped" | "stopDurations" | "stops";

type SpeedRecord = {
  dateTime: string;
  machineSpeed: number;
};

type StopEvent = {
  start: Date;
  end: Date;
  durationMinutes: number;
};

type StopReason = {
  code: number;
  category: string;
  description: string;
  isActive: boolean;
};
type ProductionStop = {
  id: number;
  startedAt: string;
  finishedAt: string | null;
  reasonCode: number | null;
  reasonDescription: string | null;
  category: string | null;
  observation: string | null;
  justifiedBy: string | null;
  justifiedAt: string | null;
  currentTableId: number | null;
  productionListNumber: number | null;
};

const STOP_SPEED_THRESHOLD_MPM = 10;
const DEFAULT_SAMPLE_INTERVAL_MILLISECONDS = 30_000;
const MAX_INTERVAL_USED_FOR_CADENCE_MILLISECONDS = 5 * 60_000;
const MAX_STOPS_TO_SHOW = 60;
const HOURS = Array.from({ length: 24 }, (_, index) => index);

const today = () => {
  const now = new Date();
  const local = new Date(now.getTime() - now.getTimezoneOffset() * 60_000);
  return local.toISOString().slice(0, 10);
};

const formatNumber = (value: number, digits = 0) =>
  value.toLocaleString("pt-BR", {
    minimumFractionDigits: digits,
    maximumFractionDigits: digits,
  });

const formatTime = (date: Date) =>
  date.toLocaleTimeString("pt-BR", { hour: "2-digit", minute: "2-digit" });

function inferSampleIntervalMilliseconds(rows: SpeedRecord[]): number {
  const occurrences = new Map<number, number>();
  for (let index = 1; index < rows.length; index += 1) {
    const interval =
      new Date(rows[index].dateTime).getTime() -
      new Date(rows[index - 1].dateTime).getTime();
    if (interval <= 0 || interval > MAX_INTERVAL_USED_FOR_CADENCE_MILLISECONDS)
      continue;
    occurrences.set(interval, (occurrences.get(interval) ?? 0) + 1);
  }

  let selectedInterval = DEFAULT_SAMPLE_INTERVAL_MILLISECONDS;
  let selectedCount = 0;
  occurrences.forEach((count, interval) => {
    if (
      count > selectedCount ||
      (count === selectedCount && interval < selectedInterval)
    ) {
      selectedInterval = interval;
      selectedCount = count;
    }
  });
  return selectedInterval;
}

function buildStopEvents(
  rows: SpeedRecord[],
  sampleIntervalMilliseconds: number,
): StopEvent[] {
  const stops: StopEvent[] = [];
  let current: StopEvent | null = null;
  const sampleIntervalMinutes = sampleIntervalMilliseconds / 60_000;

  rows.forEach((row) => {
    const dateTime = new Date(row.dateTime);
    if (row.machineSpeed < STOP_SPEED_THRESHOLD_MPM) {
      const sampleEnd = new Date(
        dateTime.getTime() + sampleIntervalMilliseconds,
      );
      if (current === null || dateTime.getTime() > current.end.getTime()) {
        if (current !== null) stops.push(current);
        current = {
          start: dateTime,
          end: sampleEnd,
          durationMinutes: sampleIntervalMinutes,
        };
      } else {
        current.end = sampleEnd;
        current.durationMinutes += sampleIntervalMinutes;
      }
      return;
    }

    if (current !== null) {
      stops.push(current);
      current = null;
    }
  });

  if (current !== null) stops.push(current);
  return stops;
}

function SpeedLineChart({ rows }: { rows: SpeedRecord[] }) {
  const speeds = rows.map((row) => row.machineSpeed);
  const average = speeds.length
    ? speeds.reduce((sum, value) => sum + value, 0) / speeds.length
    : 0;
  const maximum = Math.max(1, ...speeds);
  const stopEvents = buildStopEvents(
    rows,
    inferSampleIntervalMilliseconds(rows),
  );
  const option: InteractiveChartOption = {
    backgroundColor: "transparent",
    animation: false,
    grid: { left: 62, right: 24, top: 28, bottom: 72 },
    tooltip: {
      trigger: "axis",
      axisPointer: { type: "cross" },
      valueFormatter: (value: unknown) =>
        `${formatNumber(Number(value), 1)} m/min`,
    },
    xAxis: {
      type: "time",
      axisLabel: { hideOverlap: true },
      splitLine: { show: false },
    },
    yAxis: {
      type: "value",
      min: 0,
      name: "m/min",
      max: Math.ceil(maximum / 10) * 10,
      splitLine: { lineStyle: { color: "#26364d" } },
    },
    dataZoom: [
      { type: "inside", zoomOnMouseWheel: true, moveOnMouseMove: true },
      {
        type: "slider",
        height: 24,
        bottom: 16,
        borderColor: "#34445c",
        fillerColor: "#387bc755",
      },
    ],
    series: [
      {
        type: "line",
        name: "Velocidade",
        showSymbol: false,
        sampling: "lttb",
        data: rows.map((row) => [row.dateTime, row.machineSpeed]),
        lineStyle: { width: 2, color: "#55a2ff" },
        areaStyle: { color: "#245fa133" },
        markLine: {
          symbol: "none",
          data: [
            {
              yAxis: STOP_SPEED_THRESHOLD_MPM,
              name: "Parada",
              lineStyle: { color: "#e46b62" },
            },
            {
              yAxis: average,
              name: "Média",
              lineStyle: { color: "#f0a45d", type: "dashed" },
            },
            {
              yAxis: maximum,
              name: "Máxima",
              lineStyle: { color: "#79b6ff", type: "dotted" },
            },
          ],
        },
        markArea: {
          silent: true,
          itemStyle: { color: "#dc55451c" },
          data: stopEvents.map((stop) => [
            { xAxis: stop.start },
            { xAxis: stop.end },
          ]),
        },
      },
    ],
  };
  return (
    <InteractiveChart
      option={option}
      ariaLabel="Velocidade da linha ao longo do dia com zoom e navegação"
    />
  );
}

function HourlyBarChart({
  values,
  unit,
  valueDigits = 0,
  fixedMaximum,
}: {
  values: number[];
  unit: string;
  valueDigits?: number;
  fixedMaximum?: number;
}) {
  const maximum = fixedMaximum ?? Math.max(1, ...values);
  const option: InteractiveChartOption = {
    backgroundColor: "transparent",
    animationDuration: 250,
    grid: { left: 62, right: 24, top: 28, bottom: 70 },
    tooltip: {
      trigger: "axis",
      axisPointer: { type: "shadow" },
      valueFormatter: (value: unknown) =>
        `${formatNumber(Number(value), valueDigits)} ${unit}`,
    },
    xAxis: {
      type: "category",
      data: HOURS.map((hour) => `${hour.toString().padStart(2, "0")}h`),
      axisLabel: { interval: 1 },
    },
    yAxis: {
      type: "value",
      min: 0,
      max: maximum,
      name: unit,
      splitLine: { lineStyle: { color: "#26364d" } },
    },
    dataZoom: [{ type: "inside" }, { type: "slider", height: 22, bottom: 16 }],
    series: [
      {
        type: "bar",
        name: unit,
        data: values,
        barMaxWidth: 28,
        itemStyle: { color: "#4d91e6", borderRadius: [5, 5, 0, 0] },
        emphasis: { itemStyle: { color: "#75b2ff" } },
      },
    ],
  };
  return (
    <InteractiveChart
      option={option}
      ariaLabel={`Indicadores por hora em ${unit}`}
    />
  );
}

function StopDurationChart({ stops }: { stops: StopEvent[] }) {
  const maximum = Math.max(1, ...stops.map((stop) => stop.durationMinutes));
  const option: InteractiveChartOption = {
    backgroundColor: "transparent",
    grid: { left: 62, right: 24, top: 28, bottom: 72 },
    tooltip: {
      trigger: "axis",
      axisPointer: { type: "shadow" },
      valueFormatter: (value: unknown) =>
        `${formatNumber(Number(value), 1)} min`,
    },
    xAxis: {
      type: "category",
      data: stops.map(
        (stop, index) => `#${index + 1} · ${formatTime(stop.start)}`,
      ),
      axisLabel: { hideOverlap: true, rotate: stops.length > 20 ? 35 : 0 },
    },
    yAxis: {
      type: "value",
      min: 0,
      max: Math.ceil(maximum),
      name: "min",
      splitLine: { lineStyle: { color: "#26364d" } },
    },
    dataZoom: [
      { type: "inside" },
      {
        type: "slider",
        height: 22,
        bottom: 16,
        start:
          stops.length > 20 ? Math.max(0, 100 - (20 / stops.length) * 100) : 0,
      },
    ],
    series: [
      {
        type: "bar",
        name: "Duração",
        data: stops.map((stop) => stop.durationMinutes),
        barMaxWidth: 26,
        itemStyle: { color: "#df765d", borderRadius: [5, 5, 0, 0] },
        emphasis: { itemStyle: { color: "#ff9a7d" } },
      },
    ],
  };
  return (
    <InteractiveChart
      option={option}
      ariaLabel="Duração de cada parada com zoom e navegação"
    />
  );
}

export default function MetricsScreen() {
  const { user } = useAuth();
  const canJustify = user.role !== "Observer";
  const [date, setDate] = useState(today());
  const [rows, setRows] = useState<SpeedRecord[]>([]);
  const [view, setView] = useState<MetricView>("speed");
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");
  const [stops, setStops] = useState<ProductionStop[]>([]);
  const [reasons, setReasons] = useState<StopReason[]>([]);
  const [selectedStop, setSelectedStop] = useState<ProductionStop | null>(null);
  const [reasonCode, setReasonCode] = useState("");
  const [observation, setObservation] = useState("");
  const [saving, setSaving] = useState(false);

  const load = async () => {
    setLoading(true);
    setError("");
    try {
      const [response, stopsResponse, reasonsResponse] = await Promise.all([
        fetch(
          `/api/production-data/machine-speed?date=${encodeURIComponent(date)}`,
        ),
        fetch(`/api/production-data/stops?date=${encodeURIComponent(date)}`),
        fetch("/api/production-data/stop-reasons"),
      ]);
      if (!response.ok || !stopsResponse.ok || !reasonsResponse.ok) {
        const failedResponse = [response, stopsResponse, reasonsResponse].find(
          (item) => !item.ok,
        )!;
        const body = (await failedResponse.json().catch(() => null)) as {
          detail?: string;
          error?: string;
        } | null;
        throw new Error(
          body?.detail ??
            body?.error ??
            "Não foi possível carregar as métricas.",
        );
      }
      const data = (await response.json()) as SpeedRecord[];
      setRows(
        data
          .slice()
          .sort(
            (left, right) =>
              new Date(left.dateTime).getTime() -
              new Date(right.dateTime).getTime(),
          ),
      );
      setStops((await stopsResponse.json()) as ProductionStop[]);
      setReasons((await reasonsResponse.json()) as StopReason[]);
    } catch (exception) {
      setRows([]);
      setStops([]);
      setError(
        exception instanceof Error
          ? exception.message
          : "Não foi possível carregar as métricas.",
      );
    } finally {
      setLoading(false);
    }
  };

  // EN: Load the selected production day when the metrics page opens.
  // PT: Carrega o dia de produção selecionado quando a página de métricas é aberta.
  // oxlint-disable-next-line react-hooks/exhaustive-deps
  useEffect(() => {
    void load();
  }, []);

  const summary = useMemo(() => {
    const speeds = rows.map((row) => row.machineSpeed);
    const average = speeds.length
      ? speeds.reduce((sum, speed) => sum + speed, 0) / speeds.length
      : 0;
    const sampleIntervalMilliseconds = inferSampleIntervalMilliseconds(rows);
    const sampleIntervalMinutes = sampleIntervalMilliseconds / 60_000;
    const stoppedMinutes =
      rows.filter((row) => row.machineSpeed < STOP_SPEED_THRESHOLD_MPM).length *
      sampleIntervalMinutes;
    const stops = buildStopEvents(rows, sampleIntervalMilliseconds);
    return {
      average,
      maximum: Math.max(0, ...speeds),
      stoppedMinutes,
      stops,
      sampleIntervalSeconds: sampleIntervalMilliseconds / 1000,
      sampleIntervalMinutes,
    };
  }, [rows]);

  const hourlyAverage = useMemo(
    () =>
      HOURS.map((hour) => {
        const samples = rows.filter(
          (row) => new Date(row.dateTime).getHours() === hour,
        );
        return samples.length
          ? samples.reduce((sum, row) => sum + row.machineSpeed, 0) /
              samples.length
          : 0;
      }),
    [rows],
  );

  const hourlyStopped = useMemo(
    () =>
      HOURS.map((hour) => {
        const minutes =
          rows.filter(
            (row) =>
              new Date(row.dateTime).getHours() === hour &&
              row.machineSpeed < STOP_SPEED_THRESHOLD_MPM,
          ).length * summary.sampleIntervalMinutes;
        return Math.min(60, minutes);
      }),
    [rows, summary.sampleIntervalMinutes],
  );

  const visibleStops = useMemo(() => {
    if (summary.stops.length <= MAX_STOPS_TO_SHOW) return summary.stops;
    return summary.stops
      .slice()
      .sort((left, right) => right.durationMinutes - left.durationMinutes)
      .slice(0, MAX_STOPS_TO_SHOW)
      .sort((left, right) => left.start.getTime() - right.start.getTime());
  }, [summary.stops]);
  const pendingStopsForSelectedDay = useMemo(
    () =>
      stops.filter(
        (stop) => stop.finishedAt !== null && stop.reasonCode === null,
      ).length,
    [stops],
  );

  const groupedReasons = useMemo(
    () =>
      reasons.reduce((groups, reason) => {
        const items = groups.get(reason.category) ?? [];
        items.push(reason);
        groups.set(reason.category, items);
        return groups;
      }, new Map<string, StopReason[]>()),
    [reasons],
  );
  const stopDuration = (stop: ProductionStop) => {
    const end = stop.finishedAt
      ? new Date(stop.finishedAt).getTime()
      : Date.now();
    return Math.max(0, (end - new Date(stop.startedAt).getTime()) / 60_000);
  };
  const openJustification = (stop: ProductionStop) => {
    setSelectedStop(stop);
    setReasonCode(stop.reasonCode?.toString() ?? "");
    setObservation(stop.observation ?? "");
  };
  const saveJustification = async () => {
    if (!selectedStop || !reasonCode) return;
    setSaving(true);
    setError("");
    try {
      const response = await fetch(
        `/api/production-data/stops/${selectedStop.id}/justification`,
        {
          method: "PUT",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({
            reasonCode: Number(reasonCode),
            observation: observation.trim(),
            justifiedBy: "",
          }),
        },
      );
      if (!response.ok) {
        const body = (await response.json().catch(() => null)) as {
          detail?: string;
          error?: string;
        } | null;
        throw new Error(
          body?.detail ??
            body?.error ??
            "Não foi possível salvar a justificativa.",
        );
      }
      setSelectedStop(null);
      await load();
      window.dispatchEvent(new Event("stop-pending-changed"));
    } catch (exception) {
      setError(
        exception instanceof Error
          ? exception.message
          : "Não foi possível salvar a justificativa.",
      );
    } finally {
      setSaving(false);
    }
  };

  const viewTitle: Record<MetricView, string> = {
    speed: "Velocidade da linha",
    hourlyAverage: "Velocidade média por hora",
    hourlyStopped: "Tempo de máquina parada por hora",
    stopDurations: "Paradas: quantidade e duração",
    stops: "Tabela de paradas",
  };

  return (
    <section className="metrics-screen">
      <div className="metrics-heading">
        <div>
          <p className="metrics-eyebrow">Produção</p>
          <h2>Métricas da máquina</h2>
          <span>
            Indicadores calculados a partir do histórico de velocidade.
          </span>
        </div>
        <div className="metrics-date-action">
          <label htmlFor="metrics-date">Data</label>
          <input
            id="metrics-date"
            type="date"
            value={date}
            onChange={(event) => setDate(event.target.value)}
          />
          <button type="button" onClick={() => void load()} disabled={loading}>
            {loading ? "Carregando…" : "Carregar"}
          </button>
        </div>
      </div>

      {error && (
        <div className="metrics-error" role="alert">
          {error}
        </div>
      )}

      <div className="metrics-summary">
        <div>
          <span>Velocidade média</span>
          <b>{formatNumber(summary.average, 1)} m/min</b>
        </div>
        <div>
          <span>Velocidade máxima</span>
          <b>{formatNumber(summary.maximum)} m/min</b>
        </div>
        <div>
          <span>Tempo parado</span>
          <b>{formatNumber(summary.stoppedMinutes, 1)} min</b>
          <small>Abaixo de {STOP_SPEED_THRESHOLD_MPM} m/min</small>
        </div>
        <div>
          <span>Quantidade de paradas</span>
          <b>{formatNumber(summary.stops.length)}</b>
          <small>
            {formatNumber(rows.length)} amostras · ciclo de{" "}
            {formatNumber(summary.sampleIntervalSeconds)} s
          </small>
        </div>
      </div>

      <div className="metrics-workspace">
        <nav className="metrics-navigation" aria-label="Tipos de gráfico">
          <button
            className={view === "speed" ? "active" : ""}
            onClick={() => setView("speed")}
          >
            Velocidade da linha
          </button>
          <button
            className={view === "hourlyAverage" ? "active" : ""}
            onClick={() => setView("hourlyAverage")}
          >
            Velocidade média H/H
          </button>
          <button
            className={view === "hourlyStopped" ? "active" : ""}
            onClick={() => setView("hourlyStopped")}
          >
            Tempo de parada H/H
          </button>
          <button
            className={view === "stopDurations" ? "active" : ""}
            onClick={() => setView("stopDurations")}
          >
            Tempo de cada parada
          </button>
          <button
            className={view === "stops" ? "active" : ""}
            onClick={() => setView("stops")}
          >
            Tabela de paradas
            {pendingStopsForSelectedDay > 0 && (
              <span className="stop-view-badge">
                {pendingStopsForSelectedDay > 99
                  ? "99+"
                  : pendingStopsForSelectedDay}
              </span>
            )}
          </button>
        </nav>

        <article className="metrics-chart-card">
          <div className="metrics-chart-title">
            <div>
              <h3>{viewTitle[view]}</h3>
              <span>
                {new Date(`${date}T12:00:00`).toLocaleDateString("pt-BR")}
              </span>
            </div>
            {view === "speed" && (
              <div className="metrics-legend">
                <i className="speed" />
                Velocidade
                <i className="average" />
                Média
                <i className="maximum" />
                Máximo
              </div>
            )}
            {view !== "stops" && (
              <div className="metrics-interaction-hint">
                <b>Interativo</b>
                <span>Role para zoom · arraste para navegar</span>
              </div>
            )}
          </div>
          {loading ? (
            <div className="metrics-empty">Carregando dados…</div>
          ) : view !== "stops" && rows.length === 0 ? (
            <div className="metrics-empty">
              Sem dados para o período selecionado.
            </div>
          ) : (
            <>
              {view === "speed" && <SpeedLineChart rows={rows} />}
              {view === "hourlyAverage" && (
                <HourlyBarChart
                  values={hourlyAverage}
                  unit="m/min"
                  valueDigits={1}
                />
              )}
              {view === "hourlyStopped" && (
                <HourlyBarChart
                  values={hourlyStopped}
                  unit="min"
                  valueDigits={1}
                  fixedMaximum={60}
                />
              )}
              {view === "stopDurations" &&
                (visibleStops.length ? (
                  <StopDurationChart stops={visibleStops} />
                ) : (
                  <div className="metrics-empty">
                    Nenhuma parada encontrada.
                  </div>
                ))}
              {view === "stops" && (
                <div className="stop-table">
                  <div className="stop-table-head">
                    <span>Início</span>
                    <span>Fim</span>
                    <span>Duração</span>
                    <span>Motivo</span>
                    <span>Operador</span>
                    <span>Ação</span>
                  </div>
                  {stops.map((stop) => (
                    <div className="stop-table-row" key={stop.id}>
                      <span>
                        {new Date(stop.startedAt).toLocaleTimeString("pt-BR")}
                      </span>
                      <span>
                        {stop.finishedAt
                          ? new Date(stop.finishedAt).toLocaleTimeString(
                              "pt-BR",
                            )
                          : "Em andamento"}
                      </span>
                      <span>{formatNumber(stopDuration(stop), 1)} min</span>
                      <span>
                        {stop.reasonCode ? (
                          `${stop.reasonCode} - ${stop.reasonDescription}`
                        ) : (
                          <b className="stop-pending">Pendente</b>
                        )}
                      </span>
                      <span>{stop.justifiedBy ?? "—"}</span>
                      <span>
                        {canJustify ? (
                          <button
                            type="button"
                            disabled={!stop.finishedAt}
                            onClick={() => openJustification(stop)}
                          >
                            {stop.reasonCode ? "Editar" : "Justificar"}
                          </button>
                        ) : (
                          "Somente leitura"
                        )}
                      </span>
                    </div>
                  ))}
                  {stops.length === 0 && (
                    <div className="metrics-empty">
                      Nenhuma parada registrada neste dia.
                    </div>
                  )}
                </div>
              )}
            </>
          )}
          {view === "stopDurations" &&
            summary.stops.length > MAX_STOPS_TO_SHOW && (
              <p className="metrics-note">
                Exibindo as {MAX_STOPS_TO_SHOW} paradas de maior duração, em
                ordem cronológica.
              </p>
            )}
        </article>
      </div>
      {selectedStop && (
        <div
          className="stop-modal-backdrop"
          role="dialog"
          aria-modal="true"
          aria-label="Justificar parada"
        >
          <form
            className="stop-modal"
            onSubmit={(event) => {
              event.preventDefault();
              void saveJustification();
            }}
          >
            <h3>Justificar parada</h3>
            <p>
              {new Date(selectedStop.startedAt).toLocaleString("pt-BR")} ·{" "}
              {formatNumber(stopDuration(selectedStop), 1)} min · usuário{" "}
              {user.displayName}
            </p>
            <label>
              Motivo
              <select
                required
                value={reasonCode}
                onChange={(event) => setReasonCode(event.target.value)}
              >
                <option value="">Selecione o motivo</option>
                {Array.from(groupedReasons.entries()).map(
                  ([category, items]) => (
                    <optgroup key={category} label={category}>
                      {items.map((reason) => (
                        <option key={reason.code} value={reason.code}>
                          {reason.code} - {reason.description}
                        </option>
                      ))}
                    </optgroup>
                  ),
                )}
              </select>
            </label>
            <label>
              Observação
              <textarea
                value={observation}
                onChange={(event) => setObservation(event.target.value)}
                rows={4}
                placeholder="Observação opcional"
              />
            </label>
            <div className="stop-modal-actions">
              <button type="button" onClick={() => setSelectedStop(null)}>
                Cancelar
              </button>
              <button className="primary" type="submit" disabled={saving}>
                {saving ? "Salvando…" : "Salvar"}
              </button>
            </div>
          </form>
        </div>
      )}
    </section>
  );
}
