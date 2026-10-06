import { useState } from "react";
import type { SectionItem, SowSectionItem } from "../types";
import * as s from "../styles";

interface Props {
  sowSections: SowSectionItem[];
  proposalSections: SectionItem[];
  generating: boolean;
  onGenerate: () => Promise<void>;
  onUpdate: (section: SowSectionItem) => Promise<void>;
  onApprove: (sectionId: number) => Promise<void>;
  onExport: () => void;
}

export function SowPanel({
  sowSections,
  proposalSections,
  generating,
  onGenerate,
  onUpdate,
  onApprove,
  onExport,
}: Props) {
  const [editingId, setEditingId] = useState<number | null>(null);
  const [editText, setEditText] = useState("");

  const pendingProposal = proposalSections.filter(
    (section) => !section.isApproved
  ).length;

  const gateOpen =
    proposalSections.length > 0 && pendingProposal === 0;

  const approvedCount = sowSections.filter(
    (section) => section.isApproved
  ).length;

  function startEdit(section: SowSectionItem) {
    setEditingId(section.id);
    setEditText(section.content);
  }

  async function saveEdit(section: SowSectionItem) {
    await onUpdate({ ...section, content: editText });
    setEditingId(null);
    setEditText("");
  }

  return (
    <>
      <div
        style={{
          ...s.card,
          background: s.colours.purpleSoft,
          border: `1px solid rgba(155,124,246,0.28)`,
        }}
      >
        <h2 style={s.heading}>Statement of Work</h2>

        <p style={s.row}>
          A Statement of Work is contractual. It is drafted only from
          proposal content that has been reviewed and approved.
        </p>

        {proposalSections.length === 0 ? (
          <p style={{ ...s.row, color: s.colours.red }}>
            Generate the proposal first.
          </p>
        ) : gateOpen ? (
          <p style={{ ...s.row, color: s.colours.green }}>
            All {proposalSections.length} proposal sections are
            approved. The Statement of Work can be drafted.
          </p>
        ) : (
          <p style={{ ...s.row, color: s.colours.amber }}>
            {pendingProposal} proposal section(s) still awaiting
            review. Approve all sections to unlock this step.
          </p>
        )}

        <button
          onClick={onGenerate}
          disabled={generating || !gateOpen}
          data-busy={generating}
          style={{
            ...s.button,
            background: gateOpen
              ? s.colours.purple
              : s.colours.grey,
            boxShadow: gateOpen
              ? "0 6px 18px rgba(155,124,246,0.26)"
              : "none",
          }}
        >
          {generating
            ? "Drafting the Statement of Work... (2-4 min)"
            : "Draft Statement of Work (AI)"}
        </button>

        {sowSections.length > 0 && (
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
            Export SOW
          </button>
        )}
      </div>

      {sowSections.length > 0 && (
        <div style={s.card}>
          <h2 style={s.heading}>
            SOW Sections ({sowSections.length}) | Approved:{" "}
            {approvedCount}
          </h2>

          <div
            style={{
              background: s.colours.redSoft,
              border: `1px solid rgba(240,106,106,0.3)`,
              borderRadius: "10px",
              padding: "13px 16px",
              margin: "12px 0 18px",
              fontSize: "13px",
              lineHeight: 1.6,
              color: "#FFB4B4",
            }}
          >
            This draft has not been reviewed by Legal. It is not a
            contractual offer and must not be issued to a customer
            without legal review.
          </div>

          {sowSections.map((section) => (
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
                  {section.isApproved
                    ? "Approved"
                    : "Pending review"}
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
                  Implements: {section.coveredRequirements}
                </p>
              )}

              {editingId === section.id ? (
                <textarea
                  value={editText}
                  onChange={(event) =>
                    setEditText(event.target.value)
                  }
                  rows={16}
                  style={{ ...s.textarea, marginTop: "12px" }}
                />
              ) : (
                <pre style={s.content}>{section.content}</pre>
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
      )}
    </>
  );
}
