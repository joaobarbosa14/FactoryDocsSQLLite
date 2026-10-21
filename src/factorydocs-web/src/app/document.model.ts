export interface DocumentRecord {
  id: number;
  documentNumber: string;
  title: string;
  category: string;
  description?: string | null;
  status: string;
  createdAtUtc: string;
}

export interface CreateDocumentRequest {
  documentNumber: string;
  title: string;
  category: string;
  description?: string;
  status: string;
}

export interface DocumentVersion {
  id: number;
  documentId: number;
  versionNumber: string;
  title: string;
  category: string;
  description?: string | null;
  createdBy: string;
  changeReason: string;
  createdAtUtc: string;
}

export interface CreateDocumentVersionRequest {
  versionNumber: string;
  title: string;
  category: string;
  description?: string | null;
  createdBy: string;
  changeReason: string;
}