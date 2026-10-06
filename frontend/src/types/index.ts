export interface Opportunity {
  id: number;
  name: string;
  customerName: string;
  industry: string;
  region: string;
  estimatedValue: number;
  createdOn: string;
}

export interface OpportunityForm {
  name: string;
  customerName: string;
  industry: string;
  region: string;
  estimatedValue: number;
}

export interface DocumentItem {
  id: number;
  fileName: string;
  fileSizeBytes: number;
  pageCount: number;
  status: string;
  errorMessage: string | null;
  textLength: number;
}

export interface RequirementItem {
  id: number;
  opportunityId: number;
  documentId: number;
  code: string;
  description: string;
  category: string;
  priority: string;
  isMandatory: boolean;
  sourceReference: string;
  isApproved: boolean;
  createdOn: string;
}

export interface ClarificationItem {
  id: number;
  opportunityId: number;
  reference: string;
  question: string;
  rationale: string;
  category: string;
  impact: string;
  relatedRequirements: string;
  sourceReference: string;
  isApproved: boolean;
  isEdited: boolean;
  createdOn: string;
  updatedOn: string | null;
}

export interface SectionItem {
  id: number;
  opportunityId: number;
  sectionOrder: number;
  sectionName: string;
  content: string;
  coveredRequirements: string;
  assumptions: string;
  isApproved: boolean;
  isEdited: boolean;
  generatedOn: string;
  updatedOn: string | null;
}

export interface SowSectionItem {
  id: number;
  opportunityId: number;
  sectionOrder: number;
  sectionName: string;
  content: string;
  coveredRequirements: string;
  isApproved: boolean;
  isEdited: boolean;
  generatedOn: string;
  updatedOn: string | null;
}

export interface DeckSlideItem {
  id: number;
  opportunityId: number;
  slideOrder: number;
  layout: string;
  title: string;
  bullets: string;
  layoutData: string;
  speakerNotes: string;
  sourceSection: string;
  generatedOn: string;
}


export interface RequirementCoverage {
  code: string;
  description: string;
  category: string;
  priority: string;
  isMandatory: boolean;
  isCovered: boolean;
  isClaimedOnly: boolean;
  coveredInSections: string[];
}

export interface SectionCoverage {
  sectionOrder: number;
  sectionName: string;
  isApproved: boolean;
  requirementsCovered: number;
  placeholderCount: number;
  placeholders: string[];
  unknownCodes: string[];
}

export interface CustomerNameCheck {
  opportunityCustomerName: string;
  foundInSourceDocuments: boolean;
  appearsInProposal: boolean;
  isMismatch: boolean;
  sectionsNamingCustomer: string[];
}

export interface CoverageReport {
  totalApproved: number;
  coveredCount: number;
  uncoveredCount: number;
  uncoveredMandatoryCount: number;
  coveragePercent: number;
  totalPlaceholders: number;
  sectionsAwaitingApproval: number;
  isReadyForRelease: boolean;
  customerName: CustomerNameCheck;
  blockers: string[];
  requirements: RequirementCoverage[];
  sections: SectionCoverage[];
}

export interface GenerateClarificationsResult {
  questionsGenerated: number;
  blockingCount: number;
  requirementsConsidered: number;
}

export interface GenerateProposalResult {
  requirementsUsed: number;
  sectionsGenerated: number;
  failures: string[];
}

export interface GenerateSowResult {
  sectionsGenerated: number;
  requirementsUsed: number;
  failures: string[];
}

export interface GenerateDeckResult {
  slidesGenerated: number;
  sectionsUsed: number;
}

export interface UploadResult {
  status: string;
  pageCount: number;
  errorMessage: string | null;
}
