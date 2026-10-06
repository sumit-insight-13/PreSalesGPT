import { useState } from "react";
import type { SectionItem } from "../types";
import * as s from "../styles";

interface Props {
  sections: SectionItem[];
  regeneratingId: number | null;
  onUpdate: (section: SectionItem) => Promise<void>;
  onRegenerate: (sectionId: number) => Promise<void>;
  onApprove: (sectionId: number) => Promise<void>;
}

export function ProposalSections({
  sections,
  regeneratingId,
  onUpdate,
  onRegenerate,
  onApprove,
}: Props) {
  const [editingId, setEditingId] = useState<number | null>(null);
  const [editText, setEditText] = useState("");

  if (sections.length === 0) {
    return null;
  }

  const approvedCount = sections.filter(
    (section) => section.isApproved
  ).length;

  function startEdit(section: SectionItem) {
    setEditingId(section.id);
    setEditText(section.content);
  }

  async function saveEdit(section: SectionItem) {
    await onUpdate({ ...section, content: editText });
    setEditingId(null);
    setEditText("");
  }

  return (
    <div style={s.card}>
      <h2 style={s.heading}>
        Proposal Sections ({sections.length}) | Approved:{" "}
        {approvedCount}
      </h2>

      <p style={{ fontSize: "13px", color: s.colours.amber }}>
        AI draft. Fill in the placeholders. Read it fully before
        sending to the customer.
      </p>

      {sections.map((section) => (
        <div
          key={section.id}
          style={{
            border: section.isApproved
              ? `1px solid rgba(46,204,143,0.35)`
              : `1px solid ${s.colours.lineSoft}`,
            borderLeft: section.isApproved
              ? `3px solid ${s.colours.green}`
              : `3px solid ${s.colours.line}`,
            borderRadius: "12px",
            padding: "18px",
            marginBottom: "14px",
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
            }}
          >
            <h3
              style={{
                margin: 0,
                fontSize: "15.5px",
                color: s.colours.text,
              }}
            >
              {section.sectionOrder}. {section.sectionName}
            </h3>

            <span
              style={s.pill(
                section.isApproved ? "green" : "amber"
              )}
            >
              {section.isApproved ? "Approved" : "Pending review"}
              {section.isEdited ? " · Edited" : ""}
            </span>
          </div>

          {section.coveredRequirements && (
            <p
              style={{
                ...s.row,
                fontSize: "11.5px",
                color: s.colours.dim,
                marginTop: "8px",
              }}
            >
              Covers: {section.coveredRequirements}
            </p>
          )}

          {editingId === section.id ? (
            <textarea
              value={editText}
              onChange={(event) =>
                setEditText(event.target.value)
              }
              rows={14}
              style={{ ...s.textarea, marginTop: "12px" }}
            />
          ) : (
            <pre style={s.content}>{section.content}</pre>
          )}

          {section.assumptions && (
            <div
              style={{
                background: s.colours.amberSoft,
                border: `1px solid rgba(240,167,66,0.3)`,
                borderRadius: "10px",
                padding: "12px 14px",
                marginTop: "12px",
              }}
            >
              <strong
                style={{
                  fontSize: "11px",
                  fontWeight: 700,
                  letterSpacing: "0.06em",
                  color: s.colours.amber,
                }}
              >
                ASSUMPTIONS
              </strong>

              <pre
                style={{
                  whiteSpace: "pre-wrap",
                  fontFamily: "Inter, Segoe UI, sans-serif",
                  fontSize: "12.5px",
                  lineHeight: 1.65,
                  color: "#E4C89B",
                  margin: "8px 0 0",
                }}
              >
                {section.assumptions}
              </pre>
            </div>
          )}

          <div style={{ marginTop: "12px" }}>
            {editingId === section.id ? (
              <>
                <button
                  onClick={() => saveEdit(section)}
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
                <button
                  onClick={() => startEdit(section)}
                  style={{
                    ...s.smallButton,
                    background: s.colours.surfaceHover,
                    border: `1px solid ${s.colours.line}`,
                  }}
                >
                  Edit
                </button>

                <button
                  onClick={() => onRegenerate(section.id)}
                  disabled={regeneratingId === section.id}
                  data-busy={regeneratingId === section.id}
                  style={{
                    ...s.smallButton,
                    background: s.colours.purple,
                  }}
                >
                  {regeneratingId === section.id
                    ? "Regenerating..."
                    : "Regenerate"}
                </button>

                {!section.isApproved && (
                  <button
                    onClick={() => onApprove(section.id)}
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
              </>
            )}
          </div>
        </div>
      ))}
    </div>
  );
}
