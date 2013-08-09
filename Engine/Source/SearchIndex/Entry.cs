/* 
 * Class: GregValure.NaturalDocs.Engine.SearchIndex.Entry
 * ____________________________________________________________________________
 * 
 * A base class for all entries in the search index.
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2013 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace GregValure.NaturalDocs.Engine.SearchIndex
	{
	public class Entry
		{

		// Group: Functions
		// __________________________________________________________________________


		public Entry ()
			{
			}


		/* Function: Normalize
		 * 
		 * Converts a string to a standardized form that's easier to search.
		 * 
		 * - Text is converted to lowercase.
		 * - Whitespace is condensed.
		 * - Spaces that don't separate alphanumeric and underscore characters are removed.
		 * - Separators are normalized with <NormalizeSeparatorsOnly()>.
		 * - Leading separators are removed.
		 */
		static public string Normalize (string text)
			{
			// DEPENDENCY: If this changes NDSearch.GetSearchText() must be updated to match.

			text = text.ToLower();
			text = text.CondenseWhitespace().Trim();


			// Remove spaces unless between two alphanumeric/underscore characters

			int spaceIndex = text.IndexOf(' ');

			while (spaceIndex != -1)
				{
				char charBefore = text[spaceIndex - 1];
				char charAfter = text[spaceIndex + 1];

				bool alphanumBefore = ( (charBefore >= 'a' && charBefore <= 'z') ||
												 (charBefore >= 'A' && charBefore <= 'Z') ||
												 (charBefore >= '0' && charBefore <= '9') ||
												 charBefore == '_' );
				bool alphanumAfter = ( (charAfter >= 'a' && charAfter <= 'z') ||
												 (charAfter >= 'A' && charAfter <= 'Z') ||
												 (charAfter >= '0' && charAfter <= '9') ||
												 charAfter == '_' );

				if (alphanumBefore && alphanumAfter)
					{  spaceIndex = text.IndexOf(' ', spaceIndex + 1);  }
				else
					{
					text = text.Substring(0, spaceIndex) + text.Substring(spaceIndex + 1);
					spaceIndex = text.IndexOf(' ', spaceIndex);
					}
				}


			text = NormalizeSeparatorsOnly(text);


			// Remove leading separators.  We don't have to worry about whitespace between them and the rest.

			int i = 0;
			while (i < text.Length && (text[i] == '.' || text[i] == '/'))
				{  i++;  }

			if (i > 0)
				{  text = text.Substring(i);  }

			return text;
			}


		/* Function: NormalizeSeparatorsOnly
		 * 
		 * Converts all separators to a standardized form that's easier to search.  If you called <Normalize()> there is no need
		 * to call this as well.
		 * 
		 * - :: and -> are converted to . regardless of what the language's member operator is.  It also doesn't matter if
		 *   they appear in non-code topics or not.
		 * - \ is converted to / regardless of what the platform's path separator is.  It also doesn't matter if they appear in
		 *   non-file topics or not.
		 */
		static public string NormalizeSeparatorsOnly (string text)
			{
			text = text.Replace("::", ".");
			text = text.Replace("->", ".");
			text = text.Replace('\\', '/');

			return text;
			}

		}
	}