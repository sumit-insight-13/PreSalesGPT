import * as s from "../styles";

interface Props {
  approvedRequirementCount: number;
  hasSections: boolean;
  generating: boolean;
  checking: boolean;
  onGenerate: () => Promise<void>;
  onRunCoverage: () => Promise<void>;
  onExport: () => void;
}

export function ProposalActions({
  approvedRequirementCount,
  hasSections,
  generating,
  checking,
  onGenerate,
  onRunCoverage,
  onExport,
}: Props) {
  const ready = approvedRequirementCount > 0;

  return (
    <div
      style={{
        ...s.card,
        background: s.colours.blueSoft,
        border: `1px solid rgba(77,124,254,0.26)`,
      }}
    >
      <h2 style={s.heading}>Generate Proposal</h2>

      <p style={s.row}>
        Only approved requirements are used. Anything a reviewer has
        not approved is left out of the draft.
      </p>

      {ready ? (
        <p style={{ ...s.row, color: s.colours.green }}>
          {approvedRequirementCount} approved requirement(s) ready.
        </p>
      ) : (
        <p style={{ ...s.row, color: s.colours.red }}>
          Approve at least one requirement first.
        </p>
      )}

      <button
        onClick={onGenerate}
        disabled={generating || !ready}
        data-busy={generating}
        style={{
          ...s.button,
          background: ready ? s.colours.purple : s.colours.grey,
          boxShadow: ready
            ? "0 6px 18px rgba(155,124,246,0.26)"
            : "none",
        }}
      >
        {generating
          ? "AI is writing the proposal... (1-3 min)"
          : "Generate Proposal (AI)"}
      </button>

      {hasSections && (
        <>
          <button
            onClick={onRunCoverage}
            disabled={checking}
            data-busy={checking}
            style={{
              ...s.button,
              marginLeft: "10px",
              background: s.colours.amber,
              color: "#2B1A05",
              boxShadow: "0 6px 18px rgba(240,167,66,0.22)",
            }}
          >
            {checking ? "Checking..." : "Run Coverage Check"}
          </button>

          <button
            onClick={onExport}
            style={{
              ...s.button,
              marginLeft: "10px",
              background: s.colours.green,
              color: "#06281B",
              boxShadow: "0 6px 18px rgba(46,204,143,0.24)",
            }}
          >
            Export Proposal
          </button>
        </>
      )}
    </div>
  );
}
