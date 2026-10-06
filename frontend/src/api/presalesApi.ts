import { http, API_BASE_URL } from "./client";
import type {
  ClarificationItem,
  CoverageReport,
  DeckSlideItem,
  DocumentItem,
  GenerateClarificationsResult,
  GenerateDeckResult,
  GenerateProposalResult,
  GenerateSowResult,
  Opportunity,
  OpportunityForm,
  RequirementItem,
  SectionItem,
  SowSectionItem,
  UploadResult,
} from "../types";

export async function getOpportunities(): Promise<Opportunity[]> {
  const response = await http.get<Opportunity[]>("/Opportunities");
  return response.data;
}

export async function createOpportunity(
  form: OpportunityForm
): Promise<Opportunity> {
  const response = await http.post<Opportunity>(
    "/Opportunities",
    form
  );
  return response.data;
}

export async function getDocuments(
  opportunityId: number
): Promise<DocumentItem[]> {
  const response = await http.get<DocumentItem[]>(
    `/Documents/by-opportunity/${opportunityId}`
  );
  return response.data;
}

export async function uploadDocument(
  opportunityId: number,
  file: File
): Promise<UploadResult> {
  const formData = new FormData();
  formData.append("opportunityId", String(opportunityId));
  formData.append("file", file);

  const response = await http.post<UploadResult>(
    "/Documents/upload",
    formData
  );
  return response.data;
}

export async function getDocumentText(
  documentId: number
): Promise<string> {
  const response = await http.get<{ extractedText: string }>(
    `/Documents/${documentId}/text`
  );
  return response.data.extractedText;
}

export async function deleteDocument(
  documentId: number
): Promise<void> {
  await http.delete(`/Documents/${documentId}`);
}

export async function getRequirements(
  opportunityId: number
): Promise<RequirementItem[]> {
  const response = await http.get<RequirementItem[]>(
    `/Requirements/by-opportunity/${opportunityId}`
  );
  return response.data;
}

export async function extractRequirements(
  documentId: number
): Promise<{ count: number }> {
  const response = await http.post<{ count: number }>(
    `/Requirements/extract/${documentId}`
  );
  return response.data;
}

export async function updateRequirement(
  requirement: RequirementItem
): Promise<void> {
  await http.put(`/Requirements/${requirement.id}`, requirement);
}

export async function approveRequirement(
  requirementId: number
): Promise<void> {
  await http.post(`/Requirements/${requirementId}/approve`);
}

export async function deleteRequirement(
  requirementId: number
): Promise<void> {
  await http.delete(`/Requirements/${requirementId}`);
}

export async function getClarifications(
  opportunityId: number
): Promise<ClarificationItem[]> {
  const response = await http.get<ClarificationItem[]>(
    `/Clarifications/by-opportunity/${opportunityId}`
  );
  return response.data;
}

export async function generateClarifications(
  opportunityId: number
): Promise<GenerateClarificationsResult> {
  const response = await http.post<GenerateClarificationsResult>(
    `/Clarifications/generate/${opportunityId}`
  );
  return response.data;
}

export async function updateClarification(
  item: ClarificationItem
): Promise<void> {
  await http.put(`/Clarifications/${item.id}`, item);
}

export async function approveClarification(
  id: number
): Promise<void> {
  await http.post(`/Clarifications/${id}/approve`);
}

export async function deleteClarification(
  id: number
): Promise<void> {
  await http.delete(`/Clarifications/${id}`);
}

export function exportClarificationsUrl(
  opportunityId: number
): string {
  return `${API_BASE_URL}/Clarifications/export/${opportunityId}`;
}

export async function getSections(
  opportunityId: number
): Promise<SectionItem[]> {
  const response = await http.get<SectionItem[]>(
    `/Proposals/by-opportunity/${opportunityId}`
  );
  return response.data;
}

export async function generateProposal(
  opportunityId: number
): Promise<GenerateProposalResult> {
  const response = await http.post<GenerateProposalResult>(
    `/Proposals/generate/${opportunityId}`
  );
  return response.data;
}

export async function regenerateSection(
  sectionId: number
): Promise<void> {
  await http.post(`/Proposals/regenerate-section/${sectionId}`);
}

export async function updateSection(
  section: SectionItem
): Promise<void> {
  await http.put(`/Proposals/${section.id}`, section);
}

export async function approveSection(
  sectionId: number
): Promise<void> {
  await http.post(`/Proposals/${sectionId}/approve`);
}

export function exportProposalUrl(
  opportunityId: number
): string {
  return `${API_BASE_URL}/Proposals/export/${opportunityId}`;
}

export async function getCoverageReport(
  opportunityId: number
): Promise<CoverageReport> {
  const response = await http.get<CoverageReport>(
    `/Coverage/${opportunityId}`
  );
  return response.data;
}

export async function getSowSections(
  opportunityId: number
): Promise<SowSectionItem[]> {
  const response = await http.get<SowSectionItem[]>(
    `/Sow/by-opportunity/${opportunityId}`
  );
  return response.data;
}

export async function generateSow(
  opportunityId: number
): Promise<GenerateSowResult> {
  const response = await http.post<GenerateSowResult>(
    `/Sow/generate/${opportunityId}`
  );
  return response.data;
}

export async function updateSowSection(
  section: SowSectionItem
): Promise<void> {
  await http.put(`/Sow/${section.id}`, section);
}

export async function approveSowSection(
  sectionId: number
): Promise<void> {
  await http.post(`/Sow/${sectionId}/approve`);
}

export function exportSowUrl(opportunityId: number): string {
  return `${API_BASE_URL}/Sow/export/${opportunityId}`;
}

export async function getDeckSlides(
  opportunityId: number
): Promise<DeckSlideItem[]> {
  const response = await http.get<DeckSlideItem[]>(
    `/Deck/by-opportunity/${opportunityId}`
  );
  return response.data;
}

export async function generateDeck(
  opportunityId: number
): Promise<GenerateDeckResult> {
  const response = await http.post<GenerateDeckResult>(
    `/Deck/generate/${opportunityId}`
  );
  return response.data;
}

export function exportDeckUrl(opportunityId: number): string {
  return `${API_BASE_URL}/Deck/export/${opportunityId}`;
}
