import type { CSSProperties } from "react";

/* ---------------------------------------------------------------
   Shared inline styles.
   Every panel imports from here, so changing this file re-themes
   the whole application. Interaction states live in index.css,
   because inline styles cannot express hover or focus.
   --------------------------------------------------------------- */

export const colours = {
  ink: "#0B1524",
  surface: "#111E33",
  surfaceRaised: "#16253D",
  surfaceHover: "#1B2C48",

  line: "#223350",
  lineSoft: "#1A2942",

  text: "#E8EEFB",
  muted: "#94A4C0",
  dim: "#6A7B98",

  blue: "#4D7CFE",
  blueSoft: "rgba(77, 124, 254, 0.14)",

  green: "#2ECC8F",
  greenSoft: "rgba(46, 204, 143, 0.13)",

  amber: "#F0A742",
  amberSoft: "rgba(240, 167, 66, 0.13)",

  red: "#F06A6A",
  redSoft: "rgba(240, 106, 106, 0.13)",

  purple: "#9B7CF6",
  purpleSoft: "rgba(155, 124, 246, 0.14)",

  grey: "#5B6B87",
};

export const page: CSSProperties = {
  padding: 0,
  margin: 0,
};

export const card: CSSProperties = {
  border: `1px solid ${colours.lineSoft}`,
  borderRadius: "14px",
  padding: "24px",
  marginBottom: "22px",
  background: colours.surface,
  boxShadow:
    "0 1px 2px rgba(0,0,0,0.3), 0 8px 24px rgba(0,0,0,0.22)",
};

export const cardGrid: CSSProperties = {
  display: "grid",
  gridTemplateColumns: "repeat(auto-fill, minmax(268px, 1fr))",
  gap: "16px",
};

export const formGrid: CSSProperties = {
  display: "grid",
  gridTemplateColumns: "1fr 1fr",
  gap: "13px",
};

export const statGrid: CSSProperties = {
  display: "grid",
  gridTemplateColumns: "repeat(auto-fit, minmax(154px, 1fr))",
  gap: "13px",
};

export const statCard: CSSProperties = {
  background: colours.surfaceRaised,
  border: `1px solid ${colours.lineSoft}`,
  borderRadius: "12px",
  padding: "16px",
};

export const statLabel: CSSProperties = {
  display: "block",
  fontSize: "11.5px",
  letterSpacing: "0.02em",
  color: colours.dim,
};

export const statValue: CSSProperties = {
  display: "block",
  fontSize: "26px",
  fontWeight: 700,
  marginTop: "6px",
  color: colours.text,
};

export const heading: CSSProperties = {
  marginTop: 0,
  marginBottom: "6px",
  fontSize: "17px",
  fontWeight: 650,
  color: colours.text,
};

export const input: CSSProperties = {
  padding: "11px 13px",
  border: `1px solid ${colours.line}`,
  borderRadius: "10px",
  outline: "none",
  background: colours.surfaceRaised,
  color: colours.text,
  fontSize: "13.5px",
};

export const textarea: CSSProperties = {
  width: "100%",
  padding: "12px 13px",
  border: `1px solid ${colours.line}`,
  borderRadius: "10px",
  background: colours.surfaceRaised,
  color: colours.text,
  fontFamily: "Inter, Segoe UI, sans-serif",
  fontSize: "13.5px",
  lineHeight: 1.65,
  resize: "vertical",
};

export const content: CSSProperties = {
  whiteSpace: "pre-wrap",
  wordBreak: "break-word",
  fontFamily: "Inter, Segoe UI, sans-serif",
  fontSize: "13.5px",
  lineHeight: 1.7,
  color: "#C7D3E8",
  background: colours.ink,
  padding: "16px 18px",
  borderRadius: "10px",
  border: `1px solid ${colours.lineSoft}`,
  marginTop: "12px",
  maxHeight: "460px",
  overflowY: "auto",
};

export const table: CSSProperties = {
  width: "100%",
  borderCollapse: "collapse",
  background: "transparent",
  fontSize: "12.5px",
};

export const th: CSSProperties = {
  padding: "11px 10px",
  textAlign: "left",
  borderBottom: `1px solid ${colours.line}`,
  color: colours.dim,
  fontSize: "11px",
  fontWeight: 700,
  letterSpacing: "0.06em",
  textTransform: "uppercase",
};

export const td: CSSProperties = {
  padding: "12px 10px",
  borderBottom: `1px solid ${colours.lineSoft}`,
  verticalAlign: "top",
  color: "#C7D3E8",
  lineHeight: 1.6,
};

export const button: CSSProperties = {
  marginTop: "14px",
  padding: "11px 20px",
  border: "none",
  borderRadius: "10px",
  background: colours.blue,
  color: "#fff",
  fontWeight: 600,
  fontSize: "13.5px",
  boxShadow: "0 6px 18px rgba(77,124,254,0.26)",
};

export const smallButton: CSSProperties = {
  marginRight: "8px",
  marginTop: "8px",
  padding: "7px 13px",
  border: "none",
  borderRadius: "8px",
  background: colours.blue,
  color: "#fff",
  fontSize: "12.5px",
  fontWeight: 500,
};

export const tinyButton: CSSProperties = {
  marginRight: "5px",
  marginBottom: "4px",
  padding: "5px 10px",
  border: "none",
  borderRadius: "7px",
  background: colours.blue,
  color: "#fff",
  fontSize: "11.5px",
  fontWeight: 500,
};

export const row: CSSProperties = {
  margin: "5px 0",
  fontSize: "13.5px",
  color: colours.muted,
  lineHeight: 1.6,
};

export const errorBox: CSSProperties = {
  padding: "13px 16px",
  borderRadius: "10px",
  border: `1px solid rgba(240,106,106,0.3)`,
  background: colours.redSoft,
  color: "#FFB4B4",
  marginBottom: "20px",
  fontSize: "13.5px",
};

export const successBox: CSSProperties = {
  padding: "13px 16px",
  borderRadius: "10px",
  border: `1px solid rgba(46,204,143,0.3)`,
  background: colours.greenSoft,
  color: "#9EE7C8",
  marginBottom: "20px",
  fontSize: "13.5px",
};

/* A status pill, used wherever a state needs to read at a glance. */
export function pill(tone: "green" | "amber" | "red" | "blue"): CSSProperties {
  const map = {
    green: [colours.greenSoft, "#9EE7C8", "rgba(46,204,143,0.3)"],
    amber: [colours.amberSoft, "#F3C583", "rgba(240,167,66,0.3)"],
    red: [colours.redSoft, "#FFB4B4", "rgba(240,106,106,0.3)"],
    blue: [colours.blueSoft, "#BFD0FF", "rgba(77,124,254,0.3)"],
  }[tone];

  return {
    display: "inline-block",
    padding: "3px 10px",
    borderRadius: "20px",
    border: `1px solid ${map[2]}`,
    background: map[0],
    color: map[1],
    fontSize: "11px",
    fontWeight: 600,
    letterSpacing: "0.02em",
  };
}
