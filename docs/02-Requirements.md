# Requirements Specification

## Functional Requirements

| ID | Requirement | Priority |
|---|---|---|
| FR-01 | The system shall list documents. | Must |
| FR-02 | Users shall search by document number, title, category, or description. | Must |
| FR-03 | The system shall create a document with required metadata. | Must |
| FR-04 | The system shall persist data in Microsoft SQL Server. | Must |
| FR-05 | The system shall allow controlled status changes. | Must |
| FR-06 | The legacy importer shall read document records from CSV. | Must |
| FR-07 | The importer shall report invalid rows without silently discarding them. | Must |
| FR-08 | The system should preserve document revision history. | Should |
| FR-09 | The system should record important actions in an audit log. | Should |
| FR-10 | The system should support role-based permissions. | Should |

## Non-Functional Requirements

- **Maintainability:** keep API endpoints, data access and domain entities logically separated.
- **Security:** validate all input server-side; do not expose files directly from a public folder.
- **Reliability:** return clear API errors and do not silently accept invalid imports.
- **Usability:** provide a clear search-first document list.
- **Traceability:** make status and revision changes explainable.
- **Portability:** support local development with documented configuration.
- **Documentation:** provide setup instructions, API examples and a short user guide.

## Initial Document Statuses

- `Draft`
- `PendingApproval`
- `Approved`
- `Rejected`
- `Archived`

The initial API allows status updates for demonstration. A production version should enforce transitions based on user roles and a defined workflow.
