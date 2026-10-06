import { useState } from "react";
import type { RequirementItem } from "../types";
import * as s from "../styles";

interface Props {
  requirements: RequirementItem[];
  onApprove: (id: number) => Promise<void>;
  onApproveAll: () => Promise<void>;
  onUpdate: (requirement: RequirementItem) => Promise<void>;
  onDelete: (id: number) => Promise<void>;
}

export function RequirementsTable({
  requirements,
  onApprove,
  onApproveAll,
  onUpdate,
  onDelete,
}: Props) {
  const [editingId, setEditingId] = useState<number | null>(null);
  const [editText, setEditText] = useState("");

  if (requirements.length === 0) {
    return null;
  }

  const approvedCount = requirements.filter(
    (requirement) => requirement.isApproved
  ).length;

  const mandatoryCount = requirements.filter(
    (requirement) => requirement.isMandatory
  ).length;

  function startEdit(requirement: RequirementItem) {
    setEditingId(requirement.id);
    setEditText(requirement.description);
  }

  async function saveEdit(requirement: RequirementItem) {
    await onUpdate({ ...requirement, description: editText });
    setEditingId(null);
    setEditText("");
  }

  return (
    <div style={s.card}>
      <div
        style={{
          display: "flex",
          justifyContent: "space-between",
          alignItems: "center",
          flexWrap: "wrap",
          gap: "10px",
        }}
      >
        <div>
          <h2 style={{ ...s.heading, marginBottom: "4px" }}>
            Requirements ({requirements.length})
          </h2>

          <div
            style={{
              display: "flex",
              gap: "7px",
              flexWrap: "wrap",
              marginTop: "8px",
            }}
          >
            <span style={s.pill("green")}>
              {approvedCount} approved
            </span>
            <span style={s.pill("red")}>
              {mandatoryCount} mandatory
            </span>
          </div>
        </div>

        <button
          onClick={onApproveAll}
          style={{
            ...s.smallButton,
            marginTop: 0,
            marginRight: 0,
            background: s.colours.green,
            color: "#06281B",
            fontWeight: 600,
          }}
        >
          Approve All
        </button>
      </div>

      <p
        style={{
          fontSize: "12.5px",
          color: s.colours.dim,
          marginTop: "12px",
        }}
      >
        Only approved requirements are used to generate the
        proposal.
      </p>

      <div style={{ overflowX: "auto", marginTop: "14px" }}>
        <table style={s.table}>
          <thead>
            <tr>
              <th style={s.th}>Code</th>
              <th style={s.th}>Description</th>
              <th style={s.th}>Category</th>
              <th style={s.th}>Priority</th>
              <th style={s.th}>Mandatory</th>
              <th style={s.th}>Source</th>
              <th style={s.th}>Status</th>
              <th style={s.th}>Actions</th>
            </tr>
          </thead>

          <tbody>
            {requirements.map((requirement) => (
              <tr key={requirement.id}>
                <td
                  style={{
                    ...s.td,
                    fontWeight: 600,
                    color: s.colours.text,
                    whiteSpace: "nowrap",
                  }}
                >
                  {requirement.code}
                </td>

                <td style={{ ...s.td, minWidth: "280px" }}>
                  {editingId === requirement.id ? (
                    <textarea
                      value={editText}
                      onChange={(event) =>
                        setEditText(event.target.value)
                      }
                      rows={3}
                      style={s.textarea}
                    />
                  ) : (
                    requirement.description
                  )}
                </td>

                <td style={{ ...s.td, whiteSpace: "nowrap" }}>
                  {requirement.category}
                </td>

                <td style={s.td}>
                  <span
                    style={{
                      color:
                        requirement.priority === "High"
                          ? s.colours.red
                          : requirement.priority === "Low"
                          ? s.colours.dim
                          : s.colours.muted,
                      fontWeight: 600,
                    }}
                  >
                    {requirement.priority}
                  </span>
                </td>

                <td style={s.td}>
                  {requirement.isMandatory ? (
                    <span style={s.pill("red")}>Yes</span>
                  ) : (
                    <span style={{ color: s.colours.dim }}>
                      No
                    </span>
                  )}
                </td>

                <td
                  style={{
                    ...s.td,
                    color: s.colours.dim,
                    whiteSpace: "nowrap",
                  }}
                >
                  {requirement.sourceReference || "-"}
                </td>

                <td style={s.td}>
                  <span
                    style={s.pill(
                      requirement.isApproved ? "green" : "amber"
                    )}
                  >
                    {requirement.isApproved
                      ? "Approved"
                      : "Pending"}
                  </span>
                </td>

                <td style={{ ...s.td, whiteSpace: "nowrap" }}>
                  {editingId === requirement.id ? (
                    <>
                      <button
                        onClick={() => saveEdit(requirement)}
                        style={s.tinyButton}
                      >
                        Save
                      </button>
                      <button
                        onClick={() => setEditingId(null)}
                        style={{
                          ...s.tinyButton,
                          background: s.colours.grey,
                        }}
                      >
                        Cancel
                      </button>
                    </>
                  ) : (
                    <>
                      <button
                        onClick={() => startEdit(requirement)}
                        style={{
                          ...s.tinyButton,
                          background: s.colours.surfaceHover,
                          border: `1px solid ${s.colours.line}`,
                        }}
                      >
                        Edit
                      </button>

                      {!requirement.isApproved && (
                        <button
                          onClick={() =>
                            onApprove(requirement.id)
                          }
                          style={{
                            ...s.tinyButton,
                            background: s.colours.green,
                            color: "#06281B",
                            fontWeight: 600,
                          }}
                        >
                          Approve
                        </button>
                      )}

                      <button
                        onClick={() => onDelete(requirement.id)}
                        style={{
                          ...s.tinyButton,
                          background: "transparent",
                          border: `1px solid rgba(240,106,106,0.4)`,
                          color: s.colours.red,
                        }}
                      >
                        Delete
                      </button>
                    </>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
