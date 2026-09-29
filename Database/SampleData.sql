-- ============================================================================
-- Library Management System - Sample Data
-- ============================================================================
-- DEF-05 fixes applied:
--   * 'Gone Girl' (BookID 11) was marked 'Reserved' with no reservation record.
--   * 'Becoming'  (BookID 24) was marked 'On Loan' with no active loan record.
--     Both are now 'Available', which matches their loan and reservation rows (REQ-14).
--
--   * Loan dates were hardcoded to 2024, so every active loan read as years overdue and
--     the stated intent of this file ("one overdue", "one active") no longer held. Dates
--     are now relative to the current local date, so the data always means what it says.
--     'localtime' is used deliberately: date('now') alone is UTC (see DEF-09).
--
-- Active loans after seeding (6 total, matching the 6 books marked 'On Loan'):
--   LoanID 3  - Alice, BookID 7   due in 11 days   - not overdue
--   LoanID 6  - Bob,   BookID 8   due 6 days ago   - overdue by 6
--   LoanID 7  - Bob,   BookID 10  due in 9 days    - not overdue (Bob is at the 2-loan limit)
--   LoanID 8  - Carol, BookID 17  due today        - BOUNDARY: not overdue
--   LoanID 10 - David, BookID 19  due 4 days ago   - overdue by 4
--   LoanID 11 - Emma,  BookID 14  due yesterday    - BOUNDARY: overdue by exactly 1
--
-- Loan insert order is unchanged, so LoanID 3 is still Alice's active loan on BookID 7,
-- which TC-6 depends on.
-- ============================================================================

-- Member Accounts (5 members)
INSERT INTO Accounts (Username, PasswordHash, Role, FirstName, LastName, Email, PhoneNumber, CreatedDate) VALUES
('alice.member', '5600376e863d2f57a053518f324ad3840b0bc2348b573af281a7b7cbe7a228c6', 'Member', 'Alice', 'Johnson', 'alice.johnson@email.com', '555-0101', '2024-06-17 10:00:00'),
('bob.member', '5600376e863d2f57a053518f324ad3840b0bc2348b573af281a7b7cbe7a228c6', 'Member', 'Bob', 'Smith', 'bob.smith@email.com', '555-0102', '2024-09-15 11:00:00'),
('carol.member', '5600376e863d2f57a053518f324ad3840b0bc2348b573af281a7b7cbe7a228c6', 'Member', 'Carol', 'Williams', 'carol.williams@email.com', '555-0103', '2024-10-30 12:00:00'),
('david.member', '5600376e863d2f57a053518f324ad3840b0bc2348b573af281a7b7cbe7a228c6', 'Member', 'David', 'Brown', 'david.brown@email.com', '555-0104', '2024-11-14 13:00:00'),
('emma.member', '5600376e863d2f57a053518f324ad3840b0bc2348b573af281a7b7cbe7a228c6', 'Member', 'Emma', 'Davis', 'emma.davis@email.com', '555-0105', '2024-11-29 14:00:00');

-- Staff Accounts (2 staff)
INSERT INTO Accounts (Username, PasswordHash, Role, FirstName, LastName, Email, PhoneNumber, CreatedDate) VALUES
('jane.staff', '8f5dada329d6ade1fdba5e207b5a81b312ae838801ca287a00e9428620808dce', 'Staff', 'Jane', 'Anderson', 'jane.anderson@library.org', '555-0201', '2023-12-14 10:00:00'),
('mike.staff', '8f5dada329d6ade1fdba5e207b5a81b312ae838801ca287a00e9428620808dce', 'Staff', 'Mike', 'Thompson', 'mike.thompson@library.org', '555-0202', '2024-05-28 11:00:00');

-- Books - Fiction (Available)
INSERT INTO Books (ISBN, Title, Author, Publisher, PublicationYear, Genre, Status, Description) VALUES
('978-0-06-112008-4', 'To Kill a Mockingbird', 'Harper Lee', 'Harper Perennial', 1960, 'Fiction', 'Available', 'A classic novel set in the Deep South dealing with racial injustice.'),
('978-0-14-118776-1', '1984', 'George Orwell', 'Penguin Books', 1949, 'Fiction', 'Available', 'A dystopian social science fiction novel and cautionary tale.'),
('978-0-7432-7356-5', 'The Great Gatsby', 'F. Scott Fitzgerald', 'Scribner', 1925, 'Fiction', 'Available', 'A novel about the American Dream and the Jazz Age.'),
('978-0-452-28423-4', 'Pride and Prejudice', 'Jane Austen', 'Penguin Classics', 1813, 'Fiction', 'Available', 'A romantic novel of manners set in Georgian England.'),
('978-0-06-093546-7', 'Brave New World', 'Aldous Huxley', 'Harper Perennial', 1932, 'Fiction', 'Available', 'A dystopian novel about a futuristic World State.');

