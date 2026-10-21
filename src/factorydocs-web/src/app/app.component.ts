
import { CommonModule } from '@angular/common';
import {
  Component,
  OnInit,
  inject,
  ChangeDetectorRef
} from '@angular/core';
import { FormsModule } from '@angular/forms';

import { DocumentService } from './document.service';

import {
  CreateDocumentRequest,
  CreateDocumentVersionRequest,
  DocumentRecord,
  DocumentVersion
} from './document.model';

interface VersionDifference {
  label: string;
  oldValue: string;
  newValue: string;
  changed: boolean;
}

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css'
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

  showVersionForm = false;
  savingVersion = false;

  versionForm: CreateDocumentVersionRequest = this.emptyVersionForm();

  compareLeftId: number | null = null;
  compareRightId: number | null = null;

  restoreSourceVersion: DocumentVersion | null = null;
  restoreForm = {
    createdBy: '',
    changeReason: ''
  };
  restoring = false;

  form: CreateDocumentRequest = {
    documentNumber: '',
    title: '',
    category: 'Production',
    description: '',
    status: 'Draft'
  };

  ngOnInit(): void {
    this.loadDocuments();
  }

  loadDocuments(): void {
    this.loading = true;
    this.error = '';

    this.documentsApi.getDocuments(this.search).subscribe({
      next: items => {
        this.documents = items;
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.error =
          'Could not load documents. Check that the API is running and the API URL is correct.';
        this.loading = false;
        this.cdr.detectChanges();
      }
    });
  }

  createDocument(): void {
    this.message = '';
    this.error = '';

    this.documentsApi.createDocument(this.form).subscribe({
      next: created => {
        this.message =
          `Document ${created.documentNumber} created successfully.`;

        this.form = {
          documentNumber: '',
          title: '',
          category: 'Production',
          description: '',
          status: 'Draft'
        };

        this.loadDocuments();
      },
      error: err => {
        this.error =
          err?.error?.message ??
          'Could not create document. Check required fields and duplicate document numbers.';
      }
    });
  }

  changeStatus(document: DocumentRecord, status: string): void {
    this.documentsApi.updateStatus(document.id, status).subscribe({
      next: () => this.loadDocuments(),
      error: () => {
        this.error = 'Could not update document status.';
      }
    });
  }

  openHistory(document: DocumentRecord): void {
    this.selectedDocument = document;
    this.versions = [];
    this.historyError = '';
    this.message = '';
    this.showVersionForm = false;
    this.restoreSourceVersion = null;
    this.historyLoading = true;

    this.compareLeftId = null;
    this.compareRightId = null;

    this.documentsApi.getVersions(document.id).subscribe({
      next: versions => {
        this.versions = versions;

        if (versions.length > 0) {
          this.compareRightId = versions[0].id;
          this.compareLeftId = versions.length > 1
            ? versions[1].id
            : versions[0].id;
        }

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
    this.restoreSourceVersion = null;
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
    this.restoreSourceVersion = null;
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
        this.savingVersion = false;
        this.message =
          `Version ${created.versionNumber} created successfully.`;

        this.showVersionForm = false;
        this.loadDocuments();
        this.openHistory(this.selectedDocument!);
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

  startRestore(version: DocumentVersion): void {
    this.restoreSourceVersion = version;
    this.restoreForm = {
      createdBy: '',
      changeReason: ''
    };

    this.showVersionForm = false;
    this.historyError = '';
  }

  cancelRestore(): void {
    this.restoreSourceVersion = null;
  }

  confirmRestore(): void {
    if (
      !this.selectedDocument ||
      !this.restoreSourceVersion ||
      this.restoring
    ) {
      return;
    }

    this.historyError = '';
    this.restoring = true;

    const documentId = this.selectedDocument.id;
    const sourceVersionId = this.restoreSourceVersion.id;

    this.documentsApi.restoreVersion(
      documentId,
      sourceVersionId,
      this.restoreForm
    ).subscribe({
      next: restored => {
        this.restoring = false;
        this.restoreSourceVersion = null;

        this.message =
          `Restoration completed. New version: ${restored.versionNumber}.`;

        this.loadDocuments();
        this.openHistory(this.selectedDocument!);
      },
      error: err => {
        this.restoring = false;
        this.historyError =
          err?.error?.message ??
          'Could not restore this version. Check the author and reason.';
        this.cdr.detectChanges();
      }
    });
  }

  private nextMinorVersion(current: string): string {
    const match = current.match(/^(\d+)\.(\d+)$/);

    if (!match) return '';

    return `${match[1]}.${Number(match[2]) + 1}`;
  }

  get compareLeft(): DocumentVersion | undefined {
    return this.versions.find(v => v.id === this.compareLeftId);
  }

  get compareRight(): DocumentVersion | undefined {
    return this.versions.find(v => v.id === this.compareRightId);
  }

  get canCompare(): boolean {
    return !!this.compareLeft &&
      !!this.compareRight &&
      this.compareLeft.id !== this.compareRight.id;
  }

  get differences(): VersionDifference[] {
    const left = this.compareLeft;
    const right = this.compareRight;

    if (!left || !right) return [];

    const fields: Array<{
      label: string;
      oldValue: string;
      newValue: string;
    }> = [
      {
        label: 'Title',
        oldValue: left.title ?? '',
        newValue: right.title ?? ''
      },
      {
        label: 'Category',
        oldValue: left.category ?? '',
        newValue: right.category ?? ''
      },
      {
        label: 'Description',
        oldValue: left.description ?? '',
        newValue: right.description ?? ''
      }
    ];

    return fields.map(field => ({
      ...field,
      changed: field.oldValue !== field.newValue
    }));
  }

  get changedFieldCount(): number {
    return this.differences.filter(field => field.changed).length;
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
    return this.documents.filter(d => d.status === 'Approved').length;
  }

  get pendingCount(): number {
    return this.documents.filter(
      d => d.status === 'PendingApproval'
    ).length;
  }

  get draftCount(): number {
    return this.documents.filter(d => d.status === 'Draft').length;
  }
}