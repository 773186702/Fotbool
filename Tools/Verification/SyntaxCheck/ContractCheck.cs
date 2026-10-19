using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;

namespace Fotbool.Verification
{
    /// <summary>
    /// Validates a human readable contracts file against the API dump produced by the syntax pass.
    /// A contract is the machine checkable form of "the rest of the game calls this member", so
    /// renaming or dropping one fails verification instead of failing silently at play time.
    /// </summary>
    internal static class ContractCheck
    {
        public static int Run(string contractsPath, string dumpPath, StringBuilder report)
        {
            report.AppendLine("Contracts file: " + contractsPath);
            if (!File.Exists(contractsPath))
            {
                report.AppendLine("  ERROR contracts file not found.");
                return 1;
            }
            if (!File.Exists(dumpPath))
            {
                report.AppendLine("  ERROR API dump not found: " + dumpPath);
                return 1;
            }

            using (JsonDocument dump = JsonDocument.Parse(File.ReadAllText(dumpPath)))
            using (JsonDocument contracts = JsonDocument.Parse(File.ReadAllText(contractsPath)))
            {
                if (!contracts.RootElement.TryGetProperty("types", out JsonElement required) ||
                    required.ValueKind != JsonValueKind.Array)
                {
                    report.AppendLine("  ERROR contracts file must contain a \"types\" array.");
                    return 1;
                }

                JsonElement types = dump.RootElement.GetProperty("types");
                var errors = new List<string>();
                int checkedTypes = 0;
                int checkedMembers = 0;

                foreach (JsonElement requirement in required.EnumerateArray())
                {
                    string name = requirement.GetProperty("name").GetString();
                    checkedTypes++;
                    if (!types.TryGetProperty(name, out JsonElement type))
                    {
                        errors.Add("missing type " + name);
                        continue;
                    }
                    CheckType(name, requirement, type, errors);
                    if (!requirement.TryGetProperty("members", out JsonElement members)) continue;
                    JsonElement memberTable = type.GetProperty("members");
                    foreach (JsonElement member in members.EnumerateArray())
                    {
                        checkedMembers++;
                        CheckMember(name, member, memberTable, errors);
                    }
                }

                foreach (string error in errors) report.AppendLine("  ERROR " + error);
                report.AppendLine("Contracts: " + checkedTypes.ToString(CultureInfo.InvariantCulture) + " type(s) and " +
                                  checkedMembers.ToString(CultureInfo.InvariantCulture) + " member(s) checked");
                report.AppendLine(errors.Count == 0
                    ? "[PASS] every declared contract is satisfied"
                    : "[FAIL] " + errors.Count.ToString(CultureInfo.InvariantCulture) + " contract violation(s)");
                return errors.Count == 0 ? 0 : 1;
            }
        }

        private static void CheckType(string name, JsonElement requirement, JsonElement actual, List<string> errors)
        {
            if (requirement.TryGetProperty("kind", out JsonElement kind))
            {
                string declared = actual.GetProperty("kind").GetString();
                if (!string.Equals(declared, kind.GetString(), StringComparison.Ordinal))
                    errors.Add(name + " should be a " + kind.GetString() + " but is a " + declared);
            }

            if (!requirement.TryGetProperty("baseTypes", out JsonElement baseTypes)) return;
            foreach (JsonElement baseType in baseTypes.EnumerateArray())
                if (!HasBaseType(actual, baseType.GetString()))
                    errors.Add(name + " must derive from or implement " + baseType.GetString());
        }

        private static void CheckMember(string typeName, JsonElement requirement, JsonElement memberTable, List<string> errors)
        {
            string name = requirement.GetProperty("name").GetString();
            string where = typeName + "." + name;
            if (!memberTable.TryGetProperty(name, out JsonElement actual))
            {
                errors.Add(typeName + " is missing member " + name);
                return;
            }

            string kind = actual.GetProperty("kind").GetString();
            string visibility = actual.GetProperty("visibility").GetString();
            bool isEnumMember = string.Equals(kind, "enumMember", StringComparison.Ordinal);

            if (!isEnumMember && !string.Equals(visibility, "public", StringComparison.Ordinal))
                errors.Add(where + " must be public but is " + visibility);

            if (requirement.TryGetProperty("kind", out JsonElement expectedKind) &&
                !string.Equals(kind, expectedKind.GetString(), StringComparison.Ordinal))
                errors.Add(where + " should be a " + expectedKind.GetString() + " but is a " + kind);

            if (requirement.TryGetProperty("parameters", out JsonElement expectedParameters) &&
                expectedParameters.TryGetInt32(out int expected) &&
                actual.TryGetProperty("parameters", out JsonElement actualParameters) &&
                actualParameters.TryGetInt32(out int count) && count != expected)
                errors.Add(where + " should take " + expected.ToString(CultureInfo.InvariantCulture) +
                           " parameter(s) but takes " + count.ToString(CultureInfo.InvariantCulture));

            if (!requirement.TryGetProperty("type", out JsonElement expectedType)) return;
            string expectedName = Normalize(expectedType.GetString());
            string actualName = Normalize(actual.GetProperty("type").GetString());
            if (!string.Equals(expectedName, actualName, StringComparison.Ordinal))
                errors.Add(where + " should have type " + expectedType.GetString() + " but has " +
                           actual.GetProperty("type").GetString());
        }

