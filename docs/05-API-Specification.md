# API Specification

Base URL: `http://localhost:<port>/api`

## GET /health
Returns a simple health status.

## GET /documents
Returns all documents. Optional query parameter: `search`.

Example: `GET /api/documents?search=machine`

## GET /documents/{id}
Returns one document by identifier, or `404 Not Found`.

## POST /documents
Creates a document.

```json
{
  "documentNumber": "WI-MFG-001",
  "title": "Machine Start-up Work Instruction",
  "category": "Production",
  "description": "Demonstration work instruction.",
  "status": "Draft"
}
```

Expected outcomes:
- `201 Created` when successful.
- `400 Bad Request` when validation fails.
- `409 Conflict` when the document number already exists.

## PATCH /documents/{id}/status
Updates the document status.

```json
{
  "status": "PendingApproval"
}
```

A production implementation should restrict valid status transitions based on the user's role and the current document state.
