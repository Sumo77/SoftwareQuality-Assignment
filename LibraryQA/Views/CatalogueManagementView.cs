using LibraryQA.Core.Database;
using LibraryQA.Core.Services;
using Microsoft.Data.Sqlite;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace LibraryQA.Views
{
    // Catalogue Management, allowing staff to add, edit and remove catalogue records (REQ-1)
    public partial class CatalogueManagementView : UserControl
    {
        // StaffView listens to this so its book picker and totals stay in step with the catalogue.
        public event EventHandler? CatalogueChanged;

        private int? _selectedBookId;

        public CatalogueManagementView() // Initialise Catalogue Management View
        {
            InitializeComponent();
        }

        private void CatalogueManagementView_Loaded(object sender, RoutedEventArgs e) // Load the catalogue when the tab is first shown
        {
            LoadBooks();
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e) // Search the catalogue by title, author or ISBN
        {
            UserMessage.Clear(StatusText);
            LoadBooks();
        }

        private void ShowAllButton_Click(object sender, RoutedEventArgs e) // Clear the search and show the whole catalogue
        {
            SearchBox.Text = string.Empty;
            UserMessage.Clear(StatusText);
            LoadBooks();
        }

        private void BooksListView_SelectionChanged(object sender, SelectionChangedEventArgs e) // Selecting a book loads it into the form for editing
        {
            if (BooksListView.SelectedItem is CatalogueRow row)
            {
                FillForm(row);
                SetEditMode(row);
            }
            else
            {
                SetEditMode(null);
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e) // Add the book described by the form
        {
            UserMessage.Clear(StatusText);

            var result = Execute(service => service.AddBook(ReadForm()));
            if (result == null)
            {
                return;
            }

            if (result.Success)
            {
                ClearForm();
                LoadBooks();
                UserMessage.Show(StatusText, MessageKind.Success, result.Message);
                CatalogueChanged?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                ShowFailure(result);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e) // Save the form's details onto the selected book
        {
            UserMessage.Clear(StatusText);

            if (_selectedBookId is not int bookId)
            {
                UserMessage.Show(StatusText, MessageKind.Warning, UserMessage.NothingSelected("book"));
                return;
            }

            var result = Execute(service => service.UpdateBook(bookId, ReadForm()));
            if (result == null)
            {
                return;
            }

            if (result.Success)
            {
                ClearForm();
                LoadBooks();
                UserMessage.Show(StatusText, MessageKind.Success, result.Message);
                CatalogueChanged?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                ShowFailure(result);
            }
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e) // Remove the selected book, after confirmation
        {
            UserMessage.Clear(StatusText);

            if (BooksListView.SelectedItem is not CatalogueRow selected)
            {
                UserMessage.Show(StatusText, MessageKind.Warning, UserMessage.NothingSelected("book"));
                return;
            }

            // A confirmation, not error feedback - removal cannot be undone, so it is worth the extra click.
            var confirmation = MessageBox.Show(
                $"Remove '{selected.Title}' from the catalogue? This cannot be undone.",
                "Confirm removal", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (confirmation != MessageBoxResult.Yes)
            {
                return;
            }

            var result = Execute(service => service.RemoveBook(selected.BookId));
            if (result == null)
            {
                return;
            }

            if (result.Success)
            {
                ClearForm();
                LoadBooks();
                UserMessage.Show(StatusText, MessageKind.Success, result.Message);
                CatalogueChanged?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                ShowFailure(result);
            }
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e) // Empty the form and leave edit mode
        {
            UserMessage.Clear(StatusText);
            ClearForm();
        }

        private void LoadBooks() // Load the catalogue (filtered by the search box) into the list
        {
            try
            {
                using var db = new DatabaseHelper(App.ConnectionString);

                BooksListView.ItemsSource = db.SearchCatalogue(SearchBox.Text.Trim())
                    .Select(row => new CatalogueRow
                    {
                        BookId = Convert.ToInt32(row["BookID"]),
                        Title = row["Title"]?.ToString() ?? "",
                        Author = row["Author"]?.ToString() ?? "",
                        Isbn = row["ISBN"]?.ToString() ?? "",
                        Year = row["PublicationYear"]?.ToString() ?? "",
                        Publisher = row["Publisher"]?.ToString() ?? "",
                        Genre = row["Genre"]?.ToString() ?? "",
                        Description = row["Description"]?.ToString() ?? "",
                        Status = row["Status"]?.ToString() ?? ""
                    })
                    .ToList();
            }
            catch (SqliteException)
            {
                UserMessage.Show(StatusText, MessageKind.Error, UserMessage.DatabaseUnavailable);
            }
        }

        // Runs a service call, turning a database failure into the shared message instead of a crash (DEF-11).
        private CatalogueResult? Execute(Func<CatalogueService, CatalogueResult> action)
        {
            try
            {
                return action(new CatalogueService(App.ConnectionString));
            }
            catch (SqliteException)
            {
                UserMessage.Show(StatusText, MessageKind.Error, UserMessage.DatabaseUnavailable);
                return null;
            }
        }

        private void ShowFailure(CatalogueResult result) // Show why the action was refused and move the cursor to the field at fault
        {
            UserMessage.Show(StatusText, MessageKind.Warning, result.Message);
            FocusField(result.Field);
        }

        private void FocusField(string? field) // Puts the cursor in the field named by the validation result
        {
            TextBox? target = field switch
            {
                CatalogueService.FieldTitle => TitleBox,
                CatalogueService.FieldAuthor => AuthorBox,
                CatalogueService.FieldIsbn => IsbnBox,
                CatalogueService.FieldPublicationYear => YearBox,
                CatalogueService.FieldPublisher => PublisherBox,
                CatalogueService.FieldGenre => GenreBox,
                CatalogueService.FieldDescription => DescriptionBox,
                _ => null
            };

            target?.Focus();
        }

        private BookInput ReadForm() // Collect the form's raw text; the service trims, parses and validates it
        {
            return new BookInput
            {
                Title = TitleBox.Text,
                Author = AuthorBox.Text,
                Isbn = IsbnBox.Text,
                Publisher = PublisherBox.Text,
                PublicationYear = YearBox.Text,
                Genre = GenreBox.Text,
                Description = DescriptionBox.Text
            };
        }

        private void FillForm(CatalogueRow row) // Show the selected book's details in the form
        {
            TitleBox.Text = row.Title;
            AuthorBox.Text = row.Author;
            IsbnBox.Text = row.Isbn;
            YearBox.Text = row.Year;
            PublisherBox.Text = row.Publisher;
            GenreBox.Text = row.Genre;
            DescriptionBox.Text = row.Description;
        }

        private void ClearForm() // Empty every field and deselect any book
        {
            TitleBox.Text = string.Empty;
            AuthorBox.Text = string.Empty;
            IsbnBox.Text = string.Empty;
            YearBox.Text = string.Empty;
            PublisherBox.Text = string.Empty;
            GenreBox.Text = string.Empty;
            DescriptionBox.Text = string.Empty;
            BooksListView.SelectedItem = null;
            SetEditMode(null);
        }

        private void SetEditMode(CatalogueRow? row) // Save and Remove only make sense while a book is selected
        {
            _selectedBookId = row?.BookId;
            SaveButton.IsEnabled = row != null;
            RemoveButton.IsEnabled = row != null;
            FormHeading.Text = row == null ? "Add a new book" : $"Editing: {row.Title} (ID {row.BookId})";
        }

        private class CatalogueRow // Represents a catalogue record for display and editing in the staff interface
        {
            public int BookId { get; set; }
            public string Title { get; set; } = "";
            public string Author { get; set; } = "";
            public string Isbn { get; set; } = "";
            public string Year { get; set; } = "";
            public string Publisher { get; set; } = "";
            public string Genre { get; set; } = "";
            public string Description { get; set; } = "";
            public string Status { get; set; } = "";
        }
    }
}