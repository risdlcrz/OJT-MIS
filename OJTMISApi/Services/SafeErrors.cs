namespace OJTMISApi.Services
{
    /// <summary>
    /// The only error text the API sends to the browser. Full exceptions,
    /// SQL errors and stack traces go to the server log (with the reference
    /// id below), never into a response.
    /// </summary>
    public static class SafeErrors
    {
        public const string Server =
            "Something went wrong on our side. Please try again. If it keeps happening, contact an administrator.";

        public const string InvalidFormat = "Some of the information entered has an invalid format. Check the form and try again.";

        /// <summary>
        /// Joins field validation messages for the client, replacing the ones
        /// written by the JSON reader ("The JSON value could not be converted
        /// to System.Int32. Path: $.id | LineNumber: 0 ...") which expose type
        /// names and request structure.
        /// </summary>
        public static string ValidationMessage(IDictionary<string, string[]> errors)
        {
            var messages = new List<string>();
            foreach (var (key, values) in errors)
            {
                foreach (var value in values)
                {
                    var text = IsTechnical(key, value) ? InvalidFormat : value;
                    if (!messages.Contains(text)) messages.Add(text);
                }
            }

            return messages.Count == 0 ? InvalidFormat : string.Join(" ", messages);
        }

        /// <summary>True for messages produced by the framework rather than written for users.</summary>
        public static bool IsTechnical(string key, string message) =>
            key.StartsWith('$') ||
            message.Contains("LineNumber", StringComparison.Ordinal) ||
            message.Contains("BytePositionInLine", StringComparison.Ordinal) ||
            message.Contains("Path: $", StringComparison.Ordinal) ||
            message.Contains("System.", StringComparison.Ordinal) ||
            message.Contains("could not be converted", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("Exception", StringComparison.Ordinal);
    }
}