        private static bool HasBaseType(JsonElement type, string expected)
        {
            if (!type.TryGetProperty("baseTypes", out JsonElement baseTypes)) return false;
            string wanted = Normalize(expected);
            foreach (JsonElement candidate in baseTypes.EnumerateArray())
            {
                string value = Normalize(candidate.GetString());
                if (string.Equals(value, wanted, StringComparison.Ordinal)) return true;
                if (value.EndsWith("." + wanted, StringComparison.Ordinal)) return true;
            }
            return false;
        }

        /// <summary>
        /// Compares type spellings loosely: whitespace and nullable markers are dropped, and every
        /// identifier is reduced to its last segment so "List&lt;PlayerController&gt;" matches
        /// "System.Collections.Generic.List&lt;Fotbool.Gameplay.PlayerController&gt;".
        /// </summary>
        private static string Normalize(string typeName)
        {
            if (string.IsNullOrEmpty(typeName)) return string.Empty;

            var builder = new StringBuilder(typeName.Length);
            var identifier = new StringBuilder();

            foreach (char character in typeName)
            {
                if (char.IsWhiteSpace(character) || character == '?') continue;

                bool isIdentifierCharacter = char.IsLetterOrDigit(character) || character == '_' || character == '.';
                if (isIdentifierCharacter)
                {
                    identifier.Append(character);
                    continue;
                }

                FlushIdentifier(identifier, builder);
                builder.Append(character);
            }

            FlushIdentifier(identifier, builder);
            return builder.ToString();
        }

        private static void FlushIdentifier(StringBuilder identifier, StringBuilder builder)
        {
            if (identifier.Length == 0) return;
            string text = identifier.ToString();
            int lastDot = text.LastIndexOf('.');
            builder.Append(lastDot >= 0 ? text.Substring(lastDot + 1) : text);
            identifier.Length = 0;
        }
        private static void ValidateMember(string typeName, JsonElement requirement, JsonElement memberTable, List<string> errors)
        {
            string name = requirement.GetProperty("name").GetString();
            if (!memberTable.TryGetProperty(name, out JsonElement actual))
            {
                errors.Add(typeName + "." + name + " is missing");
                return;
            }

            string where = typeName + "." + name;

            if (requirement.TryGetProperty("kind", out JsonElement kind) &&
                !string.Equals(actual.GetProperty("kind").GetString(), kind.GetString(), StringComparison.Ordinal))
            {
                errors.Add(where + " should be a " + kind.GetString() + " but is a " + actual.GetProperty("kind").GetString());
            }

            string visibility = requirement.TryGetProperty("visibility", out JsonElement vis) ? vis.GetString() : "public";
            if (!string.Equals(actual.GetProperty("visibility").GetString(), visibility, StringComparison.Ordinal))
            {
                errors.Add(where + " must be " + visibility + " but is " + actual.GetProperty("visibility").GetString());
            }

            if (requirement.TryGetProperty("parameters", out JsonElement parameters) &&
                actual.GetProperty("parameters").GetInt32() != parameters.GetInt32())
            {
                errors.Add(where + " must take " + parameters.GetInt32().ToString(CultureInfo.InvariantCulture) +
                           " parameter(s) but takes " + actual.GetProperty("parameters").GetInt32().ToString(CultureInfo.InvariantCulture));
            }

            if (requirement.TryGetProperty("type", out JsonElement expectedType))
            {
                string actualType = Normalise(actual.GetProperty("type").GetString());
                string normalisedExpected = Normalise(expectedType.GetString());
                if (!string.Equals(actualType, normalisedExpected, StringComparison.Ordinal))
                    errors.Add(where + " should be of type " + normalisedExpected + " but is " + actualType);
            }
        }

        /// <summary>Strips global:: and namespace prefixes so contracts stay readable.</summary>
        private static string Normalise(string typeName)
        {
            if (string.IsNullOrEmpty(typeName)) return string.Empty;
            string value = typeName.Replace("global::", string.Empty).Trim();
            int generic = value.IndexOf('<');
            if (generic < 0) return LastSegment(value);
            string head = value.Substring(0, generic);
            string tail = value.Substring(generic);
            return LastSegment(head) + tail.Replace("global::", string.Empty);
        }

        private static string LastSegment(string qualifiedName)
        {
            int separator = qualifiedName.LastIndexOf('.');
            return separator < 0 ? qualifiedName : qualifiedName.Substring(separator + 1);
        }

        private static bool ContainsBase(JsonElement type, string expected)
        {
            if (!type.TryGetProperty("baseTypes", out JsonElement baseTypes)) return false;
            foreach (JsonElement baseType in baseTypes.EnumerateArray())
                if (string.Equals(LastSegment(baseType.GetString()), LastSegment(expected), StringComparison.Ordinal))
                    return true;
            return false;
        }
    }
}