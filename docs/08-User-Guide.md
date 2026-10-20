# User Guide

## Finding a Document
Use the search field to search by document number, title, category or description. The document table shows the current status and creation date.

## Creating a Document
Enter a unique document number, title, category and optional description. Choose an initial status and submit the form. The API validates required fields and saves the record to SQL Server.

## Changing Status
Use the API status endpoint or the frontend control to update a document's status. In this MVP, status changes are not yet protected by a full approval-role workflow.

## Importing Legacy Data
Prepare a UTF-8 CSV file with the columns:

`DocumentNumber,Title,Category,Description,Status`

Run the VB.NET importer with the CSV path and API base URL. Review the summary and any row-level errors.
