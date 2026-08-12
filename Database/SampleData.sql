-- ============================================================================
-- Library Management System - Sample Data
-- ============================================================================

-- Member Accounts (5 members)
INSERT INTO Accounts (Username, PasswordHash, Role, FirstName, LastName, Email, PhoneNumber, CreatedDate) VALUES
('alice.member', '5e884898da28047151d0e56f8dc6292773603d0d6aabbdd62a11ef721d1542d8', 'Member', 'Alice', 'Johnson', 'alice.johnson@email.com', '555-0101', '2024-06-17 10:00:00'),
('bob.member', '5e884898da28047151d0e56f8dc6292773603d0d6aabbdd62a11ef721d1542d8', 'Member', 'Bob', 'Smith', 'bob.smith@email.com', '555-0102', '2024-09-15 11:00:00'),
('carol.member', '5e884898da28047151d0e56f8dc6292773603d0d6aabbdd62a11ef721d1542d8', 'Member', 'Carol', 'Williams', 'carol.williams@email.com', '555-0103', '2024-10-30 12:00:00'),
('david.member', '5e884898da28047151d0e56f8dc6292773603d0d6aabbdd62a11ef721d1542d8', 'Member', 'David', 'Brown', 'david.brown@email.com', '555-0104', '2024-11-14 13:00:00'),
('emma.member', '5e884898da28047151d0e56f8dc6292773603d0d6aabbdd62a11ef721d1542d8', 'Member', 'Emma', 'Davis', 'emma.davis@email.com', '555-0105', '2024-11-29 14:00:00');

-- Staff Accounts (2 staff)
INSERT INTO Accounts (Username, PasswordHash, Role, FirstName, LastName, Email, PhoneNumber, CreatedDate) VALUES
('jane.staff', '0b14d501a594442a01c6859541bcb3e8164d183d32937b851835442f69d5c94e', 'Staff', 'Jane', 'Anderson', 'jane.anderson@library.org', '555-0201', '2023-12-14 10:00:00'),
('mike.staff', '0b14d501a594442a01c6859541bcb3e8164d183d32937b851835442f69d5c94e', 'Staff', 'Mike', 'Thompson', 'mike.thompson@library.org', '555-0202', '2024-05-28 11:00:00');

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
INSERT INTO Books (ISBN, Title, Author, Publisher, PublicationYear, Genre, Status, Description) VALUES
('978-0-14-303943-3', 'The Girl with the Dragon Tattoo', 'Stieg Larsson', 'Vintage Crime', 2005, 'Mystery', 'On Loan', 'A psychological thriller.'),
('978-0-06-207348-3', 'Gone Girl', 'Gillian Flynn', 'Broadway Books', 2012, 'Mystery', 'Reserved', 'A psychological thriller about a woman who disappears.'),
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
INSERT INTO Books (ISBN, Title, Author, Publisher, PublicationYear, Genre, Status, Description) VALUES
('978-1-4516-2639-8', 'Atomic Habits', 'James Clear', 'Avery', 2018, 'Self-Help', 'Available', 'A practical guide to building good habits.'),
('978-0-06-238329-6', 'Becoming', 'Michelle Obama', 'Crown', 2018, 'Biography', 'On Loan', 'Memoir of former First Lady.'),
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

-- Loans for Alice (Member ID = 1) - Has 1 active loan
INSERT INTO Loans (BookID, MemberID, LoanDate, DueDate, ReturnDate, ReturnCondition) VALUES
(1, 1, '2024-10-15', '2024-10-29', '2024-10-25', 'Good'),
(3, 1, '2024-10-30', '2024-11-13', '2024-11-12', 'Good'),
(7, 1, '2024-12-04', '2024-12-18', NULL, NULL);

-- Loans for Bob (Member ID = 2) - Has 2 active loans (at limit), one OVERDUE
INSERT INTO Loans (BookID, MemberID, LoanDate, DueDate, ReturnDate, ReturnCondition) VALUES
(2, 2, '2024-09-15', '2024-09-29', '2024-09-27', 'Good'),
(6, 2, '2024-10-25', '2024-11-08', '2024-11-07', 'Good'),
(8, 2, '2024-11-27', '2024-12-11', NULL, NULL),
(10, 2, '2024-12-05', '2024-12-19', NULL, NULL);

-- Loans for Carol (Member ID = 3) - Has 1 active loan
INSERT INTO Loans (BookID, MemberID, LoanDate, DueDate, ReturnDate, ReturnCondition) VALUES
(17, 3, '2024-12-11', '2024-12-25', NULL, NULL);

-- Loans for David (Member ID = 4) - Has 1 OVERDUE loan
INSERT INTO Loans (BookID, MemberID, LoanDate, DueDate, ReturnDate, ReturnCondition) VALUES
(15, 4, '2024-11-19', '2024-12-03', '2024-12-04', 'Good'),
(19, 4, '2024-11-22', '2024-12-06', NULL, NULL);

-- Loans for Emma (Member ID = 5) - New member, 1 active loan
INSERT INTO Loans (BookID, MemberID, LoanDate, DueDate, ReturnDate, ReturnCondition) VALUES
(14, 5, '2024-12-12', '2024-12-26', NULL, NULL);

-- Additional historical loan
INSERT INTO Loans (BookID, MemberID, LoanDate, DueDate, ReturnDate, ReturnCondition) VALUES
(24, 1, '2024-10-01', '2024-10-15', '2024-10-16', 'Good');

-- Active Reservations
INSERT INTO Reservations (BookID, MemberID, ReservationDate, FulfilledDate, NotifiedDate) VALUES
(7, 3, '2024-12-09', NULL, NULL),
(17, 4, '2024-12-12', NULL, NULL),
(3, 1, '2024-10-25', '2024-10-30', '2024-10-30');
