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
	public class TopicEntry : Entry
		{

		// Group: Functions
		// __________________________________________________________________________


		public TopicEntry (Topic topic) : base ()
			{
			this.topic = topic;
			var topicType = Engine.Instance.TopicTypes.FromID(topic.TopicTypeID);
			var language = Engine.Instance.Languages.FromID(topic.LanguageID);


			// Get the title without any parameters.  We don't want to include parameters in the index.  Multiple functions that 
			// differ only by parameter will be treated as one entry.

			string title, ignore;
			ParameterString.SplitFromParameters(topic.Title, out title, out ignore);
			title = title.TrimEnd();


			// Figure out the extra scope text that should be added to the title to make it a fully resolved symbol.  We do this by
			// comparing the symbol from the topic to one generated from the title.  We don't just use the symbol to begin with 
			// because we want to show the title as written; there's some normalization that occurs when generating symbols
			// that we want to bypass.

			string extraScope = null;

			SymbolString titleSymbol = SymbolString.FromPlainText_NoParameters(title);

			string titleSymbolString = titleSymbol.FormatWithSeparator(language.MemberOperator);
			string symbolString = topic.Symbol.FormatWithSeparator(language.MemberOperator);

			if (symbolString.Length > titleSymbolString.Length)
				{
				// We have to go by IndexOf rather than EndsWith because operator<string> will have <string> cut off as a parameter. xxx
				int titleIndex = symbolString.IndexOf(titleSymbolString);

				#if DEBUG
				if (titleIndex == -1)
					{  
					throw new Exception("Title symbol string \"" + titleSymbolString + "\" isn't part of symbol string \"" + symbolString + "\" which " +
													"was assumed when creating a search index entry.");  
					}
				#endif

				extraScope = symbolString.Substring(0, titleIndex);
				}


			displayName = extraScope + title;
			endOfDisplayNameQualifiers = EndOfQualifiers(displayName, topicType);

			searchText = Normalize(displayName);
			endOfSearchTextQualifiers = EndOfQualifiers(searchText, topicType);

			keywords = new List<string>();

			if (endOfDisplayNameQualifiers == 0)
				{  AddKeywords(displayName);  }
			else
				{  AddKeywords(displayName.Substring(endOfDisplayNameQualifiers));  }
			}


		/* Function: EndOfQualifiers
		 */
		protected int EndOfQualifiers (string title, TopicTypes.TopicType topicType)
			{
			MatchCollection splitSymbols = null;

			if (topicType.Flags.File == true)
				{  splitSymbols = FileSplitSymbolsRegex.Matches(title);  }
			else if (topicType.Flags.Code == true)
				{  splitSymbols = CodeSplitSymbolsRegex.Matches(title);  }
			// Leave it as null for documentation comments

			if (splitSymbols == null || splitSymbols.Count == 0)
				{  return 0;  }


			// Don't count separators on the end of the string.

			int splitCount = splitSymbols.Count;
			int endOfString = title.Length;

			for (int i = splitCount - 1; i >= 0; i--)
				{
				int afterSplitSymbolIndex = splitSymbols[i].Index + splitSymbols[i].Length;

				if (afterSplitSymbolIndex == endOfString)
					{
					splitCount--;
					endOfString = splitSymbols[i].Index;
					}
				else
					{  
					var nonWhitespaceCharsMatch = NonWhitespaceCharsRegex.Match(title, afterSplitSymbolIndex, endOfString - afterSplitSymbolIndex);

					if (nonWhitespaceCharsMatch.Success)
						{  break;  }
					else
						{
						splitCount--;
						endOfString = splitSymbols[i].Index;
						}
					}
				}


			// Now the result is after the last separator that we didn't ignore.

			if (splitCount == 0)
				{  return 0;  }
			else
				{  return splitSymbols[ splitSymbols.Count - 1 ].Index + splitSymbols[ splitSymbols.Count - 1 ].Length;  }
			}


		/* Function: AddKeywords
		 * Adds keywords to the list from the passed segment of text.  Returns how many were added
		 */
		protected int AddKeywords (string text)
			{
			text = NormalizeSeparatorsOnly(text);

			// Remove the space in "operator <" so they don't appear as two keywords.
			text = SpaceAfterOperatorKeywordRegex.Replace(text, "");

			int count = 0;
			int startingIndex = 0;

			for (;;)
				{
				int nextIndex = text.IndexOfAny(NormalizedKeywordSeparators, startingIndex);

				if (nextIndex == -1)
					{  break;  }

				if (nextIndex > startingIndex)
					{
					keywords.Add(text.Substring(startingIndex, nextIndex - startingIndex));
					count++;
					}

				startingIndex = nextIndex + 1;
				}

			if (startingIndex < text.Length)
				{
				keywords.Add(text.Substring(startingIndex));
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

		/* Property: EndOfDisplayNameQualifiers
		 * The index into <DisplayName> where the qualifiers end and the main symbol begins.  In "Package::Package::Name"
		 * the value will be 18, the index of "N".  If the name doesn't have qualifiers this will be zero.
		 */
		public int EndOfDisplayNameQualifiers
			{
			get
				{  return endOfDisplayNameQualifiers;  }
			}

		/* Propety: SearchText
		 * 
		 * The full name of the entry normalized for search, such as "package.package.name".
		 * 
		 * Normalization:
		 * - All characters are converted to lowercase, regardless of whether the language is case sensitive or not.
		 * - :: and -> are converted to . regardless of what the language's member operator is.
		 * - \ is converted to / regardless of what the platform's path separator is.
		 * - ::, ->, and \ are converted everywhere, not just in code and file topics respectively.
		 */
		public string SearchText
			{
			get
				{  return searchText;  }
			}

		/* Property: EndOfSearchTextQualifiers
		 * The index into <SearchText> where the qualifiers end and the main symbol begins.  In "package.package.name"
		 * the value will be 16, the index of "n".  If the search text doesn't have qualifiers this will be zero.
		 */
		public int EndOfSearchTextQualifiers
			{
			get
				{  return endOfSearchTextQualifiers;  }
			}

		/* Property: Keywords
		 * A list of the keywords this entry should appear under.  They are case sensitive regardless of whether the language
		 * is or not.  Note that keywords are not generated for qualifiers, only the main part of the symbol.  For
		 * "Package.Package.Name" the only keyword will be "Name".
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
		protected int endOfDisplayNameQualifiers;
		protected string searchText;
		protected int endOfSearchTextQualifiers;
		protected List<string> keywords;



		// Group: Static Variables
		// __________________________________________________________________________
		
		private static char[] NormalizedKeywordSeparators = { ' ', '.', '/' };

		static protected Regex.FileSplitSymbols FileSplitSymbolsRegex = new Regex.FileSplitSymbols();
		static protected Regex.CodeSplitSymbols CodeSplitSymbolsRegex = new Regex.CodeSplitSymbols();
		static protected Regex.NonWhitespaceChars NonWhitespaceCharsRegex =  new Regex.NonWhitespaceChars();
		static protected Regex.SpaceAfterOperatorKeyword SpaceAfterOperatorKeywordRegex = new Regex.SpaceAfterOperatorKeyword();

		}
	}