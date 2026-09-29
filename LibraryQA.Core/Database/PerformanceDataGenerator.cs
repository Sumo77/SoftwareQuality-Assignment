using Microsoft.Data.Sqlite;
using System;
using System.Globalization;

namespace LibraryQA.Core.Database
{
    // Generates a large catalogue so search performance can be measured against REQ-15.
    //
    // Used by the performance tests only; the application itself never calls this. Generated
    // rows all carry the "GEN-" ISBN prefix so they can be told apart from the seeded sample
    // catalogue and removed again afterwards.
    //
    // Generation is deterministic (fixed random seed), so two timing runs measure the same data.
    public class PerformanceDataGenerator
    {
        public const string GeneratedIsbnPrefix = "GEN-";

        private const int RandomSeed = 20250923;

        private static readonly string[] TitleOpeners =
        {
            "The", "A", "Beyond the", "Under the", "Return to the", "Notes on the", "After the"
        };

        private static readonly string[] TitleNouns =
        {
            "Harbour", "Lantern", "Compass", "Archive", "Meridian", "Orchard", "Foundry",
            "Cartographer", "Signal", "Tideline", "Observatory", "Almanac", "Quarry", "Aviary"
        };

        private static readonly string[] TitleTails =
        {
            "of Glass", "in Winter", "at Dusk", "and the Long Road", "of the Second City",
            "Revisited", "in Translation", "of Small Hours"
        };

        private static readonly string[] FirstNames =
        {
            "Mara", "Tobias", "Ines", "Rafael", "Noor", "Callum", "Yuki", "Priya",
            "Anders", "Lucia", "Omar", "Freya", "Dmitri", "Selina"
        };

        private static readonly string[] LastNames =
        {
            "Whitfield", "Okonkwo", "Bergstrom", "Delacroix", "Nakamura", "Ferreira",
            "Halloran", "Petrov", "Castellanos", "Adeyemi", "Lindqvist", "Marchetti"
        };

        private static readonly string[] Publishers =
        {
            "Northgate Press", "Halyard Books", "Verso Lane", "Kestrel House", "Sable & Co"
        };

        private static readonly string[] Genres =
        {
            "Fiction", "Science Fiction", "Mystery", "History", "Biography",
            "Fantasy", "Technology", "Business", "Poetry", "Young Adult"
        };

        private readonly string _connectionString;

        public PerformanceDataGenerator(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        // Inserts the requested number of generated books and returns how many rows were added.
        // All inserts run inside one transaction, so a failure part-way through adds nothing.
        public int GenerateBooks(int count)
        {
            if (count < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(count), "Count must be at least 1.");
            }

            var random = new Random(RandomSeed);
            int inserted = 0;

            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();

                using (var transaction = connection.BeginTransaction())
                using (var command = connection.CreateCommand())
                {
                    command.Transaction = transaction;
                    command.CommandText = @"
                        INSERT INTO Books (ISBN, Title, Author, Publisher, PublicationYear, Genre, Status, Description)
                        VALUES (@isbn, @title, @author, @publisher, @year, @genre, 'Available', @description)";

                    // Parameters are created once and reused, which is what keeps a 5,000-row
                    // insert fast enough to run inside a test.
                    var isbn = command.Parameters.Add("@isbn", SqliteType.Text);
                    var title = command.Parameters.Add("@title", SqliteType.Text);
                    var author = command.Parameters.Add("@author", SqliteType.Text);
                    var publisher = command.Parameters.Add("@publisher", SqliteType.Text);
                    var year = command.Parameters.Add("@year", SqliteType.Integer);
                    var genre = command.Parameters.Add("@genre", SqliteType.Text);
                    var description = command.Parameters.Add("@description", SqliteType.Text);

                    for (int i = 1; i <= count; i++)
                    {
                        string generatedTitle = BuildTitle(random);

                        isbn.Value = GeneratedIsbnPrefix + i.ToString("D7", CultureInfo.InvariantCulture);
                        title.Value = generatedTitle;
                        author.Value = Pick(FirstNames, random) + " " + Pick(LastNames, random);
                        publisher.Value = Pick(Publishers, random);
                        year.Value = random.Next(1950, 2025);
                        genre.Value = Pick(Genres, random);
                        description.Value = "Generated catalogue record for performance testing.";

                        inserted += command.ExecuteNonQuery();
                    }

                    transaction.Commit();
                }
            }

            return inserted;
        }

        // Removes every generated book, leaving the seeded sample catalogue untouched.
        // Generated books are never loaned or reserved, so no foreign key can block the delete.
        public int RemoveGeneratedBooks()
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "DELETE FROM Books WHERE ISBN LIKE @prefix";
                    command.Parameters.AddWithValue("@prefix", GeneratedIsbnPrefix + "%");

                    return command.ExecuteNonQuery();
                }
            }
        }

        private static string BuildTitle(Random random)
        {
            // Titles are built from a small word list so a search term such as "the" reliably
            // matches a large share of the catalogue, which is the case worth timing.
            return string.Join(
                " ",
                Pick(TitleOpeners, random),
                Pick(TitleNouns, random),
                Pick(TitleTails, random));
        }

        private static string Pick(string[] options, Random random)
        {
            return options[random.Next(options.Length)];
        }
    }
}
