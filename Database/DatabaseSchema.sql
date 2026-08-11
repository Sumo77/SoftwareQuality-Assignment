-- ============================================================================
-- Library Management System - Database Schema
-- SQLite Database Definition
-- ============================================================================
-- This script creates all tables, constraints, and indexes needed for the
-- Library Book Loaning, Returns, and Catalogue Management System.
-- 
-- Core Business Rules Enforced:
-- - Single source of truth for book availability status
-- - Single active reservation per book
-- - Role-based access (Member vs Staff)
-- - 14-day loan period
-- - Loan limit enforcement (2 items per member) handled in application
-- ============================================================================

-- Drop existing tables if they exist (for repeatability during development)
DROP TABLE IF EXISTS Reservations;
DROP TABLE IF EXISTS Loans;
DROP TABLE IF EXISTS Books;
DROP TABLE IF EXISTS Accounts;

-- ============================================================================
-- ACCOUNTS TABLE
-- ============================================================================
-- Stores both Member and Staff accounts with unified structure
-- Role field determines access level (REQ-7, REQ-11)
-- Passwords stored as hashed values (basic security)
-- ============================================================================
CREATE TABLE Accounts (
	AccountID       INTEGER PRIMARY KEY AUTOINCREMENT,
	Username        TEXT NOT NULL UNIQUE,
	PasswordHash    TEXT NOT NULL,
	Role            TEXT NOT NULL CHECK(Role IN ('Member', 'Staff')),
	FirstName       TEXT NOT NULL,
	LastName        TEXT NOT NULL,
	Email           TEXT,
	PhoneNumber     TEXT,
	CreatedDate     TEXT NOT NULL DEFAULT (datetime('now')),
	IsActive        INTEGER NOT NULL DEFAULT 1 CHECK(IsActive IN (0, 1))
);

-- Index for fast login lookups
CREATE INDEX idx_accounts_username ON Accounts(Username);

-- Index for role-based queries
CREATE INDEX idx_accounts_role ON Accounts(Role);


-- ============================================================================
-- BOOKS TABLE
-- ============================================================================
-- Catalogue of all library items
-- Status is the single source of truth: 'Available', 'On Loan', 'Reserved'
-- Status must always reflect actual Loans/Reservations state (REQ-1, REQ-14)
-- ============================================================================
CREATE TABLE Books (
	BookID          INTEGER PRIMARY KEY AUTOINCREMENT,
	ISBN            TEXT UNIQUE,
	Title           TEXT NOT NULL,
	Author          TEXT NOT NULL,
	Publisher       TEXT,
	PublicationYear INTEGER,
	Genre           TEXT,
	Status          TEXT NOT NULL DEFAULT 'Available' 
					CHECK(Status IN ('Available', 'On Loan', 'Reserved')),
	Description     TEXT,
	AddedDate       TEXT NOT NULL DEFAULT (datetime('now'))
);

-- Indexes for catalogue search functionality (REQ-1)
CREATE INDEX idx_books_title ON Books(Title);
CREATE INDEX idx_books_author ON Books(Author);
CREATE INDEX idx_books_status ON Books(Status);
CREATE INDEX idx_books_isbn ON Books(ISBN);


-- ============================================================================
-- LOANS TABLE
-- ============================================================================
-- Tracks all borrowing transactions (current and historical)
-- ReturnDate NULL indicates active loan
-- DueDate calculated as LoanDate + 14 days (REQ-2, REQ-4)
-- Overdue status calculated dynamically: date('now') > DueDate AND ReturnDate IS NULL
-- ============================================================================
CREATE TABLE Loans (
	LoanID          INTEGER PRIMARY KEY AUTOINCREMENT,
	BookID          INTEGER NOT NULL,
	MemberID        INTEGER NOT NULL,
	LoanDate        TEXT NOT NULL,
	DueDate         TEXT NOT NULL,
	ReturnDate      TEXT,
	ReturnCondition TEXT,
	FOREIGN KEY (BookID) REFERENCES Books(BookID) ON DELETE RESTRICT,
	FOREIGN KEY (MemberID) REFERENCES Accounts(AccountID) ON DELETE RESTRICT
);

-- Index for finding active loans by member (REQ-5, REQ-8)
CREATE INDEX idx_loans_member ON Loans(MemberID);

-- Index for finding loans by book
CREATE INDEX idx_loans_book ON Loans(BookID);

-- Index for finding active loans (no return date)
CREATE INDEX idx_loans_active ON Loans(ReturnDate) WHERE ReturnDate IS NULL;

-- Index for overdue loan queries (REQ-4, REQ-6)
CREATE INDEX idx_loans_overdue ON Loans(DueDate, ReturnDate);


-- ============================================================================
-- RESERVATIONS TABLE
-- ============================================================================
-- Tracks active reservations on books currently on loan
-- UNIQUE constraint on BookID ensures single reservation per book (REQ-3, REQ-4)
-- Active reservations have FulfilledDate as NULL
-- When item is returned, Status becomes 'Reserved' and member is notified
-- ============================================================================
CREATE TABLE Reservations (
	ReservationID   INTEGER PRIMARY KEY AUTOINCREMENT,
	BookID          INTEGER NOT NULL,
	MemberID        INTEGER NOT NULL,
	ReservationDate TEXT NOT NULL,
	FulfilledDate   TEXT,
	NotifiedDate    TEXT,
	FOREIGN KEY (BookID) REFERENCES Books(BookID) ON DELETE RESTRICT,
	FOREIGN KEY (MemberID) REFERENCES Accounts(AccountID) ON DELETE RESTRICT
);

-- CRITICAL: Enforce single active reservation per book (REQ-3)
-- This prevents double-booking and maintains data integrity (REQ-14)
CREATE UNIQUE INDEX idx_reservations_active_book 
	ON Reservations(BookID) 
	WHERE FulfilledDate IS NULL;

-- Index for finding reservations by member (REQ-5)
CREATE INDEX idx_reservations_member ON Reservations(MemberID);

-- Index for active reservations queries
CREATE INDEX idx_reservations_active ON Reservations(FulfilledDate) 
	WHERE FulfilledDate IS NULL;


-- ============================================================================
-- SCHEMA VALIDATION QUERIES
-- ============================================================================
-- Use these queries to verify the schema is correctly created:
--
-- View all tables:
--   SELECT name FROM sqlite_master WHERE type='table';
--
-- View table structure:
--   PRAGMA table_info(Accounts);
--   PRAGMA table_info(Books);
--   PRAGMA table_info(Loans);
--   PRAGMA table_info(Reservations);
--
-- View indexes:
--   SELECT name, tbl_name FROM sqlite_master WHERE type='index';
--
-- View foreign keys:
--   PRAGMA foreign_key_list(Loans);
--   PRAGMA foreign_key_list(Reservations);
-- ============================================================================