-- Books - Science Fiction
INSERT INTO Books (ISBN, Title, Author, Publisher, PublicationYear, Genre, Status, Description) VALUES
('978-0-441-00590-0', 'Dune', 'Frank Herbert', 'Ace Books', 1965, 'Science Fiction', 'Available', 'A science fiction novel set in the far future.'),
('978-0-553-38256-4', 'The Martian', 'Andy Weir', 'Broadway Books', 2014, 'Science Fiction', 'On Loan', 'A science fiction novel about an astronaut stranded on Mars.'),
('978-0-345-39180-3', 'Enders Game', 'Orson Scott Card', 'Tor Books', 1985, 'Science Fiction', 'On Loan', 'A military science fiction novel.'),
('978-0-553-29335-0', 'Foundation', 'Isaac Asimov', 'Bantam Spectra', 1951, 'Science Fiction', 'Available', 'First novel in the Foundation series.');

-- Books - Mystery
-- DEF-05: 'Gone Girl' was 'Reserved' with no matching reservation row - corrected to 'Available'.
INSERT INTO Books (ISBN, Title, Author, Publisher, PublicationYear, Genre, Status, Description) VALUES
('978-0-14-303943-3', 'The Girl with the Dragon Tattoo', 'Stieg Larsson', 'Vintage Crime', 2005, 'Mystery', 'On Loan', 'A psychological thriller.'),
('978-0-06-207348-3', 'Gone Girl', 'Gillian Flynn', 'Broadway Books', 2012, 'Mystery', 'Available', 'A psychological thriller about a woman who disappears.'),
('978-0-316-01792-2', 'The Cuckoos Calling', 'Robert Galbraith', 'Mulholland Books', 2013, 'Mystery', 'Available', 'A crime fiction novel.');

-- Books - Biography & History
INSERT INTO Books (ISBN, Title, Author, Publisher, PublicationYear, Genre, Status, Description) VALUES
('978-1-5011-2738-3', 'Educated', 'Tara Westover', 'Random House', 2018, 'Biography', 'Available', 'A memoir about growing up in a survivalist family.'),
('978-0-7432-7357-2', 'Steve Jobs', 'Walter Isaacson', 'Simon & Schuster', 2011, 'Biography', 'On Loan', 'The authorized biography of Apple co-founder.'),
('978-0-385-50420-3', 'Sapiens', 'Yuval Noah Harari', 'Harper', 2015, 'History', 'Available', 'A brief history of humankind.'),
('978-0-307-58837-1', 'Guns Germs and Steel', 'Jared Diamond', 'W. W. Norton', 1997, 'History', 'Available', 'An analysis of human societies.');

-- Books - Fantasy
INSERT INTO Books (ISBN, Title, Author, Publisher, PublicationYear, Genre, Status, Description) VALUES
('978-0-439-13959-5', 'Harry Potter and the Goblet of Fire', 'J.K. Rowling', 'Scholastic', 2000, 'Fantasy', 'On Loan', 'Fourth book in the Harry Potter series.'),
('978-0-345-33973-1', 'The Hobbit', 'J.R.R. Tolkien', 'Del Rey', 1937, 'Fantasy', 'Available', 'A fantasy novel about a quest.'),
('978-0-553-57340-1', 'A Game of Thrones', 'George R.R. Martin', 'Bantam', 1996, 'Fantasy', 'On Loan', 'First book in A Song of Ice and Fire.'),
('978-0-7653-4268-7', 'The Name of the Wind', 'Patrick Rothfuss', 'DAW Books', 2007, 'Fantasy', 'Available', 'First book in The Kingkiller Chronicle.');

-- Books - Romance
INSERT INTO Books (ISBN, Title, Author, Publisher, PublicationYear, Genre, Status, Description) VALUES
('978-0-14-017954-9', 'Jane Eyre', 'Charlotte Bronte', 'Penguin Classics', 1847, 'Romance', 'Available', 'A classic novel about an orphaned governess.'),
('978-0-14-243726-6', 'Wuthering Heights', 'Emily Bronte', 'Penguin Classics', 1847, 'Romance', 'Available', 'A tale of passion and revenge.');

-- Books - Self-Help & Business
-- DEF-05: 'Becoming' was 'On Loan' but its only loan was returned - corrected to 'Available'.
INSERT INTO Books (ISBN, Title, Author, Publisher, PublicationYear, Genre, Status, Description) VALUES
('978-1-4516-2639-8', 'Atomic Habits', 'James Clear', 'Avery', 2018, 'Self-Help', 'Available', 'A practical guide to building good habits.'),
('978-0-06-238329-6', 'Becoming', 'Michelle Obama', 'Crown', 2018, 'Biography', 'Available', 'Memoir of former First Lady.'),
('978-1-59184-278-5', 'The Lean Startup', 'Eric Ries', 'Crown Business', 2011, 'Business', 'Available', 'A guide to building successful startups.');

