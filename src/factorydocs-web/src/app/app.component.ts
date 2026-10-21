import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DocumentService } from './document.service';
import { CreateDocumentRequest, DocumentRecord, 
  CreateDocumentVersionRequest, DocumentVersion } from './document.model';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css',
})
export class AppComponent implements OnInit {
  private readonly documentsApi = inject(DocumentService);
  private readonly cdr = inject(ChangeDetectorRef);

  documents: DocumentRecord[] = [];
  search = '';
  loading = false;
  message = '';
  error = '';

  selectedDocument: DocumentRecord | null = null;
  versions: DocumentVersion[] = [];
  historyLoading = false;
  historyError = '';
  savingVersion = false;
  showVersionForm = false;

  versionForm: CreateDocumentVersionRequest = this.emptyVersionForm();

  form: CreateDocumentRequest = {
    documentNumber: '',
    title: '',
    category: 'Production',
    description: '',
    status: 'Draft',
  };

  ngOnInit(): void {
    this.loadDocuments();
  }

  loadDocuments(): void {
    this.loading = true;
    this.error = '';
    this.documentsApi.getDocuments(this.search).subscribe({
      next: (items) => {
        this.documents = items;
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.error =
          'Could not load documents. Check that the API is running and the API URL is correct.';
        this.loading = false;
        this.cdr.detectChanges();
      },
    });
  }

  createDocument(): void {
    this.message = '';
    this.error = '';
    this.documentsApi.createDocument(this.form).subscribe({
      next: (created) => {
        this.message = `Document ${created.documentNumber} created successfully.`;
        this.form = {
          documentNumber: '',
          title: '',
          category: 'Production',
          description: '',
          status: 'Draft',
        };
        this.loadDocuments();
      },
      error: (err) => {
        this.error =
          err?.error?.message ??
          'Could not create document. Check required fields and duplicate document numbers.';
      },
    });
  }

  changeStatus(document: DocumentRecord, status: string): void {
    this.documentsApi.updateStatus(document.id, status).subscribe({
      next: () => this.loadDocuments(),
      error: () => (this.error = 'Could not update document status.'),
    });
  }

  openHistory(document: DocumentRecord): void {
    this.selectedDocument = document;
    this.versions = [];
    this.historyError = '';
    this.showVersionForm = false;
    this.historyLoading = true;
    this.versionForm = this.emptyVersionForm();

    this.documentsApi.getVersions(document.id).subscribe({
      next: versions => {
        this.versions = versions;
        this.historyLoading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.historyError = 'Could not load version history.';
        this.historyLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  closeHistory(): void {
    this.selectedDocument = null;
    this.versions = [];
    this.showVersionForm = false;
    this.historyError = '';
  }

  openVersionForm(): void {
    if (!this.selectedDocument) return;

    const latest = this.versions[0]?.versionNumber ?? '1.0';
    this.versionForm = {
      versionNumber: this.nextMinorVersion(latest),
      title: this.selectedDocument.title,
      category: this.selectedDocument.category,
      description: this.selectedDocument.description ?? '',
      createdBy: '',
      changeReason: ''
    };

    this.showVersionForm = true;
  }

  private nextMinorVersion(current: string): string {
    const match = current.match(/^(\d+)\.(\d+)$/);

    if (!match) return '';

    return `${match[1]}.${Number(match[2]) + 1}`;
  }

  saveVersion(): void {
    if (!this.selectedDocument || this.savingVersion) return;

    this.historyError = '';
    this.savingVersion = true;

    this.documentsApi.createVersion(
      this.selectedDocument.id,
      this.versionForm
    ).subscribe({
      next: created => {
        this.message =
          `Version ${created.versionNumber} created successfully.`;

        this.savingVersion = false;
        this.showVersionForm = false;

        // Atualizar o histórico e os dados atuais.
        if (this.selectedDocument) {
          this.selectedDocument = {
            ...this.selectedDocument,
            title: created.title,
            category: created.category,
            description: created.description
          };

          this.openHistory(this.selectedDocument);
        }

        this.loadDocuments();
      },
      error: err => {
        this.savingVersion = false;
        this.historyError =
          err?.error?.message ??
          'Could not create version. Check the version number and required fields.';
        this.cdr.detectChanges();
      }
    });
  }

  private emptyVersionForm(): CreateDocumentVersionRequest {
    return {
      versionNumber: '1.1',
      title: '',
      category: 'Production',
      description: '',
      createdBy: '',
      changeReason: ''
    };
  }

  get approvedCount(): number {
    return this.documents.filter((d) => d.status === 'Approved').length;
  }
  get pendingCount(): number {
    return this.documents.filter((d) => d.status === 'PendingApproval').length;
  }
  get draftCount(): number {
    return this.documents.filter((d) => d.status === 'Draft').length;
  }
}
