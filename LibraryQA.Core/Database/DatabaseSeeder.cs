using Microsoft.Data.Sqlite;
using System;
using System.IO;

namespace LibraryQA.Core.Database
{
    
    /// Populates an initialised database with the sample data set.
    /// DatabaseInitializer creates the schema; this class fills it.
    public class DatabaseSeeder
    {
        private readonly string _connectionString;

        
        /// Initializes a new instance of DatabaseSeeder for the specified database file.
        public DatabaseSeeder(string databasePath)
        {
            if (databasePath == null)
            {
                throw new ArgumentNullException(nameof(databasePath));
            }

            _connectionString = $"Data Source={databasePath}";
        }

        
        /// Executes SampleData.sql against the database.
        /// Assumes the schema already exists and the tables are empty -
        /// running this twice will create duplicate rows.
        public bool SeedSampleData()
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

                    // Wrap in a transaction so a failure part-way through leaves
                    // no half-populated database behind.
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
