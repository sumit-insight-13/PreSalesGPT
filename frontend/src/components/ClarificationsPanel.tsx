import { useState } from "react";
import type { ClarificationItem } from "../types";
import * as s from "../styles";

interface Props {
  clarifications: ClarificationItem[];
  hasDocuments: boolean;
  requirementCount: number;
  generating: boolean;
  onGenerate: () => Promise<void>;
  onUpdate: (item: ClarificationItem) => Promise<void>;
  onApprove: (id: number) => Promise<void>;
  onDelete: (id: number) => Promise<void>;
  onExport: () => void;
}

const impactColour: Record<string, string> = {
  Blocking: s.colours.red,
  Important: s.colours.amber,
  Minor: s.colours.grey,
};

const impactTone: Record<string, "red" | "amber" | "blue"> = {
  Blocking: "red",
  Important: "amber",
  Minor: "blue",
};

export function ClarificationsPanel({
  clarifications,
  hasDocuments,
  requirementCount,
  generating,
  onGenerate,
  onUpdate,
  onApprove,
  onDelete,
  onExport,
}: Props) {
  const [editingId, setEditingId] = useState<number | null>(null);
  const [editQuestion, setEditQuestion] = useState("");
  const [openRationale, setOpenRationale] = useState<number | null>(
    null
  );

  const approvedCount = clarifications.filter(
    (item) => item.isApproved
  ).length;

  const blockingCount = clarifications.filter(
    (item) => item.impact === "Blocking"
  ).length;

  function startEdit(item: ClarificationItem) {
    setEditingId(item.id);
    setEditQuestion(item.question);
  }

  async function saveEdit(item: ClarificationItem) {
    await onUpdate({ ...item, question: editQuestion });
    setEditingId(null);
    setEditQuestion("");
  }

  return (
    <>
      <div
        style={{
          ...s.card,
          background: s.colours.amberSoft,
          border: `1px solid rgba(240,167,66,0.26)`,
        }}
      >
        <h2 style={s.heading}>Clarification Questions</h2>

        <p style={s.row}>
          Questions are usually submitted once, to a deadline, and
          the answers are shared with every bidder. Review each one
          before approving it for submission.
        </p>

        {!hasDocuments ? (
          <p style={{ ...s.row, color: s.colours.red }}>
            Upload and extract the tender document first.
          </p>
        ) : (
          <p style={{ ...s.row, color: s.colours.green }}>
            {requirementCount} extracted requirement(s) available as
            context.
          </p>
        )}

        <button
          onClick={onGenerate}
          disabled={generating || !hasDocuments}
          data-busy={generating}
          style={{
            ...s.button,
            background: hasDocuments
              ? s.colours.amber
              : s.colours.grey,
            color: hasDocuments ? "#2B1A05" : "#fff",
            boxShadow: hasDocuments
              ? "0 6px 18px rgba(240,167,66,0.22)"
              : "none",
          }}
        >
          {generating
            ? "Preparing questions... (1-2 min)"
            : "Prepare Questions (AI)"}
        </button>

        {clarifications.length > 0 && (
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
            Export Approved Questions
          </button>
        )}
      </div>

      {clarifications.length > 0 && (
        <div style={s.card}>
          <h2 style={s.heading}>
            Questions ({clarifications.length}) | Approved:{" "}
            {approvedCount} | Blocking: {blockingCount}
          </h2>

          <p style={{ fontSize: "13px", color: s.colours.amber }}>
            Only approved questions are exported. The internal
            rationale is never included in the export.
          </p>

          <div style={{ marginTop: "14px" }}>
            {clarifications.map((item) => {
              const rationaleOpen = openRationale === item.id;

              return (
                <div
                  key={item.id}
                  style={{
                    border: item.isApproved
                      ? `1px solid rgba(46,204,143,0.35)`
                      : `1px solid ${s.colours.lineSoft}`,
                    borderLeft: `3px solid ${
                      impactColour[item.impact] ?? s.colours.grey
                    }`,
                    borderRadius: "12px",
                    padding: "16px 18px",
                    marginBottom: "12px",
                    background: s.colours.surfaceRaised,
                  }}
                >
                  <div
                    style={{
                      display: "flex",
                      justifyContent: "space-between",
                      alignItems: "center",
                      flexWrap: "wrap",
                      gap: "8px",
                      marginBottom: "10px",
                    }}
                  >
                    <div
                      style={{
                        display: "flex",
                        alignItems: "center",
                        gap: "10px",
                        flexWrap: "wrap",
                      }}
                    >
                      <strong
                        style={{
                          fontSize: "12.5px",
                          color: s.colours.text,
                        }}
                      >
                        {item.reference}
                      </strong>

                      <span
                        style={s.pill(
                          impactTone[item.impact] ?? "blue"
                        )}
                      >
                        {item.impact}
                      </span>

                      <span
                        style={{
                          fontSize: "11.5px",
                          color: s.colours.dim,
                        }}
                      >
                        {item.category}
                      </span>
                    </div>

                    <span
                      style={s.pill(
                        item.isApproved ? "green" : "amber"
                      )}
                    >
                      {item.isApproved
                        ? "Approved"
                        : "Pending review"}
                      {item.isEdited ? " · Edited" : ""}
                    </span>
                  </div>

                  {editingId === item.id ? (
                    <textarea
                      value={editQuestion}
                      onChange={(event) =>
                        setEditQuestion(event.target.value)
                      }
                      rows={4}
                      style={s.textarea}
                    />
                  ) : (
                    <p
                      style={{
                        margin: "0 0 10px",
                        fontSize: "13.5px",
                        lineHeight: 1.65,
                        color: "#C7D3E8",
                      }}
                    >
                      {item.question}
                    </p>
                  )}

                  {(item.relatedRequirements ||
                    item.sourceReference) && (
                    <p
                      style={{
                        margin: "0 0 10px",
                        fontSize: "11.5px",
                        color: s.colours.dim,
                      }}
                    >
                      {item.relatedRequirements &&
                        `Relates to: ${item.relatedRequirements}`}
                      {item.relatedRequirements &&
                        item.sourceReference &&
                        " · "}
                      {item.sourceReference}
                    </p>
                  )}

                  {item.rationale && rationaleOpen && (
                    <div
                      style={{
                        background: s.colours.ink,
                        border: `1px solid ${s.colours.lineSoft}`,
                        borderRadius: "10px",
                        padding: "12px 14px",
                        marginBottom: "10px",
                      }}
                    >
                      <strong
                        style={{
                          fontSize: "10.5px",
                          fontWeight: 700,
                          letterSpacing: "0.08em",
                          color: s.colours.dim,
                        }}
                      >
                        INTERNAL · NOT EXPORTED
                      </strong>

                      <p
                        style={{
                          margin: "7px 0 0",
                          fontSize: "12.5px",
                          lineHeight: 1.65,
                          color: s.colours.muted,
                        }}
                      >
                        {item.rationale}
                      </p>
                    </div>
                  )}

                  <div>
                    {editingId === item.id ? (
                      <>
                        <button
                          onClick={() => saveEdit(item)}
                          style={s.smallButton}
                        >
                          Save
                        </button>
                        <button
                          onClick={() => setEditingId(null)}
                          style={{
                            ...s.smallButton,
                            background: s.colours.grey,
                          }}
                        >
                          Cancel
                        </button>
                      </>
                    ) : (
                      <>
                        {item.rationale && (
                          <button
                            onClick={() =>
                              setOpenRationale(
                                rationaleOpen ? null : item.id
                              )
                            }
                            style={{
                              ...s.smallButton,
                              background: "transparent",
                              border: `1px solid ${s.colours.line}`,
                              color: s.colours.muted,
                            }}
                          >
                            {rationaleOpen
                              ? "Hide rationale"
                              : "Why ask this"}
                          </button>
                        )}

                        <button
                          onClick={() => startEdit(item)}
                          style={{
                            ...s.smallButton,
                            background: s.colours.surfaceHover,
                            border: `1px solid ${s.colours.line}`,
                          }}
                        >
                          Edit
                        </button>

                        {!item.isApproved && (
                          <button
                            onClick={() => onApprove(item.id)}
                            style={{
                              ...s.smallButton,
                              background: s.colours.green,
                              color: "#06281B",
                              fontWeight: 600,
                            }}
                          >
                            Approve
                          </button>
                        )}

                        <button
                          onClick={() => onDelete(item.id)}
                          style={{
                            ...s.smallButton,
                            background: "transparent",
                            border: `1px solid rgba(240,106,106,0.4)`,
                            color: s.colours.red,
                          }}
                        >
                          Delete
                        </button>
                      </>
                    )}
                  </div>
                </div>
              );
            })}
          </div>
        </div>
      )}
    </>
  );
}
