using System;
using System.IO;
using System.Text;
using System.Windows;
using LibraryQA.Core.Database;
using System.Security.Cryptography;

namespace LibraryQA
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Full path to the SQLite database file, in the application's output folder.
        /// </summary>
        public static string DatabasePath { get; } = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "library.db");

        /// <summary>
        /// Connection string for the application database.
        /// Pass this to DatabaseHelper wherever data access is needed.
        /// </summary>
        public static string ConnectionString => $"Data Source={DatabasePath}";

        protected override void OnStartup(StartupEventArgs e)
        {
            // Create and seed the database before the main window opens.
            // Runs once - on later launches the existing library.db is reused.
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

                var seeder = new DatabaseSeeder(DatabasePath);

                if (!seeder.SeedSampleData())
                {
                    MessageBox.Show(
                        "The database was created but the sample data could not be loaded. " +
                        "The catalogue will be empty.",
                        "Startup warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }

            base.OnStartup(e);
        }

    }
}