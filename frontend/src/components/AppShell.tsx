import type { ReactNode } from "react";

interface Stage {
  label: string;
  count: number;
  done: boolean;
}

interface Props {
  opportunityName: string;
  customerName: string;
  industry: string;
  stages: Stage[];
  children: ReactNode;
}

/**
 * Application chrome: sidebar, header and progress rail.
 *
 * The rail is derived from the data rather than tracked as state.
 * A stage is complete when it has produced something, so the rail
 * always reflects reality rather than an intention.
 */
export function AppShell({
  opportunityName,
  customerName,
  industry,
  stages,
  children,
}: Props) {
  const currentIndex = stages.findIndex((stage) => !stage.done);

  const completed = stages.filter((stage) => stage.done).length;

  return (
    <div className="shell">
      <aside className="sidebar">
        <div className="brand">
          <div className="brand-mark">PG</div>

          <div>
            <div className="brand-name">PresalesGPT</div>
            <div className="brand-sub">
              RFP to proposal workspace
            </div>
          </div>
        </div>

        <nav className="nav">
          <div className="nav-label">WORKFLOW</div>

          {stages.map((stage, index) => {
            const isCurrent = index === currentIndex;

            const className = [
              "nav-item",
              isCurrent ? "is-active" : "",
              stage.done ? "is-done" : "",
            ]
              .filter(Boolean)
              .join(" ");

            return (
              <div key={stage.label} className={className}>
                <span>{stage.label}</span>

                {stage.done ? (
                  stage.count > 0 ? (
                    <span className="nav-count">
                      {stage.count}
                    </span>
                  ) : (
                    <span className="nav-tick">✓</span>
                  )
                ) : null}
              </div>
            );
          })}
        </nav>

        <div className="sidebar-foot">
          <p>
            Every AI step is followed by a human gate. Nothing
            reaches a customer without review.
          </p>
        </div>
      </aside>

      <main>
        <header className="topbar">
          <div>
            <div className="crumb">
              Opportunities ·{" "}
              <strong>
                {opportunityName || "None selected"}
              </strong>
            </div>
          </div>

          <div className="chips">
            {customerName && (
              <span className="chip">
                <span className="chip-dot" />
                {customerName}
              </span>
            )}

            {industry && <span className="chip">{industry}</span>}

            <span className="chip">
              {completed} of {stages.length} stages
            </span>
          </div>
        </header>

        <div className="rail">
          {stages.map((stage, index) => {
            const isCurrent = index === currentIndex;

            const className = [
              "step",
              stage.done ? "is-done" : "",
              isCurrent ? "is-current" : "",
            ]
              .filter(Boolean)
              .join(" ");

            return (
              <div
                key={stage.label}
                style={{ display: "contents" }}
              >
                {index > 0 && <div className="step-join" />}

                <div className={className}>
                  <span className="step-num">
                    {stage.done ? "✓" : index + 1}
                  </span>
                  {stage.label}
                </div>
              </div>
            );
          })}
        </div>

        <div className="work">{children}</div>
      </main>
    </div>
  );
}
