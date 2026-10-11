using System;
using System.IO;
using System.Text;
using System.Windows;
using LibraryQA.Core.Database;
using System.Security.Cryptography;

namespace LibraryQA
{
    /// Interaction logic for App.xaml
    public partial class App : Application
    {

        /// Full path to the SQLite database file, in the application's output folder.
        public static string DatabasePath { get; } = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "library.db");

        /// Connection string for the application database.
        /// Pass this to DatabaseHelper wherever data access is needed.
        public static string ConnectionString => $"Data Source={DatabasePath}";

        protected override void OnStartup(StartupEventArgs e)
        {
            // Create and seed the database before the main window opens.
            var initializer = new DatabaseInitializer(DatabasePath);

            if (!initializer.DatabaseExists())
            {
                if (!initializer.InitializeDatabase())
                {
                    MessageBox.Show(
                        "The database could not be created. The application will close.",
                        "Startup error", MessageBoxButton.OK, MessageBoxImage.Error);
                    Shutdown();
                    return;
                }
            }

            // DEF-15: seeding is checked separately from schema creation.
            // Previously, if the schema was created but seeding failed, the database file existed
            // on the next launch, DatabaseExists() returned true, and the app never tried to seed
            // again - leaving an empty catalogue permanently. Now the seed runs whenever the
            // database holds no accounts, so a failed seed is retried on the next launch.
            var seeder = new DatabaseSeeder(DatabasePath);

            if (!seeder.HasSampleData() && !seeder.SeedSampleData())
            {
                MessageBox.Show(
                    "The sample data could not be loaded, so the catalogue will be empty. " +
                    "Closing and reopening the application will try again.",
                    "Startup warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            base.OnStartup(e);
        }

    }
}