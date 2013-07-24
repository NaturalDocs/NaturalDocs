/* 
 * Class: GregValure.NaturalDocs.Engine.SearchIndex.TopicEntry
 * ____________________________________________________________________________
 * 
 * A single topic entry in the search index.
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2013 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using GregValure.NaturalDocs.Engine.Symbols;
using GregValure.NaturalDocs.Engine.Topics;


namespace GregValure.NaturalDocs.Engine.SearchIndex
	{
	public class TopicEntry
		{

		// Group: Functions
		// __________________________________________________________________________


		public TopicEntry (Topic topic)
			{
			this.topic = topic;
			var topicType = Engine.Instance.TopicTypes.FromID(topic.TopicTypeID);
			var language = Engine.Instance.Languages.FromID(topic.LanguageID);


			// We don't want to include the parameters in the index.  Multiple functions that differ only by parameter
			// will be treated as one entry.

			string title, ignore;
			ParameterString.SplitFromParameters(topic.Title, out title, out ignore);
			title = title.TrimEnd();


			// Figure out any extra scope text that should appear by comparing the fully resolved symbol to one generated
			// from the title.  We want to use the title as written for indexing rather than having the extra normalization that
			// occurs in SymbolStrings applied.

			string extraScope = null;

			SymbolString titleSymbol = SymbolString.FromPlainText_NoParameters(title);

			string titleSymbolString = titleSymbol.FormatWithSeparator(language.MemberOperator);
			string symbolString = topic.Symbol.FormatWithSeparator(language.MemberOperator);

			if (symbolString.Length > titleSymbolString.Length)
				{
				#if DEBUG
				if (symbolString.IndexOf(titleSymbolString) == -1)
					{  
					throw new Exception("Title symbol string \"" + titleSymbolString + "\" isn't part of symbol string \"" + symbolString + "\" which " +
													"was assumed when creating a search index entry.");  
					}
				#endif

				extraScope = symbolString.Substring(0, symbolString.Length - titleSymbolString.Length);
				}


			displayName = extraScope + title;
			normalizedName = NormalizeName(displayName);
			keywords = new List<string>();

			if (topicType.Flags.Code)
				{  AddKeywordsFromLastSegment( NormalizeSeparators(extraScope + title), '.');  }
			else if (topicType.Flags.File)
				{  AddKeywordsFromLastSegment( NormalizeSeparators(title), '/');  }
			else // documentation
				{  AddKeywordsFromSegment( NormalizeSeparators(title) );  }
			}



		// Group: Static Functions
		// __________________________________________________________________________


		/* Function: NormalizeSeparators
		 * 
		 * Converts all separators to a standardized form that's easier to search.
		 * 
		 * - :: and -> are converted to . regardless of what the language's member operator is.  It also doesn't matter if
		 *   they appear in non-code topics or not.
		 * - \ is converted to / regardless of what the platform's path separator is.  It also doesn't matter if they appear in
		 *   non-file topics or not.
		 */
		static public string NormalizeSeparators (string text)
			{
			text = text.Replace("::", ".");
			text = text.Replace("->", ".");
			text = text.Replace('\\', '/');

			return text;
			}


		/* Function: NormalizeName
		 * 
		 * Converts a display name to a standardized form that's easier to search.
		 * 
		 * - Text is converted to lowercase.
		 * - Whitespace is condensed.
		 * - Spaces that don't separate alphanumeric and underscore characters are removed.
		 * - Separators are normalized with <NormalizeSeparators()>.
		 * - Leading separators are removed.
		 */
		static public string NormalizeName (string text)
			{
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


			text = NormalizeSeparators(text);


			// Remove leading separators.  We don't have to worry about whitespace between them and the rest.

			int i = 0;
			while (i < text.Length && (text[i] == '.' || text[i] == '/'))
				{  i++;  }

			if (i > 0)
				{  text = text.Substring(i);  }

			return text;
			}


		/* Function: AddKeywordsFromLastSegment
		 * Adds keywords to the list from the last segment of text as determined by the passed separator.  Returns
		 * how many were added.  The text must already have normalized separators.
		 */
		protected int AddKeywordsFromLastSegment (string normalizedSeparatorText, char normalizedSeparator)
			{
			#if DEBUG
			if (normalizedSeparatorText != NormalizeSeparators(normalizedSeparatorText))
				{  throw new Exception("The text passed to AddKeywordsFromLastSegment() must have its separators already normalized.");  }
			#endif

			int lastSeparator = normalizedSeparatorText.LastIndexOf(normalizedSeparator, normalizedSeparatorText.Length - 1);

			for (;;)
				{
				if (lastSeparator == -1)
					{  
					return AddKeywordsFromSegment(normalizedSeparatorText, 0);
					}
				else
					{  
					int count = AddKeywordsFromSegment(normalizedSeparatorText, lastSeparator + 1);

					if (count > 0 || lastSeparator == 0)
						{  return count;  }
					else
						{  lastSeparator = normalizedSeparatorText.LastIndexOf(normalizedSeparator, lastSeparator - 1);  }
					}
				}
			}


		/* Function: AddKeywordsFromSegment
		 * Adds keywords to the list from the passed segment of text.  Returns how many were added.  The text must already have
		 * normalized separators.
		 */
		protected int AddKeywordsFromSegment (string normalizedSeparatorText, int startingIndex = 0)
			{
			#if DEBUG
			if (normalizedSeparatorText != NormalizeSeparators(normalizedSeparatorText))
				{  throw new Exception("The text passed to AddKeywordsFromSegment() must have its separators already normalized.");  }
			#endif

			int count = 0;

			for (;;)
				{
				int nextIndex = normalizedSeparatorText.IndexOfAny(NormalizedKeywordSeparators, startingIndex);

				if (nextIndex == -1)
					{  break;  }

				if (nextIndex > startingIndex)
					{
					keywords.Add(normalizedSeparatorText.Substring(startingIndex, nextIndex - startingIndex));
					count++;
					}

				startingIndex = nextIndex + 1;
				}

			if (startingIndex < normalizedSeparatorText.Length)
				{
				keywords.Add(normalizedSeparatorText.Substring(startingIndex));
				count++;
				}

			return count;
			}




		// Group: Properties
		// __________________________________________________________________________


		/* Property: Topic
		 * The <Topics.Topic> associated with this entry.
		 */
		public Topic Topic
			{
			get
				{  return topic;  }
			}

		/* Property: DisplayName
		 * The full name of the entry as it should be displayed on screen, such as "Package::Package::Name".
		 */
		public string DisplayName
			{
			get
				{  return displayName;  }
			}

		/* Propety: NormalizedName
		 * 
		 * The full name of the entry normalized for matching, such as "package.package.name".
		 * 
		 * Normalization:
		 * - All characters are converted to lowercase, regardless of whether the language is case sensitive or not.
		 * - :: and -> are converted to . regardless of what the language's member operator is.
		 * - \ is converted to / regardless of what the platform's path separator is.
		 * - ::, ->, and \ are converted everywhere, not just in code and file topics respectively.
		 */
		public string NormalizedName
			{
			get
				{  return normalizedName;  }
			}

		/* Property: Keywords
		 * A list of the keywords this entry should appear under.  They are case sensitive regardless of whether the language
		 * is or not.
		 */
		public List<string> Keywords
			{
			get
				{  return keywords;  }
			}



		// Group: Variables
		// __________________________________________________________________________

		protected Topic topic;
		protected string displayName;
		protected string normalizedName;
		protected List<string> keywords;



		// Group: Static Variables
		// __________________________________________________________________________
		
		private static char[] NormalizedKeywordSeparators = { ' ', '.', '/' };
		}
	}