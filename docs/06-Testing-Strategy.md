# Testing Strategy

## Unit Tests
- Required document number, title and category validation.
- Allowed document status values.
- Status transition rules when a workflow service is added.
- CSV row validation.

## API Integration Tests
- Create a document and retrieve it.
- Search by document number and title.
- Reject duplicate document numbers.
- Return 404 for an unknown identifier.
- Reject invalid status values.

## Importer Tests
- Valid rows are submitted.
- Missing document number/title is reported.
- Duplicate or API-rejected rows are reported.
- The import summary includes total, imported and failed counts.

## Manual Demonstration Checklist
1. Start the API and database.
2. Open the frontend.
3. Search for a seeded document.
4. Create a new document.
5. Change its status.
6. Run the VB.NET importer against a sample CSV.
7. Confirm the imported document appears in the UI.
