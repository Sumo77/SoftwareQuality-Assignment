using System;
using LibraryQA.Core.Database;

namespace LibraryQA.Core.Services
{
    public class RegistrationResult // Stored result of a registration attempt
    {
        public bool Success { get; init; }
        public string Message { get; init; } = string.Empty;
        public int? AccountId { get; init; }
    }

    // REQ-16: Account Creation. A new member chooses a unique username and a password, which is
    // stored hashed, and the account is created Pending so library staff must activate it before
    // it can be used. All the validation lives here rather than in the view, so the same rules
    // apply however the account is created (REQ-9, REQ-13).
    public class RegistrationService
    {
        public const int MinimumUsernameLength = 3;
        public const int MaximumUsernameLength = 20;
        public const int MinimumPasswordLength = 8;

        private const string MemberRole = "Member";
        private const string PendingStatus = "Pending";

        private readonly string _connectionString;

        public RegistrationService(string connectionString)
        {
            _connectionString = connectionString
                ?? throw new ArgumentNullException(nameof(connectionString));
        }

        public RegistrationResult Register(string username, string password, string firstName,
            string lastName, string? email = null, string? phoneNumber = null)
        {
            username = (username ?? string.Empty).Trim();
            firstName = (firstName ?? string.Empty).Trim();
            lastName = (lastName ?? string.Empty).Trim();
            password ??= string.Empty;

            if (firstName.Length == 0)
            {
                return Rejected("Please enter a first name.");
            }

            if (lastName.Length == 0)
            {
                return Rejected("Please enter a last name.");
            }

            if (username.Length < MinimumUsernameLength || username.Length > MaximumUsernameLength)
            {
                return Rejected($"A username must be between {MinimumUsernameLength} and {MaximumUsernameLength} characters.");
            }

            if (!IsAllowedUsername(username))
            {
                return Rejected("A username may only contain letters, numbers, full stops and underscores.");
            }

            if (password.Length < MinimumPasswordLength)
            {
                return Rejected($"A password must be at least {MinimumPasswordLength} characters.");
            }

            if (!string.IsNullOrWhiteSpace(email) && !IsPlausibleEmail(email.Trim()))
            {
                return Rejected("Please enter a valid email address, or leave it blank.");
            }

            if (!string.IsNullOrWhiteSpace(phoneNumber) && !IsPlausiblePhoneNumber(phoneNumber.Trim()))
            {
                return Rejected("Please enter a valid phone number, or leave it blank.");
            }

            using (var db = new DatabaseHelper(_connectionString))
            {
                int? accountId = db.CreateAccount(
                    username,
                    AuthenticationService.HashPassword(password), // never stored in plain text (REQ-11)
                    MemberRole,
                    firstName,
                    lastName,
                    string.IsNullOrWhiteSpace(email) ? null : email.Trim(),
                    string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber.Trim(),
                    PendingStatus);

                if (accountId == null)
                {
                    return Rejected("That username is already taken. Please choose another.");
                }

                return new RegistrationResult
                {
                    Success = true,
                    AccountId = accountId,
                    Message = "Account created. Library staff will activate it before you can log in."
                };
            }
        }

        // REQ-16: answers "has this username been used before?" for the registration screen, so the
        // member is told while they are still on the username field rather than after submitting.
        public bool IsUsernameAvailable(string username)
        {
            username = (username ?? string.Empty).Trim();

            if (username.Length == 0)
            {
                return true; // nothing typed yet - the required-field check covers that
            }

            using (var db = new DatabaseHelper(_connectionString))
            {
                return !db.UsernameExists(username);
            }
        }

        private static bool IsAllowedUsername(string username)
        {
            foreach (char character in username)
            {
                if (!char.IsLetterOrDigit(character) && character != '.' && character != '_')
                {
                    return false;
                }
            }

            return true;
        }

        // Deliberately permissive. An address is only proved real by sending to it, so this
        // catches obvious typos rather than pretending to verify (REQ-9).
        private static bool IsPlausibleEmail(string email)
        {
            int at = email.IndexOf('@');

            return at > 0
                && email.IndexOf('@', at + 1) < 0
                && email.IndexOf('.', at) > at + 1
                && !email.EndsWith(".")
                && !email.Contains(' ');
        }

        // Permissive for the same reason as the email check, and because phone formats vary by
        // country - it rejects letters and obviously short entries, nothing more (REQ-9).
        private static bool IsPlausiblePhoneNumber(string phoneNumber)
        {
            int digits = 0;

            foreach (char character in phoneNumber)
            {
                if (char.IsDigit(character))
                {
                    digits++;
                }
                else if (character != ' ' && character != '-' && character != '+'
                    && character != '(' && character != ')')
                {
                    return false;
                }
            }

            return digits >= 6;
        }

        private static RegistrationResult Rejected(string message)
        {
            return new RegistrationResult { Success = false, Message = message };
        }
    }
}