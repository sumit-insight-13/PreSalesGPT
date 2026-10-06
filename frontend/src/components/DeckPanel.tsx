import { useState } from "react";
import type { DeckSlideItem } from "../types";
import * as s from "../styles";

interface Props {
  slides: DeckSlideItem[];
  approvedSectionCount: number;
  generating: boolean;
  onGenerate: () => Promise<void>;
  onExport: () => void;
}

interface LayoutData {
  stats?: { value: string; label: string }[];
  columns?: { heading: string; items: string[] }[];
  callout?: string;
}

/* The slide previews keep their own palette. A preview should look
   like the slide it represents, not like the application around it.
   These match the colours used by PowerPointBuilder. */
const SLIDE_INK = "#0F1E38";
const SLIDE_ACCENT = "#3B6FF5";
const SLIDE_BODY = "#37455C";
const SLIDE_TINT = "#F2F6FD";

function parseLayoutData(raw: string): LayoutData {
  if (!raw) {
    return {};
  }

  try {
    return JSON.parse(raw) as LayoutData;
  } catch {
    return {};
  }
}

export function DeckPanel({
  slides,
  approvedSectionCount,
  generating,
  onGenerate,
  onExport,
}: Props) {
  const [openNotes, setOpenNotes] = useState<number | null>(null);

  const ready = approvedSectionCount > 0;

  const layoutMix = slides.reduce<Record<string, number>>(
    (counts, slide) => {
      const layout = slide.layout || "Bullets";
      counts[layout] = (counts[layout] ?? 0) + 1;
      return counts;
    },
    {}
  );

  return (
    <>
      <div
        style={{
          ...s.card,
          background: "rgba(14, 116, 144, 0.13)",
          border: `1px solid rgba(14, 116, 144, 0.3)`,
        }}
      >
        <h2 style={s.heading}>Customer Presentation</h2>

        <p style={s.row}>
          Slides are built only from approved proposal sections.
          Placeholders are carried through rather than filled in.
        </p>

        {ready ? (
          <p style={{ ...s.row, color: s.colours.green }}>
            {approvedSectionCount} approved proposal section(s)
            available.
          </p>
        ) : (
          <p style={{ ...s.row, color: s.colours.red }}>
            Approve at least one proposal section first.
          </p>
        )}

        {slides.length > 0 && (
          <div
            style={{
              display: "flex",
              gap: "7px",
              flexWrap: "wrap",
              marginTop: "12px",
            }}
          >
            {Object.entries(layoutMix).map(([layout, count]) => (
              <span key={layout} style={s.pill("blue")}>
                {layout} · {count}
              </span>
            ))}
          </div>
        )}

        <button
          onClick={onGenerate}
          disabled={generating || !ready}
          data-busy={generating}
          style={{
            ...s.button,
            background: ready ? "#0E7490" : s.colours.grey,
            boxShadow: ready
              ? "0 6px 18px rgba(14,116,144,0.3)"
              : "none",
          }}
        >
          {generating
            ? "Building the deck... (1-2 min)"
            : "Build Deck (AI)"}
        </button>

        {slides.length > 0 && (
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
            Download PowerPoint
          </button>
        )}
      </div>

      {slides.length > 0 && (
        <div style={s.card}>
          <h2 style={s.heading}>Slides ({slides.length})</h2>

          <p style={{ fontSize: "12.5px", color: s.colours.dim }}>
            A section divider is inserted automatically whenever the
            deck moves to a new part of the proposal.
          </p>

          <div
            style={{
              display: "grid",
              gridTemplateColumns:
                "repeat(auto-fill, minmax(348px, 1fr))",
              gap: "18px",
              marginTop: "16px",
            }}
          >
            {slides.map((slide) => {
              const layout = slide.layout || "Bullets";
              const data = parseLayoutData(slide.layoutData);
              const notesOpen = openNotes === slide.id;

              const bullets = (slide.bullets || "")
                .split("\n")
                .map((bullet) => bullet.trim())
                .filter((bullet) => bullet.length > 0);

              return (
                <div
                  key={slide.id}
                  style={{
                    border: `1px solid ${s.colours.line}`,
                    borderRadius: "12px",
                    overflow: "hidden",
                    background: s.colours.surfaceRaised,
                  }}
                >
                  <div
                    style={{
                      background: s.colours.ink,
                      padding: "9px 14px",
                      fontSize: "11px",
                      display: "flex",
                      justifyContent: "space-between",
                      alignItems: "center",
                      borderBottom: `1px solid ${s.colours.lineSoft}`,
                    }}
                  >
                    <span style={{ color: s.colours.dim }}>
                      Slide {slide.slideOrder}
                    </span>

                    <span
                      style={{
                        background: s.colours.blueSoft,
                        border: `1px solid rgba(77,124,254,0.3)`,
                        color: "#BFD0FF",
                        padding: "2px 9px",
                        borderRadius: "20px",
                        fontWeight: 600,
                        fontSize: "10.5px",
                      }}
                    >
                      {layout}
                    </span>
                  </div>

                  {/* The preview itself, on a light surface */}
                  <div
                    style={{
                      padding: "18px",
                      minHeight: "200px",
                      background: "#FFFFFF",
                    }}
                  >
                    <h3
                      style={{
                        margin: 0,
                        fontSize: "14.5px",
                        lineHeight: 1.4,
                        color: SLIDE_INK,
                      }}
                    >
                      {slide.title}
                    </h3>

                    <div
                      style={{
                        width: "40px",
                        height: "3px",
                        background: SLIDE_ACCENT,
                        margin: "10px 0 14px",
                      }}
                    />

                    {layout === "Stats" &&
                      data.stats &&
                      data.stats.length > 0 && (
                        <div
                          style={{
                            display: "grid",
                            gridTemplateColumns: `repeat(${Math.min(
                              data.stats.length,
                              2
                            )}, 1fr)`,
                            gap: "9px",
                          }}
                        >
                          {data.stats.map((stat, index) => (
                            <div
                              key={index}
                              style={{
                                background: SLIDE_TINT,
                                borderTop: `3px solid ${SLIDE_ACCENT}`,
                                padding: "12px",
                              }}
                            >
                              <div
                                style={{
                                  fontSize: "21px",
                                  fontWeight: 700,
                                  color: SLIDE_INK,
                                }}
                              >
                                {stat.value}
                              </div>
                              <div
                                style={{
                                  fontSize: "11px",
                                  color: SLIDE_BODY,
                                  marginTop: "4px",
                                  lineHeight: 1.45,
                                }}
                              >
                                {stat.label}
                              </div>
                            </div>
                          ))}
                        </div>
                      )}

                    {layout === "TwoColumn" &&
                      data.columns &&
                      data.columns.length > 0 && (
                        <div
                          style={{
                            display: "grid",
                            gridTemplateColumns: "1fr 1fr",
                            gap: "10px",
                          }}
                        >
                          {data.columns
                            .slice(0, 2)
                            .map((column, index) => (
                              <div
                                key={index}
                                style={{
                                  background: SLIDE_TINT,
                                  padding: "12px",
                                }}
                              >
                                <div
                                  style={{
                                    fontSize: "11.5px",
                                    fontWeight: 700,
                                    color: SLIDE_ACCENT,
                                    marginBottom: "7px",
                                  }}
                                >
                                  {column.heading}
                                </div>

                                <ul
                                  style={{
                                    margin: 0,
                                    paddingLeft: "15px",
                                    fontSize: "11px",
                                    lineHeight: 1.6,
                                    color: SLIDE_BODY,
                                  }}
                                >
                                  {column.items.map(
                                    (item, itemIndex) => (
                                      <li key={itemIndex}>
                                        {item}
                                      </li>
                                    )
                                  )}
                                </ul>
                              </div>
                            ))}
                        </div>
                      )}

                    {layout === "Callout" && data.callout && (
                      <div
                        style={{
                          background: SLIDE_TINT,
                          borderLeft: `4px solid ${SLIDE_ACCENT}`,
                          padding: "14px 16px",
                          fontSize: "13.5px",
                          lineHeight: 1.6,
                          color: SLIDE_INK,
                        }}
                      >
                        {data.callout}
                      </div>
                    )}

                    {layout === "Bullets" && bullets.length > 0 && (
                      <ul
                        style={{
                          margin: 0,
                          paddingLeft: "17px",
                          fontSize: "12.5px",
                          lineHeight: 1.7,
                          color: SLIDE_BODY,
                        }}
                      >
                        {bullets.map((bullet, index) => (
                          <li key={index}>{bullet}</li>
                        ))}
                      </ul>
                    )}
                  </div>

                  <div
                    style={{
                      padding: "12px 14px",
                      borderTop: `1px solid ${s.colours.lineSoft}`,
                    }}
                  >
                    <div
                      style={{
                        display: "flex",
                        justifyContent: "space-between",
                        alignItems: "center",
                        gap: "10px",
                      }}
                    >
                      <span
                        style={{
                          fontSize: "11px",
                          color: s.colours.dim,
                        }}
                      >
                        {slide.sourceSection}
                      </span>

                      {slide.speakerNotes && (
                        <button
                          onClick={() =>
                            setOpenNotes(
                              notesOpen ? null : slide.id
                            )
                          }
                          style={{
                            ...s.tinyButton,
                            marginRight: 0,
                            marginBottom: 0,
                            background: "transparent",
                            border: `1px solid ${s.colours.line}`,
                            color: s.colours.muted,
                          }}
                        >
                          {notesOpen ? "Hide notes" : "Notes"}
                        </button>
                      )}
                    </div>

                    {notesOpen && slide.speakerNotes && (
                      <p
                        style={{
                          margin: "10px 0 0",
                          padding: "11px 13px",
                          background: s.colours.ink,
                          border: `1px solid ${s.colours.lineSoft}`,
                          borderRadius: "9px",
                          fontSize: "12px",
                          lineHeight: 1.65,
                          color: s.colours.muted,
                        }}
                      >
                        {slide.speakerNotes}
                      </p>
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
