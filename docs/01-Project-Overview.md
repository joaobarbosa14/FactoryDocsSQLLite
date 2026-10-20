# FactoryDocs — Project Overview

## 1. Executive Summary

FactoryDocs is a lightweight Document Management System (DMS) designed to demonstrate how an industrial organisation can centralise technical documentation, manage document status, and create a foundation for revision control and approval workflows.

The solution uses an Angular and TypeScript frontend, an ASP.NET Core REST API written in C#, and Microsoft SQL Server. A separate VB.NET console utility demonstrates importing document metadata from a legacy CSV export through the API.

This is an independent technical demonstration inspired by common industrial IT requirements. It is not an official Mürdter application and does not use company-confidential information.

## 2. Business Objectives

- Centralise technical document metadata.
- Make documents searchable by number, title, category, and status.
- Establish a clear lifecycle for document status.
- Preserve traceability for important changes.
- Demonstrate integration between a legacy utility and a modern API.
- Provide clear setup and technical documentation.

## 3. Target Users

- Production employees who need to find current work instructions.
- Maintenance employees who consult equipment procedures.
- Quality staff who manage quality-related documents.
- Document controllers who maintain and review documentation.
- IT administrators who support the application.

## 4. MVP Scope

### Included
- Document metadata CRUD foundations.
- Search and filtering.
- Status changes.
- SQL Server persistence.
- API documentation.
- A small Angular UI.
- CSV import through a VB.NET utility.
- English technical documentation.

### Planned next
- Revision records and file uploads.
- Approval and rejection workflow.
- Authentication and role-based permissions.
- Full audit history.
- Windows/Active Directory integration.
- Notifications and external-system integration.

## 5. Success Criteria

The MVP is successful when a user can create and search for a document, persist it in SQL Server, change its status through the API, and import sample records using the VB.NET utility. Setup steps should be repeatable and understandable by another developer.
