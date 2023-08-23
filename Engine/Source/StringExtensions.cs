/*
 * Class: CodeClear.NaturalDocs.Engine.StringExtensions
 * ____________________________________________________________________________
 *
 * A static class for all the functions added to the string type.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2023 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;


namespace CodeClear.NaturalDocs.Engine
	{
	static public class StringExtensions
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Function: CondenseWhitespace
		 * Replaces all strings of whitespace characters with a single space.
		 */
		static public string CondenseWhitespace (this string input)
			{
			return WhitespaceCharsRegex.Replace(input, " ");
			}

		/* Function: RemoveWhitespace
		 * Removes all whitespace from the string.
		 */
		static public string RemoveWhitespace (this string input)
			{
			return WhitespaceCharsRegex.Replace(input, "");
			}

		/* Function: NormalizeLineBreaks
		 * Replaces all line breaks in a string with the native platform's format, which is CRLF for Windows and LF for Mac/Linux.
		 */
		static public string NormalizeLineBreaks (this string input)
			{
			bool hasCR = (input.IndexOf('\r') != -1);
			bool hasLF = (input.IndexOf('\n') != -1);

			if (Engine.SystemInfo.OnUnix)
				{
				if (hasCR)
					{  return input.Replace("\r", "");  }
				else
					{  return input;  }
				}
			else // Windows
				{
				if (hasCR && hasLF)
					{  return input;  }
				else if (hasCR)
					{  return input.Replace("\r", "\r\n");  }
				else // hasLF
					{  return input.Replace("\n", "\r\n");  }
				}
			}

		/* Function: SplitLines
		 * Returns the string as an array of lines regardless of the format of line break it contains.
		 */
		static public string[] SplitLines (this string input)
			{
			return input.Split(LineBreakChars, StringSplitOptions.RemoveEmptyEntries);
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

		/* Function: OnlyAToZ
		 * Converts the passed string to a version that only contains the letters A to Z.  All other characters are stripped or
		 * replaced with related characters.  If there are no ASCII characters available it returns null.
		 */
		public static string OnlyAToZ (this string input)
			{
			// Use a compatibility decomposition to increase the chances of finding ASCII letters.  Hopefully some accented
			// Latin characters will be separated into ASCII characters and combining characters.
			string result = input.Normalize(System.Text.NormalizationForm.FormKD);

			Regex.NonASCIILetters nonASCIILettersRegex = new Regex.NonASCIILetters();
			result = nonASCIILettersRegex.Replace(result, "");

			if (String.IsNullOrEmpty(result))
				{  result = null;  }

			return result;
			}

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
			return CopyrightAndTrademarkRegex.Replace(input,
				delegate (Match match)
					{
					string lcMatch = match.Value.ToLower(CultureInfo.InvariantCulture);

					if (lcMatch == "(c)")
						{  return "©";  }
					else if (lcMatch == "(r)")
						{  return "®";  }
					else // (lcMatch == "(tm)")
						{  return "™";  }
					}
				);
			}

		/* Function: ConvertLineBreaksToBR
		 *	 Replaces line breaks in any format with <br> tags.  If you plan to use this and <ConvertMultipleWhitespaceChars()>,
		 *	 you must call this function first.
		 */
		public static string ConvertLineBreaksToBR (this string input)
			{
			return LineBreakRegex.Replace(input, "<br>");
			}


		/* Function: ConvertMultipleWhitespaceChars
		 * Replaces instances of at least two whitespace characters in a row with &nbsp; and a space.  If you plan to use this and
		 * <ConvertLineBreaksToBR()>, you must call the other function first.
		 */
		public static string ConvertMultipleWhitespaceChars (this string input)
			{
			return MultipleWhitespaceCharsRegex.Replace(input, "&nbsp; ");
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



		// Group: Entity and Escapement Functions
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

		static public char[] LineBreakChars = new char[] { '\r', '\n' };

		static public Regex.WhitespaceChars WhitespaceCharsRegex = new Regex.WhitespaceChars();
		static public Regex.NDMarkup.CopyrightAndTrademark CopyrightAndTrademarkRegex = new Regex.NDMarkup.CopyrightAndTrademark();
		static public Regex.MultipleWhitespaceChars MultipleWhitespaceCharsRegex = new Regex.MultipleWhitespaceChars();
		static public Regex.LineBreak LineBreakRegex = new Regex.LineBreak();

		}
	}
