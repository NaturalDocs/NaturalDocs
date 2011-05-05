/* 
 * Class: GregValure.NaturalDocs.Engine.StringExtensions
 * ____________________________________________________________________________
 * 
 * A static class for all the functions added to the string type.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2011 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Text;
using System.Text.RegularExpressions;


namespace GregValure.NaturalDocs.Engine
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
		
		/* Function: NormalizeKey
		 * Converts the string to a version suitable for use as the key in a table according to the passed parameters.  If 
		 * ignoreCase is set it will be converted to lowercase.  If normalizeUnicode is set Unicode compatibility normalization 
		 * will be applied (FormKC).
		 */
		public static string NormalizeKey (this string key, bool ignoreCase, bool normalizeUnicode)
			{
			if (ignoreCase)
				{  key = key.ToLower();  }
			if (normalizeUnicode)
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
					string lcMatch = match.Value.ToLower();
					
					if (lcMatch == "(c)")
						{  return "©";  }
					else if (lcMatch == "(r)")
						{  return "®";  }
					else // (lcMatch == "(tm)")
						{  return "™";  }
					}
				);
			}

		/* Function: ConvertMultipleWhitespaceChars
		 * Replaces instances of at least two whitespace characters in a row with &nbsp; and a space.
		 */
		public static string ConvertMultipleWhitespaceChars (this string input)
			{
			return MultipleWhitespaceCharsRegex.Replace(input, "&nbsp; ");
			}

		

		// Group: Entity and Escapement Functions
		// __________________________________________________________________________					


		/* Function: EntityEncode
		 * Returns the input character as a string with <, >, ", and & replaced by their entity encodings.  Technically an 
		 * extension to char instead of string.
		 */
		public static string EntityEncode (this char input)
			{
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

		static public Regex.WhitespaceChars WhitespaceCharsRegex = new Regex.WhitespaceChars();
		static public Regex.NDMarkup.CopyrightAndTrademark CopyrightAndTrademarkRegex = new Regex.NDMarkup.CopyrightAndTrademark();
		static public Regex.MultipleWhitespaceChars MultipleWhitespaceCharsRegex = new Regex.MultipleWhitespaceChars();
			
		}
	}