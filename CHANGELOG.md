# Changelog

All notable changes to the PG Management Software will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.0.0] - 2026-04-04

### Added
- Multi-branch PG management with separate branch names
- Tenant onboarding with room allocation
- Monthly rent tracking and pending rent reports
- Payment recording and receipt generation (PDF via QuestPDF)
- Role-based access control (SuperAdmin, Admin, Manager)
- JWT authentication with refresh tokens
- Owner management with email updates
- Room and bed management per branch
- Expense tracking per branch
- Dashboard with occupancy and financial summaries
- Email notifications via AWS SES / MailKit
- Background jobs via Hangfire (rent generation, reminders)
- Structured logging with Serilog (File + Seq sinks)
- Angular web UI for administration
- Flutter mobile app for on-the-go management
- Swagger API documentation
