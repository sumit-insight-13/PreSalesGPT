import type { CoverageReport } from "../types";
import * as s from "../styles";

interface Props {
  report: CoverageReport | null;
}

export function CoverageReportPanel({ report }: Props) {
  if (!report) {
    return null;
  }

  const uncovered = report.requirements.filter(
    (requirement) => !requirement.isCovered
  );

  const claimedOnly = report.requirements.filter(
    (requirement) => requirement.isClaimedOnly
  );

  const ready = report.isReadyForRelease;

  return (
    <div
      style={{
        ...s.card,
        background: ready
          ? s.colours.greenSoft
          : s.colours.amberSoft,
        border: ready
          ? `1px solid rgba(46,204,143,0.3)`
          : `1px solid rgba(240,167,66,0.3)`,
      }}
    >
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
          Coverage Report
        </h2>

        <span style={s.pill(ready ? "green" : "amber")}>
          {ready
            ? "Ready for customer release"
            : "Not ready for release"}
        </span>
      </div>

      <div style={{ ...s.statGrid, marginTop: "18px" }}>
        <Stat
          label="Coverage"
          value={`${report.coveragePercent}%`}
          good={report.coveragePercent === 100}
        />
        <Stat
          label="Covered"
          value={`${report.coveredCount} / ${report.totalApproved}`}
        />
        <Stat
          label="Uncovered mandatory"
          value={String(report.uncoveredMandatoryCount)}
          good={report.uncoveredMandatoryCount === 0}
          severe
        />
        <Stat
          label="Placeholders"
          value={String(report.totalPlaceholders)}
          good={report.totalPlaceholders === 0}
        />
        <Stat
          label="Sections pending"
          value={String(report.sectionsAwaitingApproval)}
          good={report.sectionsAwaitingApproval === 0}
        />
      </div>

      {report.blockers.length > 0 && (
        <div
          style={{
            background: s.colours.surfaceRaised,
            border: `1px solid ${s.colours.lineSoft}`,
            borderRadius: "12px",
            padding: "16px 18px",
            marginTop: "18px",
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
            BLOCKERS BEFORE RELEASE
          </strong>

          <ul
            style={{
              margin: "10px 0 0",
              paddingLeft: "18px",
              color: "#C7D3E8",
            }}
          >
            {report.blockers.map((blocker, index) => (
              <li
                key={index}
                style={{
                  fontSize: "13px",
                  lineHeight: 1.65,
                  marginBottom: "6px",
                }}
              >
                {blocker}
              </li>
            ))}
          </ul>
        </div>
      )}

      {uncovered.length > 0 && (
        <div style={{ marginTop: "20px" }}>
          <strong
            style={{
              fontSize: "11px",
              fontWeight: 700,
              letterSpacing: "0.06em",
              color: s.colours.red,
            }}
          >
            UNCOVERED REQUIREMENTS
          </strong>

          <div style={{ overflowX: "auto", marginTop: "10px" }}>
            <table style={s.table}>
              <thead>
                <tr>
                  <th style={s.th}>Code</th>
                  <th style={s.th}>Description</th>
                  <th style={s.th}>Category</th>
                  <th style={s.th}>Mandatory</th>
                </tr>
              </thead>
              <tbody>
                {uncovered.map((requirement) => (
                  <tr key={requirement.code}>
                    <td style={s.td}>{requirement.code}</td>
                    <td style={s.td}>
                      {requirement.description}
                    </td>
                    <td style={s.td}>{requirement.category}</td>
                    <td style={s.td}>
                      <span
                        style={s.pill(
                          requirement.isMandatory
                            ? "red"
                            : "blue"
                        )}
                      >
                        {requirement.isMandatory ? "Yes" : "No"}
                      </span>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {claimedOnly.length > 0 && (
        <div style={{ marginTop: "20px" }}>
          <strong
            style={{
              fontSize: "11px",
              fontWeight: 700,
              letterSpacing: "0.06em",
              color: s.colours.amber,
            }}
          >
            CLAIMED BUT NOT VISIBLE IN SECTION TEXT
          </strong>

          <p
            style={{
              fontSize: "12.5px",
              color: s.colours.dim,
              marginTop: "6px",
            }}
          >
            The model listed these as covered, but their code does
            not appear in any section text. Verify manually.
          </p>

          <ul
            style={{
              paddingLeft: "18px",
              color: "#C7D3E8",
            }}
          >
            {claimedOnly.map((requirement) => (
              <li
                key={requirement.code}
                style={{
                  fontSize: "13px",
                  lineHeight: 1.65,
                  marginBottom: "5px",
                }}
              >
                <strong>{requirement.code}</strong> ·{" "}
                {requirement.description}
              </li>
            ))}
          </ul>
        </div>
      )}

      <div style={{ marginTop: "20px" }}>
        <strong
          style={{
            fontSize: "11px",
            fontWeight: 700,
            letterSpacing: "0.06em",
            color: s.colours.dim,
          }}
        >
          SECTION BREAKDOWN
        </strong>

        <div style={{ overflowX: "auto", marginTop: "10px" }}>
          <table style={s.table}>
            <thead>
              <tr>
                <th style={s.th}>#</th>
                <th style={s.th}>Section</th>
                <th style={s.th}>Approved</th>
                <th style={s.th}>Reqs</th>
                <th style={s.th}>Placeholders</th>
              </tr>
            </thead>
            <tbody>
              {report.sections.map((section) => (
                <tr key={section.sectionOrder}>
                  <td style={s.td}>{section.sectionOrder}</td>
                  <td style={s.td}>{section.sectionName}</td>
                  <td style={s.td}>
                    <span
                      style={s.pill(
                        section.isApproved ? "green" : "amber"
                      )}
                    >
                      {section.isApproved ? "Yes" : "Pending"}
                    </span>
                  </td>
                  <td style={s.td}>
                    {section.requirementsCovered}
                  </td>
                  <td style={s.td}>
                    {section.placeholderCount === 0 ? (
                      <span style={{ color: s.colours.green }}>
                        0
                      </span>
                    ) : (
                      <div>
                        <strong
                          style={{ color: s.colours.amber }}
                        >
                          {section.placeholderCount}
                        </strong>
                        <div
                          style={{
                            fontSize: "11px",
                            color: s.colours.dim,
                            marginTop: "4px",
                            lineHeight: 1.5,
                          }}
                        >
                          {section.placeholders.join(" · ")}
                        </div>
                      </div>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}

interface StatProps {
  label: string;
  value: string;
  good?: boolean;
  severe?: boolean;
}

function Stat({ label, value, good, severe }: StatProps) {
  const colour =
    good === undefined
      ? s.colours.text
      : good
      ? s.colours.green
      : severe
      ? s.colours.red
      : s.colours.amber;

  return (
    <div style={s.statCard}>
      <span style={s.statLabel}>{label}</span>
      <strong style={{ ...s.statValue, color: colour }}>
        {value}
      </strong>
    </div>
  );
}
