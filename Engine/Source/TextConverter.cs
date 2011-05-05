/* 
 * Class: GregValure.NaturalDocs.Engine.TextConverter
 * ____________________________________________________________________________
 * 
 * Functions to manage converting between plain text, HTML, and the <NDMarkup Format>.  There's significant
 * overlap between HTML and NDMarkup so it makes sense to put them all in one package.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2011 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Text;
using System.Text.RegularExpressions;


namespace GregValure.NaturalDocs.Engine
	{
	static public class TextConverter
		{
		
		// Group: Functions
		// __________________________________________________________________________
		

		/* Function: TextToHTML
		 * Converts a plain text string to HTML.  This encodes <, >, ", and & as entity characters, replaces generic quotes
		 * with left and right, and encodes double spaces with &nbsp;.
		 */
		public static string TextToHTML (string text, bool convertQuotes = true, bool convertDoubleSpaces = true)
			{
			if (convertQuotes)
				{  text = ConvertQuotes(text);  }

			text = EncodeEntityChars(text);

			if (convertDoubleSpaces)
				{  text = ConvertMultipleWhitespaceChars(text);  }

			return text;
			}


		/* Function: EncodeEntityChar
		 * Returns the input character as a string with <, >, ", and & replaced by their entity encodings.
		 */
		public static string EncodeEntityChar (char input)
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


		/* Function: EncodeEntityChars
		 * Returns the input string with <, >, ", and & replaced by their entity encodings.  If the result
		 * string will be appended to a StringBuilder, it is more efficient to use <EncodeEntityCharsAndAppend> instead 
		 * of this function.
		 */
		public static string EncodeEntityChars (string input)
			{
			if (input.IndexOfAny(EntityCharLiterals) == -1)
				{  return input;  }

			StringBuilder output = new StringBuilder();
			output.EncodeEntityCharsAndAppend(input);
			return output.ToString();
			}
		
		
		/* Function: DecodeEntityChars
		 * Returns the input string with <, >, ", and & restored from their entity encodings.
		 */
		public static string DecodeEntityChars (string input)
			{
			if (input.IndexOf('&') == -1)
				{  return input;  }

			string output = input.Replace("&quot;", "\"");
			output = output.Replace("&lt;", "<");
			output = output.Replace("&gt;", ">");
			output = output.Replace("&amp;", "&");

			return output;
			}


		/* Function: EscapeStringChars
		 * Returns the input string with ", ', and \ escaped so that they can be put into a JavaScript string.  If the result
		 * string will be appended to a StringBuilder, it is more efficient to use <EscapeStringCharsAndAppend> instead 
		 * of this function.
		 */
		public static string EscapeStringChars (string input)
			{
			if (input.IndexOfAny(EscapedStringChars) == -1)
				{  return input;  }

			StringBuilder output = new StringBuilder();
			output.EscapeStringCharsAndAppend(input);
			return output.ToString();
			}
		
		
		/* Function: ConvertCopyrightAndTrademark
		 * Returns a string with all occurrances of (c), (r), and (tm) converted to their respective Unicode characters.
		 */
		public static string ConvertCopyrightAndTrademark (string input)
			{
			return copyrightAndTrademarkRegex.Replace(input, 
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


		/* Function: ConvertQuotes
		 * Converts neutral quotes and apostrophes into left and right Unicode characters.
		 */
		public static string ConvertQuotes (string input)
			{
			int index = input.IndexOfAny(QuoteLiterals);

			if (index == -1)
				{  return input;  }

			StringBuilder output = new StringBuilder(input);
			string acceptableLeftCharacters = " \t([{";

			do
				{
				if (index == 0 || acceptableLeftCharacters.IndexOf(input[index - 1]) != -1)
					{  
					if (output[index] == '"')
						{  output[index] = '“';  }
					else
						{  output[index] = '‘';  }
					}
				else
					{
					if (output[index] == '"')
						{  output[index] = '”';  }
					else
						{  output[index] = '’';  }
					}

				index = input.IndexOfAny(QuoteLiterals, index + 1);
				}
			while (index != -1);

			return output.ToString();
			}


		/* Function: ConvertMultipleWhitespaceChars
		 * Replaces instances of at least two whitespace characters in a row with &nbsp; and a space.
		 */
		public static string ConvertMultipleWhitespaceChars (string input)
			{
			return multipleWhitespaceCharsRegex.Replace(input, "&nbsp; ");
			}

		
		
		// Group: StringBuilder Extension Functions
		// __________________________________________________________________________


		/* Function: EncodeEntityCharsAndAppend
		 * Appends the input character to the StringBuilder with <, >, ", and & replaced by their entity encodings.
		 */
		public static void EncodeEntityCharsAndAppend (this StringBuilder output, char input)
			{
			output.Append(EncodeEntityChar(input));
			}


		/* Function: EncodeEntityCharsAndAppend
		 * Appends the contents of the input string to the StringBuilder with <, >, ", and & replaced by their entity 
		 * encodings.
		 */
		public static void EncodeEntityCharsAndAppend (this StringBuilder output, string input)
			{
			output.EncodeEntityCharsAndAppend(input, 0, input.Length);
			}
		
		
		/* Function: EncodeEntityCharsAndAppend
		 * Appends the contents of the input string to the StringBuilder with <, >, ", and & replaced by their entity 
		 * encodings.  Offset and length represent the portion of the input string to convert.
		 */
		public static void EncodeEntityCharsAndAppend (this StringBuilder output, string input, int offset, int length)
			{
			int endOfInput = offset + length;
			
			while (offset < endOfInput)
				{
				int nextEntityChar = input.IndexOfAny(EntityCharLiterals, offset, endOfInput - offset);
				
				if (nextEntityChar == -1)
					{  break;  }
				
				if (nextEntityChar != offset)
					{  output.Append(input, offset, nextEntityChar - offset);  }
					
				output.Append( EncodeEntityChar(input[nextEntityChar]) );
					
				offset = nextEntityChar + 1;
				}
				
			if (offset < endOfInput)
				{  output.Append(input, offset, endOfInput - offset);  }
			}
			
			
		/* Function: EscapeStringCharsAndAppend
		 * Appends the contents of the input string to the StringBuilder with ', ", and \ escaped.
		 */
		public static void EscapeStringCharsAndAppend (this StringBuilder output, string input)
			{
			output.EscapeStringCharsAndAppend(input, 0, input.Length);
			}
		
		
		/* Function: EscapeStringCharsAndAppend
		 * Appends the contents of the input string to the StringBuilder with ', ", and \ escaped.  Offset and length 
		 * represent the portion of the input string to convert.
		 */
		public static void EscapeStringCharsAndAppend (this StringBuilder output, string input, int offset, int length)
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
			
			

		// Group: Variables
		// __________________________________________________________________________

		/* var: EntityCharLiterals
		 * An array of characters that would need to be converted to entity characters in <NDMarkup>.  Useful for String.IndexOfAny(char[]).
		 */
		static char[] EntityCharLiterals = new char[] { '"', '&', '<', '>' };

		/* var: QuoteLiterals
		 * The neutral quote and apostrophe characters suitable for String.IndexOfAny(char[]).
		 */
		static char[] QuoteLiterals = new char[] { '"', '\'' };
		
		/* var: EscapedStringChars
		 * An array of characters that must be escaped in a JavaScript string.
		 */
		static char[] EscapedStringChars = new char[] { '"', '\'', '\\' };
		
		static Regex.NDMarkup.CopyrightAndTrademark copyrightAndTrademarkRegex = new Regex.NDMarkup.CopyrightAndTrademark();
		static Regex.MultipleWhitespaceChars multipleWhitespaceCharsRegex = new Regex.MultipleWhitespaceChars();
		}
	}