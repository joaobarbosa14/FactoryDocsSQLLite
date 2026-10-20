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
