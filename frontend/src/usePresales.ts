import { useCallback, useEffect, useState } from "react";
import * as api from "./api/presalesApi";
import { errorMessage } from "./api/client";
import type {
  ClarificationItem,
  CoverageReport,
  DeckSlideItem,
  DocumentItem,
  Opportunity,
  OpportunityForm,
  RequirementItem,
  SectionItem,
  SowSectionItem,
} from "./types";

export function usePresales() {
  const [opportunities, setOpportunities] = useState<Opportunity[]>(
    []
  );
  const [documents, setDocuments] = useState<DocumentItem[]>([]);
  const [requirements, setRequirements] = useState<
    RequirementItem[]
  >([]);
  const [clarifications, setClarifications] = useState<
    ClarificationItem[]
  >([]);
  const [sections, setSections] = useState<SectionItem[]>([]);
  const [sowSections, setSowSections] = useState<SowSectionItem[]>(
    []
  );
  const [slides, setSlides] = useState<DeckSlideItem[]>([]);
  const [coverage, setCoverage] = useState<CoverageReport | null>(
    null
  );

  const [selectedId, setSelectedId] = useState<number | null>(null);
  const [previewText, setPreviewText] = useState("");

  const [loading, setLoading] = useState(true);
  const [extractingId, setExtractingId] = useState<number | null>(
    null
  );
  const [preparingQuestions, setPreparingQuestions] =
    useState(false);
  const [generating, setGenerating] = useState(false);
  const [regeneratingId, setRegeneratingId] = useState<
    number | null
  >(null);
  const [checking, setChecking] = useState(false);
  const [draftingSow, setDraftingSow] = useState(false);
  const [buildingDeck, setBuildingDeck] = useState(false);

  const [error, setError] = useState("");
  const [message, setMessage] = useState("");

  function notifyError(text: string) {
    setError(text);
    setMessage("");
  }

  function notifySuccess(text: string) {
    setMessage(text);
    setError("");
  }

  const loadOpportunities = useCallback(async () => {
    try {
      setLoading(true);
      setOpportunities(await api.getOpportunities());
      setError("");
    } catch (caught) {
      notifyError(
        errorMessage(
          caught,
          "Could not reach the backend. Is the API running?"
        )
      );
    } finally {
      setLoading(false);
    }
  }, []);

  const loadDocuments = useCallback(async (id: number) => {
    try {
      setDocuments(await api.getDocuments(id));
    } catch {
      setDocuments([]);
    }
  }, []);

  const loadRequirements = useCallback(async (id: number) => {
    try {
      setRequirements(await api.getRequirements(id));
    } catch {
      setRequirements([]);
    }
  }, []);

  const loadClarifications = useCallback(async (id: number) => {
    try {
      setClarifications(await api.getClarifications(id));
    } catch {
      setClarifications([]);
    }
  }, []);

  const loadSections = useCallback(async (id: number) => {
    try {
      setSections(await api.getSections(id));
    } catch {
      setSections([]);
    }
  }, []);

  const loadSowSections = useCallback(async (id: number) => {
    try {
      setSowSections(await api.getSowSections(id));
    } catch {
      setSowSections([]);
    }
  }, []);

  const loadSlides = useCallback(async (id: number) => {
    try {
      setSlides(await api.getDeckSlides(id));
    } catch {
      setSlides([]);
    }
  }, []);

  useEffect(() => {
    loadOpportunities();
  }, [loadOpportunities]);

  useEffect(() => {
    if (selectedId === null) {
      return;
    }

    loadDocuments(selectedId);
    loadRequirements(selectedId);
    loadClarifications(selectedId);
    loadSections(selectedId);
    loadSowSections(selectedId);
    loadSlides(selectedId);

    setCoverage(null);
    setPreviewText("");
  }, [
    selectedId,
    loadDocuments,
    loadRequirements,
    loadClarifications,
    loadSections,
    loadSowSections,
    loadSlides,
  ]);

  async function createOpportunity(form: OpportunityForm) {
    try {
      await api.createOpportunity(form);
      notifySuccess("Opportunity created.");
      await loadOpportunities();
    } catch (caught) {
      notifyError(
        errorMessage(caught, "Could not create the opportunity.")
      );
    }
  }

  async function uploadDocument(file: File) {
    if (selectedId === null) {
      return;
    }

    try {
      const result = await api.uploadDocument(selectedId, file);

      if (result.status === "Failed") {
        notifyError(
          `Extraction failed: ${result.errorMessage ?? "unknown"}`
        );
      } else {
        notifySuccess(
          `Text extracted. Pages: ${result.pageCount}`
        );
      }

      await loadDocuments(selectedId);
    } catch (caught) {
      notifyError(errorMessage(caught, "Upload failed."));
    }
  }

  async function extractRequirements(documentId: number) {
    try {
      setExtractingId(documentId);
      const result = await api.extractRequirements(documentId);

      notifySuccess(
        `The model extracted ${result.count} requirement(s).`
      );

      if (selectedId !== null) {
        await loadRequirements(selectedId);
      }
    } catch (caught) {
      notifyError(
        errorMessage(caught, "Requirement extraction failed.")
      );
    } finally {
      setExtractingId(null);
    }
  }

  async function viewDocumentText(documentId: number) {
    try {
      setPreviewText(await api.getDocumentText(documentId));
    } catch (caught) {
      notifyError(
        errorMessage(caught, "Could not load the text.")
      );
    }
  }

  async function deleteDocument(documentId: number) {
    try {
      await api.deleteDocument(documentId);
      setPreviewText("");

      if (selectedId !== null) {
        await loadDocuments(selectedId);
        await loadRequirements(selectedId);
      }
    } catch (caught) {
      notifyError(errorMessage(caught, "Delete failed."));
    }
  }

  async function approveRequirement(id: number) {
    try {
      await api.approveRequirement(id);

      if (selectedId !== null) {
        await loadRequirements(selectedId);
      }
    } catch (caught) {
      notifyError(errorMessage(caught, "Approve failed."));
    }
  }

  async function approveAllRequirements() {
    const pending = requirements.filter(
      (requirement) => !requirement.isApproved
    );

    if (pending.length === 0) {
      notifySuccess("All requirements are already approved.");
      return;
    }

    try {
      for (const requirement of pending) {
        await api.approveRequirement(requirement.id);
      }

      notifySuccess(`${pending.length} requirement(s) approved.`);

      if (selectedId !== null) {
        await loadRequirements(selectedId);
      }
    } catch (caught) {
      notifyError(errorMessage(caught, "Bulk approve failed."));
    }
  }

  async function updateRequirement(requirement: RequirementItem) {
    try {
      await api.updateRequirement(requirement);

      if (selectedId !== null) {
        await loadRequirements(selectedId);
      }
    } catch (caught) {
      notifyError(
        errorMessage(caught, "Could not save the edit.")
      );
    }
  }

  async function deleteRequirement(id: number) {
    try {
      await api.deleteRequirement(id);

      if (selectedId !== null) {
        await loadRequirements(selectedId);
      }
    } catch (caught) {
      notifyError(errorMessage(caught, "Delete failed."));
    }
  }

  async function generateClarifications() {
    if (selectedId === null) {
      return;
    }

    try {
      setPreparingQuestions(true);

      const result = await api.generateClarifications(selectedId);

      notifySuccess(
        `${result.questionsGenerated} question(s) prepared, ` +
          `${result.blockingCount} marked blocking.`
      );

      await loadClarifications(selectedId);
    } catch (caught) {
      notifyError(
        errorMessage(caught, "Could not prepare the questions.")
      );
    } finally {
      setPreparingQuestions(false);
    }
  }

  async function updateClarification(item: ClarificationItem) {
    try {
      await api.updateClarification(item);

      if (selectedId !== null) {
        await loadClarifications(selectedId);
      }
    } catch (caught) {
      notifyError(
        errorMessage(caught, "Could not save the question.")
      );
    }
  }

  async function approveClarification(id: number) {
    try {
      await api.approveClarification(id);

      if (selectedId !== null) {
        await loadClarifications(selectedId);
      }
    } catch (caught) {
      notifyError(errorMessage(caught, "Approve failed."));
    }
  }

  async function deleteClarification(id: number) {
    try {
      await api.deleteClarification(id);

      if (selectedId !== null) {
        await loadClarifications(selectedId);
      }
    } catch (caught) {
      notifyError(errorMessage(caught, "Delete failed."));
    }
  }

  function exportClarifications() {
    if (selectedId === null) {
      return;
    }

    window.open(api.exportClarificationsUrl(selectedId), "_blank");
  }

  async function generateProposal() {
    if (selectedId === null) {
      return;
    }

    try {
      setGenerating(true);
      setCoverage(null);

      const result = await api.generateProposal(selectedId);

      notifySuccess(
        `${result.sectionsGenerated} section(s) generated from ` +
          `${result.requirementsUsed} approved requirement(s).`
      );

      if (result.failures.length > 0) {
        notifyError(
          `Some sections failed: ${result.failures.join(" | ")}`
        );
      }

      await loadSections(selectedId);
    } catch (caught) {
      notifyError(
        errorMessage(caught, "Proposal generation failed.")
      );
    } finally {
      setGenerating(false);
    }
  }

  async function regenerateSection(sectionId: number) {
    try {
      setRegeneratingId(sectionId);
      await api.regenerateSection(sectionId);
      setCoverage(null);

      if (selectedId !== null) {
        await loadSections(selectedId);
      }
    } catch (caught) {
      notifyError(errorMessage(caught, "Regeneration failed."));
    } finally {
      setRegeneratingId(null);
    }
  }

  async function updateSection(section: SectionItem) {
    try {
      await api.updateSection(section);
      setCoverage(null);

      if (selectedId !== null) {
        await loadSections(selectedId);
      }
    } catch (caught) {
      notifyError(
        errorMessage(caught, "Could not save the section.")
      );
    }
  }

  async function approveSection(sectionId: number) {
    try {
      await api.approveSection(sectionId);
      setCoverage(null);

      if (selectedId !== null) {
        await loadSections(selectedId);
      }
    } catch (caught) {
      notifyError(errorMessage(caught, "Approve failed."));
    }
  }

  async function runCoverageCheck() {
    if (selectedId === null) {
      return;
    }

    try {
      setChecking(true);
      setCoverage(await api.getCoverageReport(selectedId));
      setError("");
    } catch (caught) {
      notifyError(errorMessage(caught, "Coverage check failed."));
    } finally {
      setChecking(false);
    }
  }

  function exportProposal() {
    if (selectedId === null) {
      return;
    }

    window.open(api.exportProposalUrl(selectedId), "_blank");
  }

  async function generateSow() {
    if (selectedId === null) {
      return;
    }

    try {
      setDraftingSow(true);

      const result = await api.generateSow(selectedId);

      notifySuccess(
        `${result.sectionsGenerated} SOW section(s) drafted from ` +
          `${result.requirementsUsed} approved requirement(s).`
      );

      if (result.failures.length > 0) {
        notifyError(
          `Some sections failed: ${result.failures.join(" | ")}`
        );
      }

      await loadSowSections(selectedId);
    } catch (caught) {
      notifyError(
        errorMessage(caught, "Statement of Work drafting failed.")
      );
    } finally {
      setDraftingSow(false);
    }
  }

  async function updateSowSection(section: SowSectionItem) {
    try {
      await api.updateSowSection(section);

      if (selectedId !== null) {
        await loadSowSections(selectedId);
      }
    } catch (caught) {
      notifyError(
        errorMessage(caught, "Could not save the section.")
      );
    }
  }

  async function approveSowSection(sectionId: number) {
    try {
      await api.approveSowSection(sectionId);

      if (selectedId !== null) {
        await loadSowSections(selectedId);
      }
    } catch (caught) {
      notifyError(errorMessage(caught, "Approve failed."));
    }
  }

  function exportSow() {
    if (selectedId === null) {
      return;
    }

    window.open(api.exportSowUrl(selectedId), "_blank");
  }

  async function generateDeck() {
    if (selectedId === null) {
      return;
    }

    try {
      setBuildingDeck(true);

      const result = await api.generateDeck(selectedId);

      notifySuccess(
        `${result.slidesGenerated} slide(s) built from ` +
          `${result.sectionsUsed} approved section(s).`
      );

      await loadSlides(selectedId);
    } catch (caught) {
      notifyError(errorMessage(caught, "Deck build failed."));
    } finally {
      setBuildingDeck(false);
    }
  }

  function exportDeck() {
    if (selectedId === null) {
      return;
    }

    window.open(api.exportDeckUrl(selectedId), "_blank");
  }

  const selectedOpportunity = opportunities.find(
    (opportunity) => opportunity.id === selectedId
  );

  const approvedRequirementCount = requirements.filter(
    (requirement) => requirement.isApproved
  ).length;

  const approvedSectionCount = sections.filter(
    (section) => section.isApproved
  ).length;

  return {
    opportunities,
    documents,
    requirements,
    clarifications,
    sections,
    sowSections,
    slides,
    coverage,
    selectedId,
    selectedOpportunity,
    approvedRequirementCount,
    approvedSectionCount,
    previewText,
    loading,
    extractingId,
    preparingQuestions,
    generating,
    regeneratingId,
    checking,
    draftingSow,
    buildingDeck,
    error,
    message,
    setSelectedId,
    setPreviewText,
    notifyError,
    createOpportunity,
    uploadDocument,
    extractRequirements,
    viewDocumentText,
    deleteDocument,
    approveRequirement,
    approveAllRequirements,
    updateRequirement,
    deleteRequirement,
    generateClarifications,
    updateClarification,
    approveClarification,
    deleteClarification,
    exportClarifications,
    generateProposal,
    regenerateSection,
    updateSection,
    approveSection,
    runCoverageCheck,
    exportProposal,
    generateSow,
    updateSowSection,
    approveSowSection,
    exportSow,
    generateDeck,
    exportDeck,
  };
}
