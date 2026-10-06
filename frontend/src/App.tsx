import { usePresales } from "./usePresales";
import { AppShell } from "./components/AppShell";
import { Banner } from "./components/Banner";
import { OpportunityPanel } from "./components/OpportunityPanel";
import { DocumentPanel } from "./components/DocumentPanel";
import { RequirementsTable } from "./components/RequirementsTable";
import { ClarificationsPanel } from "./components/ClarificationsPanel";
import { ProposalActions } from "./components/ProposalActions";
import { CoverageReportPanel } from "./components/CoverageReportPanel";
import { ProposalSections } from "./components/ProposalSections";
import { SowPanel } from "./components/SowPanel";
import { DeckPanel } from "./components/DeckPanel";
import { TextPreview } from "./components/TextPreview";

function App() {
  const presales = usePresales();

  const hasOpportunity = presales.selectedId !== null;

  const hasExtractedDocument = presales.documents.some(
    (document) => document.status === "Extracted"
  );

  const approvedSowCount = presales.sowSections.filter(
    (section) => section.isApproved
  ).length;

  const stages = [
    {
      label: "Opportunity",
      count: 0,
      done: hasOpportunity,
    },
    {
      label: "Documents",
      count: presales.documents.length,
      done: hasExtractedDocument,
    },
    {
      label: "Requirements",
      count: presales.approvedRequirementCount,
      done: presales.approvedRequirementCount > 0,
    },
    {
      label: "Clarifications",
      count: presales.clarifications.length,
      done: presales.clarifications.length > 0,
    },
    {
      label: "Proposal",
      count: presales.approvedSectionCount,
      done: presales.approvedSectionCount > 0,
    },
    {
      label: "Deck",
      count: presales.slides.length,
      done: presales.slides.length > 0,
    },
    {
      label: "Statement of Work",
      count: approvedSowCount,
      done: presales.sowSections.length > 0,
    },
  ];

  return (
    <AppShell
      opportunityName={
        presales.selectedOpportunity?.name ?? ""
      }
      customerName={
        presales.selectedOpportunity?.customerName ?? ""
      }
      industry={presales.selectedOpportunity?.industry ?? ""}
      stages={stages}
    >
      <Banner error={presales.error} message={presales.message} />

      <OpportunityPanel
        opportunities={presales.opportunities}
        selectedId={presales.selectedId}
        loading={presales.loading}
        onSelect={presales.setSelectedId}
        onCreate={presales.createOpportunity}
        onValidationError={presales.notifyError}
      />

      <DocumentPanel
        documents={presales.documents}
        selectedOpportunity={presales.selectedOpportunity}
        extractingId={presales.extractingId}
        onUpload={presales.uploadDocument}
        onExtract={presales.extractRequirements}
        onViewText={presales.viewDocumentText}
        onDelete={presales.deleteDocument}
        onValidationError={presales.notifyError}
      />

      <RequirementsTable
        requirements={presales.requirements}
        onApprove={presales.approveRequirement}
        onApproveAll={presales.approveAllRequirements}
        onUpdate={presales.updateRequirement}
        onDelete={presales.deleteRequirement}
      />

      {hasOpportunity && (
        <ClarificationsPanel
          clarifications={presales.clarifications}
          hasDocuments={hasExtractedDocument}
          requirementCount={presales.requirements.length}
          generating={presales.preparingQuestions}
          onGenerate={presales.generateClarifications}
          onUpdate={presales.updateClarification}
          onApprove={presales.approveClarification}
          onDelete={presales.deleteClarification}
          onExport={presales.exportClarifications}
        />
      )}

      {hasOpportunity && (
        <ProposalActions
          approvedRequirementCount={
            presales.approvedRequirementCount
          }
          hasSections={presales.sections.length > 0}
          generating={presales.generating}
          checking={presales.checking}
          onGenerate={presales.generateProposal}
          onRunCoverage={presales.runCoverageCheck}
          onExport={presales.exportProposal}
        />
      )}

      <CoverageReportPanel report={presales.coverage} />

      <ProposalSections
        sections={presales.sections}
        regeneratingId={presales.regeneratingId}
        onUpdate={presales.updateSection}
        onRegenerate={presales.regenerateSection}
        onApprove={presales.approveSection}
      />

      {hasOpportunity && presales.sections.length > 0 && (
        <DeckPanel
          slides={presales.slides}
          approvedSectionCount={presales.approvedSectionCount}
          generating={presales.buildingDeck}
          onGenerate={presales.generateDeck}
          onExport={presales.exportDeck}
        />
      )}

      {hasOpportunity && presales.sections.length > 0 && (
        <SowPanel
          sowSections={presales.sowSections}
          proposalSections={presales.sections}
          generating={presales.draftingSow}
          onGenerate={presales.generateSow}
          onUpdate={presales.updateSowSection}
          onApprove={presales.approveSowSection}
          onExport={presales.exportSow}
        />
      )}

      <TextPreview
        text={presales.previewText}
        onClose={() => presales.setPreviewText("")}
      />
    </AppShell>
  );
}

export default App;
