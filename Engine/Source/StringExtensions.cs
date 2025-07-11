/*
 * Class: CodeClear.NaturalDocs.Engine.StringExtensions
 * ____________________________________________________________________________
 *
 * A static class for all the functions added to the string and StringBuilder types.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2025 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;


namespace CodeClear.NaturalDocs.Engine
	{
	static public partial class StringExtensions
		{

		// Group: Normalization Functions
		// __________________________________________________________________________


		/* Function: CondenseWhitespace
		 * Replaces all strings of whitespace characters with a single space.
		 */
		static public string CondenseWhitespace (this string input)
			{
			return FindWhitespaceCharsRegex().Replace(input, " ");
			}


		/* Function: RemoveWhitespace
		 * Removes all whitespace from the string.
		 */
		static public string RemoveWhitespace (this string input)
			{
			return FindWhitespaceCharsRegex().Replace(input, "");
			}


		/* Function: NormalizeLineBreaks
		 * Replaces all line breaks in a string with the native platform's format, which is CRLF for Windows and LF for Mac/Linux.
		 */
		static public string NormalizeLineBreaks (this string input)
			{
			bool hasCR = (input.IndexOf('\r') != -1);
			bool hasLF = (input.IndexOf('\n') != -1);

			#if WINDOWS
				if (hasCR && hasLF)
					{  return input;  }
				else if (hasCR)
					{  return input.Replace("\r", "\r\n");  }
				else // hasLF
					{  return input.Replace("\n", "\r\n");  }
			#elif MAC || LINUX
				if (hasCR)
					{  return input.Replace("\r", "");  }
				else
					{  return input;  }
			#else
				throw new Exception("Unsupported platform");
			#endif
			}


		/* Function: NormalizeKey
		 * Normalizes a string key by applying <Collections.KeySettings>.
		 */
		public static string NormalizeKey (this string key, Collections.KeySettings keySettings)
			{
			if ((keySettings & Collections.KeySettings.IgnoreCase) != 0)
				{  key = key.ToLower(CultureInfo.InvariantCulture);  }
			if ((keySettings & Collections.KeySettings.NormalizeUnicode) != 0)
				{  key = key.Normalize(System.Text.NormalizationForm.FormKC);  }

			return key;
			}



		// Group: Entity Encoding Functions
		// __________________________________________________________________________


		/* Function: EntityEncode
		 * Returns the input character as a string with <, >, ", and & replaced by their entity encodings.  Technically an
		 * extension to char instead of string.
		 */
		public static string EntityEncode (this char input)
			{
			// DEPENDENCY: Must update Styles/NDCore.js String.EntityDecode() if this function changes.

			if (input == '"')
				{  return "&quot;";  }
			else if (input == '&')
				{  return "&amp;";  }
			else if (input == '<')
				{  return "&lt;";  }
			else if (input == '>')
				{  return "&gt;";  }
			else
				{  return input.ToString();  }
			}


		/* Function: EntityEncode
		 * Returns the string with <, >, ", and & replaced by their entity encodings.  If the result string will be appended to a
		 * StringBuilder, it is more efficient to use <StringBuilderExtensions.EntityEncodeAndAppend()> instead of this function.
		 */
		public static string EntityEncode (this string input)
			{
			if (input.IndexOfAny(EntityCharLiterals) == -1)
				{  return input;  }

			StringBuilder output = new StringBuilder();
			output.EntityEncodeAndAppend(input);
			return output.ToString();
			}


		/* Function: EntityEncodeAndAppend
		 * Appends the input character to the StringBuilder with <, >, ", and & replaced by their entity encodings.
		 */
		public static void EntityEncodeAndAppend (this StringBuilder output, char input)
			{
			output.Append(input.EntityEncode());
			}


		/* Function: EntityEncodeAndAppend
		 * Appends the contents of the input string to the StringBuilder with <, >, ", and & replaced by their entity
		 * encodings.
		 */
		public static void EntityEncodeAndAppend (this StringBuilder output, string input)
			{
			output.EntityEncodeAndAppend(input, 0, input.Length);
			}


		/* Function: EntityEncodeAndAppend
		 * Appends the contents of the input string to the StringBuilder with <, >, ", and & replaced by their entity
		 * encodings.  Offset and length represent the portion of the input string to convert.
		 */
		public static void EntityEncodeAndAppend (this StringBuilder output, string input, int offset, int length)
			{
			int endOfInput = offset + length;

			while (offset < endOfInput)
				{
				int nextEntityChar = input.IndexOfAny(EntityCharLiterals, offset, endOfInput - offset);

				if (nextEntityChar == -1)
					{  break;  }

				if (nextEntityChar != offset)
					{  output.Append(input, offset, nextEntityChar - offset);  }

				output.Append( input[nextEntityChar].EntityEncode() );

				offset = nextEntityChar + 1;
				}

			if (offset < endOfInput)
				{  output.Append(input, offset, endOfInput - offset);  }
			}


		/* Function: EntityDecode
		 * Returns the string with <, >, ", and & restored from their entity encodings.
		 */
		public static string EntityDecode (this string input)
			{
			if (input.IndexOf('&') == -1)
				{  return input;  }

			string output = input.Replace("&quot;", "\"");
			output = output.Replace("&lt;", "<");
			output = output.Replace("&gt;", ">");
			output = output.Replace("&amp;", "&");

			return output;
			}



		// Group: String Escaping Functions
		// __________________________________________________________________________


		/* Function: StringEscape
		 * Returns the string with ", ', and \ escaped so that they can be put into a JavaScript string.  If the result will be
		 * appended to a StringBuilder, it is more efficient to use <StringBuilderExtensions.StringEscapeAndAppend()>
		 * instead of this function.
		 */
		public static string StringEscape (this string input)
			{
			if (input.IndexOfAny(EscapedStringChars) == -1)
				{  return input;  }

			StringBuilder output = new StringBuilder();
			output.StringEscapeAndAppend(input);
			return output.ToString();
			}


		/* Function: StringEscapeAndAppend
		 * Appends the contents of the input string to the StringBuilder with ', ", and \ escaped.
		 */
		public static void StringEscapeAndAppend (this StringBuilder output, string input)
			{
			output.StringEscapeAndAppend(input, 0, input.Length);
			}


		/* Function: StringEscapeAndAppend
		 * Appends the contents of the input string to the StringBuilder with ', ", and \ escaped.  Offset and length
		 * represent the portion of the input string to convert.
		 */
		public static void StringEscapeAndAppend (this StringBuilder output, string input, int offset, int length)
			{
			int endOfInput = offset + length;

			while (offset < endOfInput)
				{
				int nextEscapedChar = input.IndexOfAny(EscapedStringChars, offset, endOfInput - offset);

				if (nextEscapedChar == -1)
					{  break;  }

				if (nextEscapedChar != offset)
					{  output.Append(input, offset, nextEscapedChar - offset);  }

				output.Append('\\');
				output.Append(input[nextEscapedChar]);

				offset = nextEscapedChar + 1;
				}

			if (offset < endOfInput)
				{  output.Append(input, offset, endOfInput - offset);  }
			}



		// Group: Other Conversion Functions
		// __________________________________________________________________________


		/* Function: ToHTML
		 * Converts the plain text string to HTML.  This encodes <, >, ", and & as entity characters and encodes double
		 * spaces with &nbsp;.
		 */
		public static string ToHTML (this string text)
			{
			text = text.EntityEncode();
			text = text.ConvertLineBreaksToBR();
			text = text.ConvertMultipleWhitespaceChars();

			return text;
			}


		/* Function: ConvertCopyrightAndTrademark
		 * Returns a string with all occurrances of (c), (r), and (tm) converted to their respective Unicode characters.
		 */
		public static string ConvertCopyrightAndTrademark (this string input)
			{
			return FindCopyrightAndTrademarksRegex().Replace(input,
				delegate (Match match)
					{
					char mainChar = match.Value[1];

					if (mainChar == 'c' || mainChar == 'C')
						{  return "©";  }
					else if (mainChar == 'r' || mainChar == 'R')
						{  return "®";  }
					else // (mainChar == 't' || mainChar == 'T')
						{  return "™";  }
					}
				);
			}


		/* Function: ConvertLineBreaksToBR
		 *	 Replaces line breaks in any format with <br> or <br /> tags.  If you plan to use this and
		 *	 <ConvertMultipleWhitespaceChars()> you must call this function first.
		 */
		public static string ConvertLineBreaksToBR (this string input, bool includeSlash = false)
			{
			if (includeSlash)
				{  return FindLineBreakRegex().Replace(input, "<br />");  }
			else
				{  return FindLineBreakRegex().Replace(input, "<br>");  }
			}


		/* Function: ConvertMultipleWhitespaceChars
		 * Replaces instances of at least two whitespace characters in a row with &nbsp; and a space.  If you plan to use this and
		 * <ConvertLineBreaksToBR()>, you must call the other function first.
		 */
		public static string ConvertMultipleWhitespaceChars (this string input)
			{
			return FindMultipleWhitespaceCharsRegex().Replace(input, "&nbsp; ");
			}



		// Group: Other Functions
		// __________________________________________________________________________


		/* Function: IsOnlyAToZ
		 * Returns whether the passed string only contains the letters A to Z.
		 */
		public static bool IsOnlyAToZ (this string input)
			{
			return (!FindNonAToZRegex().IsMatch(input));
			}


		/* Function: OnlyAToZ
		 * Converts the passed string to a version that only contains the letters A to Z.  All other characters are stripped or
		 * replaced with related characters.  If there are no ASCII characters available it returns null.
		 */
		public static string OnlyAToZ (this string input)
			{
			// Use a compatibility decomposition to increase the chances of finding ASCII letters.  Hopefully some accented
			// Latin characters will be separated into ASCII characters and combining characters.
			string result = input.Normalize(System.Text.NormalizationForm.FormKD);

			result = FindNonAToZRegex().Replace(result, "");

			if (String.IsNullOrEmpty(result))
				{  result = null;  }

			return result;
			}


		/* Function: Count
		 * Returns the number of times the passed character appears in the string.
		 */
		static public int Count (this string input, char character)
			{
			return input.Count(character, 0, input.Length);
			}


		/* Function: Count
		 * Returns the number of times the passed character appears in the string segment.
		 */
		static public int Count (this string input, char character, int index, int length)
			{
			int endingIndex = index + length;
			int count = 0;

			while (index < endingIndex)
				{
				int match = input.IndexOf(character, index, endingIndex - index);

				if (match == -1)
					{  break;  }

				count++;
				index = match + 1;
				}

			return count;
			}



		// Group: Static Variables
		// __________________________________________________________________________


		/* var: EntityCharLiterals
		 * An array of characters that would need to be converted to entity characters in <NDMarkup>.  Useful for
		 * String.IndexOfAny(char[]).
		 */
		static public char[] EntityCharLiterals = new char[] { '"', '&', '<', '>' };


		/* var: EscapedStringChars
		 * An array of characters that must be escaped in a JavaScript string.
		 */
		static public char[] EscapedStringChars = new char[] { '"', '\'', '\\' };



		// Group: Regular Expressions
		// __________________________________________________________________________


		/* Regex: FindWhitespaceCharsRegex
		 *
		 * Will match one or more consecutive whitespace characters in a string.  Multiple consecutive whitespace characters will
		 * be returned as a single match rather than matching on each individual character.
		 */
		[GeneratedRegex("""\s+""",
								  RegexOptions.Singleline | RegexOptions.CultureInvariant)]
		static private partial Regex FindWhitespaceCharsRegex();


		/* Regex: FindMultipleWhitespaceCharsRegex
		 *
		 * Will match _two_ or more consecutive whitespace characters in a string.  Each set of consecutive whitespace characters
		 * will be returned as a single match.  It will not match on single characters at all.
		 */
		[GeneratedRegex("""\s{2,}""",
								  RegexOptions.Singleline | RegexOptions.CultureInvariant)]
		static private partial Regex FindMultipleWhitespaceCharsRegex();


		/* Regex: FindLineBreakRegex
		 *
		 * Will match a single line break in a string, regardless if it's in CRLF, CR, or LF format.  This has the benefit of treating
		 * CRLF as a single line break.
		 */
		[GeneratedRegex("""\r\n|\n|\r""",
								  RegexOptions.Singleline | RegexOptions.CultureInvariant)]
		static private partial Regex FindLineBreakRegex();


		/* Regex: FindCopyrightAndTrademarksRegex
		 *
		 * Will match occurrences of (c), (r), or (tm) in a string.
		 */
		[GeneratedRegex("""\((?:c|r|tm)\)""",
								  RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
		static private partial Regex FindCopyrightAndTrademarksRegex();


		/* Regex: FindNonAToZRegex
		 *
		 * Will match occurrences of any characters that are not A to Z.  Multiple consecutive non-A to Z characters will be
		 * returned as a single match rather than matching on each individual character.
		 */
		[GeneratedRegex("""[^a-zA-Z]+""",
								  RegexOptions.Singleline | RegexOptions.CultureInvariant)]
		static private partial Regex FindNonAToZRegex();

		}
	}
