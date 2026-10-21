import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { CreateDocumentRequest, DocumentRecord, 
  CreateDocumentVersionRequest, DocumentVersion } from './document.model';

@Injectable({ providedIn: 'root' })
export class DocumentService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = 'http://localhost:5000/api/documents';

  getDocuments(search = ''): Observable<DocumentRecord[]> {
    let params = new HttpParams();
    if (search.trim()) params = params.set('search', search.trim());
    return this.http.get<DocumentRecord[]>(this.apiUrl, { params });
  }

  createDocument(request: CreateDocumentRequest): Observable<DocumentRecord> {
    return this.http.post<DocumentRecord>(this.apiUrl, request);
  }

  updateStatus(id: number, status: string): Observable<DocumentRecord> {
    return this.http.patch<DocumentRecord>(`${this.apiUrl}/${id}/status`, { status });
  }

  getVersions(id: number): Observable<DocumentVersion[]> {
    return this.http.get<DocumentVersion[]>(
      `${this.apiUrl}/${id}/versions`
    );
  }

  createVersion(
    id: number,
    request: CreateDocumentVersionRequest
  ): Observable<DocumentVersion> {
    return this.http.post<DocumentVersion>(
      `${this.apiUrl}/${id}/versions`,
      request
    );
  }

  restoreVersion(
    documentId: number,
    versionId: number,
    request: {
      createdBy: string;
      changeReason: string;
    }
  ): Observable<DocumentVersion> {
    return this.http.post<DocumentVersion>(
      `${this.apiUrl}/${documentId}/versions/${versionId}/restore`,
      request
    );
  }
}
