import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DocumentService } from './document.service';
import { CreateDocumentRequest, DocumentRecord } from './document.model';

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
      next: items => { this.documents = items; this.loading = false; this.cdr.detectChanges(); },
      error: () => { this.error = 'Could not load documents. Check that the API is running and the API URL is correct.'; this.loading = false; this.cdr.detectChanges(); }
    });
  }

  createDocument(): void {
    this.message = '';
    this.error = '';
    this.documentsApi.createDocument(this.form).subscribe({
      next: created => {
        this.message = `Document ${created.documentNumber} created successfully.`;
        this.form = { documentNumber: '', title: '', category: 'Production', description: '', status: 'Draft' };
        this.loadDocuments();
      },
      error: err => {
        this.error = err?.error?.message ?? 'Could not create document. Check required fields and duplicate document numbers.';
      }
    });
  }

  changeStatus(document: DocumentRecord, status: string): void {
    this.documentsApi.updateStatus(document.id, status).subscribe({
      next: () => this.loadDocuments(),
      error: () => this.error = 'Could not update document status.'
    });
  }

  get approvedCount(): number { return this.documents.filter(d => d.status === 'Approved').length; }
  get pendingCount(): number { return this.documents.filter(d => d.status === 'PendingApproval').length; }
  get draftCount(): number { return this.documents.filter(d => d.status === 'Draft').length; }
}
