using System.Collections.Generic;

namespace LibraryQA.Core.Database
{
    // Staff member lookup (REQ-6). Kept in its own file so it does not collide with other work in DatabaseHelper.cs.
    public partial class DatabaseHelper
    {
        // Returns only the details staff may see (REQ-6, REQ-11) - columns are named, so the password
        // hash and login data are never loaded. A staff ID returns null, so only members can be looked up.
        public Dictionary<string, object>? GetMemberBasicInfo(int memberId)
        {
            OpenConnection();

            using (var command = _connection!.CreateCommand())
            {
                command.CommandText = @"
                    SELECT AccountID, FirstName, LastName, AccountStatus, Email, PhoneNumber
                    FROM Accounts
                    WHERE AccountID = @memberId AND Role = 'Member'";

                command.Parameters.AddWithValue("@memberId", memberId);

                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new Dictionary<string, object>
                        {
                            ["AccountID"] = reader["AccountID"],
                            ["FirstName"] = reader["FirstName"],
                            ["LastName"] = reader["LastName"],
                            ["AccountStatus"] = reader["AccountStatus"],
                            ["Email"] = reader["Email"] ?? "",
                            ["PhoneNumber"] = reader["PhoneNumber"] ?? ""
                        };
                    }
                }
            }

            return null;
        }
    }
}