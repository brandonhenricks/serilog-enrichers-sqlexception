using System.Text.RegularExpressions;
using System.Xml;

namespace Serilog.Enrichers.SqlException.Helpers;

/// <summary>
/// Extracts deadlock graph XML from SQL Server error messages.
/// </summary>
/// <remarks>
/// This extractor uses a regex pattern to locate deadlock-list XML fragments within error messages.
/// The pattern supports opening tags with attributes and namespaces. The extracted content is validated
/// to ensure it is well-formed XML before being returned.
/// 
/// Limitations:
/// - Only extracts the first deadlock-list element found in the message
/// - Requires the closing tag to match the opening tag name (deadlock-list)
/// - Does not handle nested deadlock-list elements
/// </remarks>
internal static class DeadlockGraphExtractor
{
    // Updated regex pattern to handle attributes and namespaces in the opening tag
    // Pattern: <deadlock-list[any attributes]>...</deadlock-list>
    private static readonly Regex s_deadlockGraphPattern = new(
        @"<deadlock-list(?:\s+[^>]*)?>.*?</deadlock-list>",
        RegexOptions.Singleline | RegexOptions.Compiled);

    /// <summary>
    /// Attempts to extract the deadlock graph XML from an error message.
    /// </summary>
    /// <param name="errorMessage">The SQL Server error message.</param>
    /// <param name="graph">The extracted deadlock graph XML, or null if not found or invalid.</param>
    /// <returns>True if a valid deadlock graph was found; otherwise, false.</returns>
    public static bool TryExtractGraph(string? errorMessage, out string? graph)
    {
        graph = null;

        if (string.IsNullOrWhiteSpace(errorMessage))
        {
            return false;
        }

        var match = s_deadlockGraphPattern.Match(errorMessage);
        if (match.Success)
        {
            var potentialGraph = match.Value;
            
            // Validate that the extracted content is well-formed XML
            if (IsWellFormedXml(potentialGraph))
            {
                graph = potentialGraph;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Validates that a string contains well-formed XML.
    /// </summary>
    /// <param name="xml">The XML string to validate.</param>
    /// <returns>True if the XML is well-formed; otherwise, false.</returns>
    private static bool IsWellFormedXml(string xml)
    {
        try
        {
            var settings = new XmlReaderSettings
            {
                ConformanceLevel = ConformanceLevel.Fragment,
                CheckCharacters = true,
                IgnoreWhitespace = false,
                IgnoreComments = false,
                IgnoreProcessingInstructions = false
            };

            using var stringReader = new System.IO.StringReader(xml);
            using var xmlReader = XmlReader.Create(stringReader, settings);
            
            // Read through the entire XML to validate it
            while (xmlReader.Read())
            {
                // Just reading through validates the structure
            }

            return true;
        }
        catch (XmlException)
        {
            // XML is malformed
            return false;
        }
    }
}
