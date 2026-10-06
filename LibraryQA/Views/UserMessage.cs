using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace LibraryQA.Views
{
    // Severity of a message shown to the user.
    public enum MessageKind
    {
        Success,
        Info,
        Warning,
        Error
    }

    // Single place where user-facing messages are formatted and displayed (DEF-04, REQ-10, REQ-12).
    // Every view calls this instead of building its own strings or opening a MessageBox, so the
    // same kind of event looks and reads the same way on every screen (REQ-13).
    public static class UserMessage
    {
        // Severity is carried by a symbol as well as by colour, so meaning never relies on colour alone.
        private const string SuccessSymbol = "\u2713"; // check mark
        private const string InfoSymbol = "\u2139";    // information
        private const string WarningSymbol = "\u26A0"; // warning triangle
        private const string ErrorSymbol = "\u2716";   // cross

        private static readonly Brush SuccessBrush = FrozenBrush(Color.FromRgb(0x1B, 0x5E, 0x20));
        private static readonly Brush InfoBrush = FrozenBrush(Color.FromRgb(0x0D, 0x47, 0xA1));
        private static readonly Brush WarningBrush = FrozenBrush(Color.FromRgb(0x8A, 0x50, 0x00));
        private static readonly Brush ErrorBrush = FrozenBrush(Color.FromRgb(0xB7, 0x1C, 0x1C));

        // Shown whenever the database cannot be reached (DEF-11, REQ-12).
        public const string DatabaseUnavailable =
            "Could not reach the library database. Please try again.";

        // Deliberately does not say which of the two was wrong, so the login screen cannot be
        // used to confirm whether a username exists (REQ-11).
        public const string InvalidCredentials = "Invalid username or password.";

        // Shown only after correct credentials, so it never reveals that an account exists (REQ-19).
        public const string AccountSuspended =
            "This account has been suspended. Please contact the Library Staff.";

        // Shown only after correct credentials, for the same reason (REQ-16: a new account is not
        // usable until library staff activate it).
        public const string AccountPending =
            "This account is awaiting activation by library staff.";

        // Displays a message in the supplied status TextBlock.
        public static void Show(TextBlock? target, MessageKind kind, string message)
        {
            if (target == null)
            {
                return;
            }

            target.Text = $"{SymbolFor(kind)} {message}";
            target.Foreground = BrushFor(kind);
            target.Visibility = Visibility.Visible;
        }

        // Clears a message, so a stale one from a previous action is never left on screen.
        public static void Clear(TextBlock? target)
        {
            if (target == null)
            {
                return;
            }

            target.Text = string.Empty;
            target.Visibility = Visibility.Collapsed;
        }

        // Standard wordings (DEF-12, REQ-9).
        // Validation messages name the field at fault rather than describing the whole form,
        // and live here so every view words the same situation identically.

        public static string FieldMustBeANumber(string fieldName)
        {
            return $"{fieldName} must be a number.";
        }

        public static string FieldIsRequired(string fieldName)
        {
            return $"Please enter a {fieldName}.";
        }

        public static string NothingSelected(string itemDescription)
        {
            return $"Please select a {itemDescription} first.";
        }

        private static Brush FrozenBrush(Color colour)
        {
            var brush = new SolidColorBrush(colour);
            brush.Freeze();
            return brush;
        }

        private static string SymbolFor(MessageKind kind)
        {
            return kind switch
            {
                MessageKind.Success => SuccessSymbol,
                MessageKind.Info => InfoSymbol,
                MessageKind.Warning => WarningSymbol,
                _ => ErrorSymbol
            };
        }

        private static Brush BrushFor(MessageKind kind)
        {
            return kind switch
            {
                MessageKind.Success => SuccessBrush,
                MessageKind.Info => InfoBrush,
                MessageKind.Warning => WarningBrush,
                _ => ErrorBrush
            };
        }
    }
}
