import { useState } from "react";
import type { Opportunity, OpportunityForm } from "../types";
import * as s from "../styles";

const emptyForm: OpportunityForm = {
  name: "",
  customerName: "",
  industry: "",
  region: "",
  estimatedValue: 0,
};

interface Props {
  opportunities: Opportunity[];
  selectedId: number | null;
  loading: boolean;
  onSelect: (id: number) => void;
  onCreate: (form: OpportunityForm) => Promise<void>;
  onValidationError: (message: string) => void;
}

export function OpportunityPanel({
  opportunities,
  selectedId,
  loading,
  onSelect,
  onCreate,
  onValidationError,
}: Props) {
  const [form, setForm] = useState<OpportunityForm>(emptyForm);
  const [saving, setSaving] = useState(false);
  const [open, setOpen] = useState(false);

  function handleChange(
    event: React.ChangeEvent<HTMLInputElement>
  ) {
    const field = event.target.name;
    const value = event.target.value;

    setForm((previous) => {
      const updated = { ...previous };

      if (field === "name") updated.name = value;
      else if (field === "customerName")
        updated.customerName = value;
      else if (field === "industry") updated.industry = value;
      else if (field === "region") updated.region = value;
      else if (field === "estimatedValue")
        updated.estimatedValue = Number(value);

      return updated;
    });
  }

  async function handleSubmit(event: React.FormEvent) {
    event.preventDefault();

    if (!form.name || !form.customerName) {
      onValidationError(
        "Opportunity Name and Customer Name are required."
      );
      return;
    }

    setSaving(true);

    try {
      await onCreate(form);
      setForm(emptyForm);
      setOpen(false);
    } finally {
      setSaving(false);
    }
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
        <h2 style={{ ...s.heading, marginBottom: 0 }}>
          Opportunities ({opportunities.length})
        </h2>

        <button
          onClick={() => setOpen(!open)}
          style={{
            ...s.smallButton,
            marginTop: 0,
            marginRight: 0,
            background: open
              ? s.colours.surfaceHover
              : s.colours.blue,
            border: open
              ? `1px solid ${s.colours.line}`
              : "none",
          }}
        >
          {open ? "Cancel" : "+ New Opportunity"}
        </button>
      </div>

      {open && (
        <form
          onSubmit={handleSubmit}
          style={{
            marginTop: "18px",
            padding: "18px",
            border: `1px solid ${s.colours.lineSoft}`,
            borderRadius: "12px",
            background: s.colours.surfaceRaised,
          }}
        >
          <div style={s.formGrid}>
            <input
              name="name"
              placeholder="Opportunity name"
              value={form.name}
              onChange={handleChange}
              style={s.input}
            />
            <input
              name="customerName"
              placeholder="Customer name"
              value={form.customerName}
              onChange={handleChange}
              style={s.input}
            />
            <input
              name="industry"
              placeholder="Industry"
              value={form.industry}
              onChange={handleChange}
              style={s.input}
            />
            <input
              name="region"
              placeholder="Region"
              value={form.region}
              onChange={handleChange}
              style={s.input}
            />
            <input
              name="estimatedValue"
              type="number"
              placeholder="Estimated value"
              value={form.estimatedValue}
              onChange={handleChange}
              style={s.input}
            />
          </div>

          <button
            type="submit"
            disabled={saving}
            data-busy={saving}
            style={s.button}
          >
            {saving ? "Saving..." : "Create Opportunity"}
          </button>
        </form>
      )}

      <div style={{ marginTop: "18px" }}>
        {loading ? (
          <p style={s.row}>Loading...</p>
        ) : opportunities.length === 0 ? (
          <p style={s.row}>
            No opportunities yet. Create one to begin.
          </p>
        ) : (
          <div style={s.cardGrid}>
            {opportunities.map((opportunity) => {
              const isSelected = opportunity.id === selectedId;

              return (
                <div
                  key={opportunity.id}
                  onClick={() => onSelect(opportunity.id)}
                  style={{
                    border: isSelected
                      ? `1px solid ${s.colours.blue}`
                      : `1px solid ${s.colours.lineSoft}`,
                    borderRadius: "12px",
                    padding: "16px",
                    background: isSelected
                      ? s.colours.blueSoft
                      : s.colours.surfaceRaised,
                    cursor: "pointer",
                    transition: "border-color 140ms ease",
                  }}
                >
                  <div
                    style={{
                      display: "flex",
                      justifyContent: "space-between",
                      alignItems: "flex-start",
                      gap: "8px",
                    }}
                  >
                    <h3
                      style={{
                        margin: 0,
                        fontSize: "14.5px",
                        lineHeight: 1.4,
                        color: s.colours.text,
                      }}
                    >
                      {opportunity.name}
                    </h3>

                    {isSelected && (
                      <span style={s.pill("blue")}>Active</span>
                    )}
                  </div>

                  <p
                    style={{
                      ...s.row,
                      marginTop: "10px",
                      fontSize: "12.5px",
                    }}
                  >
                    {opportunity.customerName}
                  </p>

                  <p
                    style={{
                      ...s.row,
                      fontSize: "11.5px",
                      color: s.colours.dim,
                    }}
                  >
                    {opportunity.industry || "Industry not set"}
                    {opportunity.region
                      ? ` · ${opportunity.region}`
                      : ""}
                  </p>
                </div>
              );
            })}
          </div>
        )}
      </div>
    </div>
  );
}
