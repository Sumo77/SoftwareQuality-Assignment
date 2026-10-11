using Microsoft.Data.Sqlite;
using System;
using System.IO;

namespace LibraryQA.Core.Database
{

    // Database Seeder, populates an initialised database with the sample data set.
    // DatabaseInitializer creates the schema, this seed / populates it.
    public class DatabaseSeeder
    {
        private readonly string _connectionString;

        public DatabaseSeeder(string databasePath) // Initializes a new instance of DatabaseSeeder for the specified database file
        {
            if (databasePath == null)
            {
                throw new ArgumentNullException(nameof(databasePath));
            }

            _connectionString = $"Data Source={databasePath}";
        }

        // DEF-15: reports whether the database has already been seeded.
        // The caller uses this instead of "does the file exist", so a seed that failed part-way
        // through is retried on the next launch rather than leaving an empty catalogue forever.
        // Accounts is checked rather than Books because accounts are never deleted by the
        // application, so an empty Accounts table can only mean the seed never completed.
        public bool HasSampleData()
        {
            try
            {
                using (var connection = new SqliteConnection(_connectionString))
                {
                    connection.Open();

                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "SELECT COUNT(*) FROM Accounts;";
                        return Convert.ToInt32(command.ExecuteScalar()) > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                // An unreadable or schema-less database counts as not seeded.
                Console.WriteLine($"Error checking for sample data: {ex.Message}");
                return false;
            }
        }

        public bool SeedSampleData() // Seeds the database with sample data from the SampleData.sql file
        {
            try
            {
                string dataPath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory, "Database", "SampleData.sql");

                if (!File.Exists(dataPath))
                {
                    throw new FileNotFoundException($"Sample data file not found at: {dataPath}");
                }

                string sql = File.ReadAllText(dataPath);

                using (var connection = new SqliteConnection(_connectionString))
                {
                    connection.Open();

                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "PRAGMA foreign_keys = ON;";
                        command.ExecuteNonQuery();
                    }

                    // Wrap in a single transaction so a failure part-way through leaves no half-populated database behind.
                    using (var transaction = connection.BeginTransaction())
                    using (var command = connection.CreateCommand())
                    {
                        command.Transaction = transaction;
                        command.CommandText = sql;
                        command.ExecuteNonQuery();
                        transaction.Commit();
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error seeding database: {ex.Message}");
                return false;
            }
        }
    }
}
