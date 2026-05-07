# TIMS - Travel Information Management System

## Overview

TIMS is a complex, mission-based travel management system built for IMF operations. It integrates with PeopleSoft Finance and provides comprehensive travel authorization, expense claims processing, and payment management capabilities.

## Key Features

### 1. Mission Management
- Mission creation and tracking
- Mission types: Official, Training, Conference, Consultation
- Budget allocation and tracking
- PeopleSoft project integration

### 2. Travel Requests
- Travel authorization workflow
- Multi-level approval (Manager, Finance, Director)
- Integration with missions
- PeopleSoft request synchronization

### 3. Claims Processing
- Expense claim submission
- **Three-Way Matching**: Budget-Invoice-Approval validation
- Automated variance detection
- Multiple currency support

### 4. Corporate Banking/Payment Processing
- E-Transfer, Wire Transfer, Check, Corporate Card payments
- Bank account management
- Payment status tracking
- PeopleSoft voucher integration

### 5. Budget Management
- Budget allocation by department
- Fiscal year tracking
- Committed vs. Spent tracking
- PeopleSoft budget synchronization

## Entity Structure

### Core Entities
- **tims_mission**: Mission records
- **tims_travel_request**: Travel authorization requests
- **tims_claim**: Expense claims
- **tims_budget**: Budget allocations
- **tims_invoice**: Vendor invoices
- **tims_payment**: Payment records
- **tims_bank_account**: Corporate bank accounts
- **tims_approval**: Approval workflow records

## PeopleSoft Integration

All entities include PeopleSoft integration fields:
- `peoplesoft_project_id` (Mission)
- `peoplesoft_request_id` (Travel Request)
- `peoplesoft_voucher_id` (Claim)
- `peoplesoft_invoice_id` (Invoice)
- `peoplesoft_payment_id` (Payment)
- `peoplesoft_budget_id` (Budget)
- `peoplesoft_account_id` (Bank Account)

## Three-Way Matching Process

The claims processing includes automated three-way matching:

1. **Budget Amount**: Allocated budget for the claim
2. **Invoice Amount**: Vendor invoice total
3. **Claim Amount**: Submitted claim total

**Match Statuses:**
- `matched`: All amounts within tolerance
- `variance`: Minor variance (requires review)
- `failed`: Significant variance (rejected)

## Installation

1. Add plugin to WebVella ERP solution
2. Build and deploy
3. Plugin will auto-initialize on first run
4. Creates all entities, relations, and sitemap structure

## Branding

TIMS includes IMF and PeopleSoft branding throughout the application.

## Development

### Services
- `TimsService`: Core business logic for missions, claims, and payments

### Hooks
- `ClaimHooks`: Automated three-way matching on claim submission
- `MissionHooks`: Mission status validation and transitions

## Version History

- **20250101**: Initial release with core TIMS functionality
