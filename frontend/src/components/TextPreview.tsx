import * as s from "../styles";

interface Props {
  text: string;
  onClose: () => void;
}

export function TextPreview({ text, onClose }: Props) {
  if (!text) {
    return null;
  }

  return (
    <div style={s.card}>
      <div
        style={{
          display: "flex",
          justifyContent: "space-between",
          alignItems: "center",
          gap: "10px",
        }}
      >
        <h2 style={{ ...s.heading, marginBottom: 0 }}>
          Extracted Text
        </h2>

        <button
          onClick={onClose}
          style={{
            ...s.smallButton,
            marginTop: 0,
            marginRight: 0,
            background: s.colours.surfaceHover,
            border: `1px solid ${s.colours.line}`,
          }}
        >
          Close
        </button>
      </div>

      <pre
        style={{
          whiteSpace: "pre-wrap",
          wordBreak: "break-word",
          maxHeight: "440px",
          overflowY: "auto",
          background: s.colours.ink,
          padding: "16px 18px",
          borderRadius: "10px",
          border: `1px solid ${s.colours.lineSoft}`,
          fontSize: "12.5px",
          lineHeight: 1.7,
          color: s.colours.muted,
          marginTop: "14px",
        }}
      >
        {text}
      </pre>
    </div>
  );
}
