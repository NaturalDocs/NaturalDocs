/* 
 * Class: GregValure.NaturalDocs.Engine.StringExtensions
 * ____________________________________________________________________________
 * 
 * A static class for all the extension functions for the string type.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2011 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace GregValure.NaturalDocs.Engine
	{
	static public class StringExtensions
		{
		
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
					


		static public Regex.WhitespaceChars WhitespaceCharsRegex = new Regex.WhitespaceChars();
		
		}
	}