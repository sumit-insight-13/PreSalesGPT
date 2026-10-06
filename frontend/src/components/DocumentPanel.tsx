import { useState } from "react";
import type { DocumentItem, Opportunity } from "../types";
import * as s from "../styles";

interface Props {
  documents: DocumentItem[];
  selectedOpportunity: Opportunity | undefined;
  extractingId: number | null;
  onUpload: (file: File) => Promise<void>;
  onExtract: (documentId: number) => Promise<void>;
  onViewText: (documentId: number) => Promise<void>;
  onDelete: (documentId: number) => Promise<void>;
  onValidationError: (message: string) => void;
}

export function DocumentPanel({
  documents,
  selectedOpportunity,
  extractingId,
  onUpload,
  onExtract,
  onViewText,
  onDelete,
  onValidationError,
}: Props) {
  const [file, setFile] = useState<File | null>(null);
  const [uploading, setUploading] = useState(false);

  async function handleUpload(event: React.FormEvent) {
    event.preventDefault();

    if (!selectedOpportunity) {
      onValidationError("Select an opportunity first.");
      return;
    }

    if (!file) {
      onValidationError("Choose a file first.");
      return;
    }

    setUploading(true);

    try {
      await onUpload(file);
      setFile(null);
    } finally {
      setUploading(false);
    }
  }

  return (
    <>
      <div style={s.card}>
        <h2 style={s.heading}>Upload Document</h2>

        {selectedOpportunity ? (
          <p style={{ ...s.row, color: s.colours.blue }}>
            Selected: {selectedOpportunity.name}
          </p>
        ) : (
          <p style={{ ...s.row, color: s.colours.red }}>
            Select an opportunity above.
          </p>
        )}

        <form onSubmit={handleUpload}>
          <input
            type="file"
            accept=".pdf,.docx,.txt"
            onChange={(event) =>
              setFile(
                event.target.files ? event.target.files[0] : null
              )
            }
            style={{ marginTop: "8px", display: "block" }}
          />

          <button
            type="submit"
            disabled={uploading || !selectedOpportunity}
            data-busy={uploading}
            style={s.button}
          >
            {uploading ? "Extracting..." : "Upload & Extract"}
          </button>
        </form>
      </div>

      {documents.length > 0 && (
        <div style={s.card}>
          <h2 style={s.heading}>
            Documents ({documents.length})
          </h2>

          {documents.map((document) => {
            const extracted = document.status === "Extracted";

            return (
              <div
                key={document.id}
                style={{
                  border: `1px solid ${s.colours.lineSoft}`,
                  borderRadius: "12px",
                  padding: "16px",
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
                  }}
                >
                  <strong
                    style={{
                      fontSize: "14px",
                      color: s.colours.text,
                    }}
                  >
                    {document.fileName}
                  </strong>

                  <span
                    style={s.pill(extracted ? "green" : "red")}
                  >
                    {document.status}
                  </span>
                </div>

                <p
                  style={{
                    ...s.row,
                    fontSize: "12px",
                    color: s.colours.dim,
                  }}
                >
                  {document.pageCount} page(s) ·{" "}
                  {document.textLength.toLocaleString()} characters
                  · {Math.round(document.fileSizeBytes / 1024)} KB
                </p>

                {document.errorMessage && (
                  <p style={{ ...s.row, color: s.colours.red }}>
                    {document.errorMessage}
                  </p>
                )}

                <button
                  onClick={() => onExtract(document.id)}
                  disabled={
                    extractingId === document.id || !extracted
                  }
                  data-busy={extractingId === document.id}
                  style={{
                    ...s.smallButton,
                    background: s.colours.green,
                    color: "#06281B",
                    fontWeight: 600,
                  }}
                >
                  {extractingId === document.id
                    ? "AI is reading..."
                    : "Extract Requirements (AI)"}
                </button>

                <button
                  onClick={() => onViewText(document.id)}
                  style={{
                    ...s.smallButton,
                    background: s.colours.surfaceHover,
                    border: `1px solid ${s.colours.line}`,
                  }}
                >
                  View Text
                </button>

                <button
                  onClick={() => onDelete(document.id)}
                  style={{
                    ...s.smallButton,
                    background: "transparent",
                    border: `1px solid rgba(240,106,106,0.4)`,
                    color: s.colours.red,
                  }}
                >
                  Delete
                </button>
              </div>
            );
          })}
        </div>
      )}
    </>
  );
}