-- Books - Children & Young Adult
INSERT INTO Books (ISBN, Title, Author, Publisher, PublicationYear, Genre, Status, Description) VALUES
('978-0-06-440055-8', 'Where the Wild Things Are', 'Maurice Sendak', 'Harper Collins', 1963, 'Children', 'Available', 'A beloved childrens picture book.'),
('978-0-14-240733-0', 'The Fault in Our Stars', 'John Green', 'Penguin', 2012, 'Young Adult', 'Available', 'A coming-of-age story.');

-- Books - Poetry & Drama
INSERT INTO Books (ISBN, Title, Author, Publisher, PublicationYear, Genre, Status, Description) VALUES
('978-0-486-27077-9', 'Romeo and Juliet', 'William Shakespeare', 'Dover Publications', 1597, 'Drama', 'Available', 'The tragedy of two star-crossed lovers.'),
('978-0-14-042470-3', 'The Odyssey', 'Homer', 'Penguin Classics', -800, 'Poetry', 'Available', 'An ancient Greek epic poem.');

-- Books - Technology
INSERT INTO Books (ISBN, Title, Author, Publisher, PublicationYear, Genre, Status, Description) VALUES
('978-0-13-468599-1', 'Clean Code', 'Robert C. Martin', 'Prentice Hall', 2008, 'Technology', 'Available', 'A handbook of agile software craftsmanship.'),
('978-0-262-03384-8', 'Introduction to Algorithms', 'Thomas H. Cormen', 'MIT Press', 2009, 'Computer Science', 'Available', 'Comprehensive text on algorithms.');

-- Loans for Alice (MemberID 1) - 1 active loan, not overdue
INSERT INTO Loans (BookID, MemberID, LoanDate, DueDate, ReturnDate, ReturnCondition) VALUES
(1, 1, date('now', 'localtime', '-70 days'), date('now', 'localtime', '-56 days'), date('now', 'localtime', '-60 days'), 'Good'),
(3, 1, date('now', 'localtime', '-55 days'), date('now', 'localtime', '-41 days'), date('now', 'localtime', '-42 days'), 'Good'),
(7, 1, date('now', 'localtime', '-3 days'), date('now', 'localtime', '+11 days'), NULL, NULL);

-- Loans for Bob (MemberID 2) - 2 active loans (at the limit), one overdue by 6 days
INSERT INTO Loans (BookID, MemberID, LoanDate, DueDate, ReturnDate, ReturnCondition) VALUES
(2, 2, date('now', 'localtime', '-90 days'), date('now', 'localtime', '-76 days'), date('now', 'localtime', '-78 days'), 'Good'),
(6, 2, date('now', 'localtime', '-50 days'), date('now', 'localtime', '-36 days'), date('now', 'localtime', '-37 days'), 'Good'),
(8, 2, date('now', 'localtime', '-20 days'), date('now', 'localtime', '-6 days'), NULL, NULL),
(10, 2, date('now', 'localtime', '-5 days'), date('now', 'localtime', '+9 days'), NULL, NULL);

-- Loans for Carol (MemberID 3) - 1 active loan due TODAY (boundary case: must not be overdue)
INSERT INTO Loans (BookID, MemberID, LoanDate, DueDate, ReturnDate, ReturnCondition) VALUES
(17, 3, date('now', 'localtime', '-14 days'), date('now', 'localtime'), NULL, NULL);

-- Loans for David (MemberID 4) - 1 active loan overdue by 4 days, plus one returned late
INSERT INTO Loans (BookID, MemberID, LoanDate, DueDate, ReturnDate, ReturnCondition) VALUES
(15, 4, date('now', 'localtime', '-40 days'), date('now', 'localtime', '-26 days'), date('now', 'localtime', '-25 days'), 'Good'),
(19, 4, date('now', 'localtime', '-18 days'), date('now', 'localtime', '-4 days'), NULL, NULL);

-- Loans for Emma (MemberID 5) - 1 active loan due YESTERDAY (boundary case: overdue by exactly 1)
INSERT INTO Loans (BookID, MemberID, LoanDate, DueDate, ReturnDate, ReturnCondition) VALUES
(14, 5, date('now', 'localtime', '-15 days'), date('now', 'localtime', '-1 days'), NULL, NULL);

-- Additional historical loan (returned)
INSERT INTO Loans (BookID, MemberID, LoanDate, DueDate, ReturnDate, ReturnCondition) VALUES
(24, 1, date('now', 'localtime', '-120 days'), date('now', 'localtime', '-106 days'), date('now', 'localtime', '-105 days'), 'Good');

-- Reservations
-- Two active reservations, both on books currently 'On Loan' (REQ-3), plus one already fulfilled.
INSERT INTO Reservations (BookID, MemberID, ReservationDate, FulfilledDate, NotifiedDate) VALUES
(7, 3, date('now', 'localtime', '-2 days'), NULL, NULL),
(17, 4, date('now', 'localtime', '-1 days'), NULL, NULL),
(3, 1, date('now', 'localtime', '-48 days'), date('now', 'localtime', '-42 days'), date('now', 'localtime', '-42 days'));
