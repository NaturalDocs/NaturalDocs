/* 
 * Class: CodeClear.NaturalDocs.Engine.Output.HTML.SearchIndex.Entries.Topic
 * ____________________________________________________________________________
 * 
 * A single topic entry in the search index.
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2021 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using CodeClear.NaturalDocs.Engine.Symbols;


namespace CodeClear.NaturalDocs.Engine.Output.HTML.SearchIndex.Entries
	{
	public class Topic : Entry
		{

		// Group: Functions
		// __________________________________________________________________________


		public Topic (Engine.Topics.Topic topic, SearchIndex.Manager searchIndex) : base ()
			{
			wrappedTopic = topic;

			var commentType = searchIndex.EngineInstance.CommentTypes.FromID(topic.CommentTypeID);
			var language = searchIndex.EngineInstance.Languages.FromID(topic.LanguageID);

			// It's possible for language to be null if we're creating this to handle an OnDeleteTopic event for a language that
			// was deleted.
			var memberOperator = (language != null ? language.MemberOperator : ".");


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

			string titleSymbolString = titleSymbol.FormatWithSeparator(memberOperator);
			string symbolString = topic.Symbol.FormatWithSeparator(memberOperator);

			if (symbolString.Length > titleSymbolString.Length)
				{
				// We have to go by LastIndexOf rather than EndsWith because "operator<string>" will have "<string>" cut off as a parameter.
				// We also have to go by LastIndexOf instead of IndexOf so constructors don't get cut off (Package.Class.Class).
				int titleIndex = symbolString.LastIndexOf(titleSymbolString);

				#if DEBUG
				if (titleIndex == -1)
					{  
					throw new Exception("Title symbol string \"" + titleSymbolString + "\" isn't part of symbol string \"" + symbolString + "\" which " +
													"was assumed when creating a search index entry.");  
					}
				#endif

				extraScope = symbolString.Substring(0, titleIndex);
				}


			// Remove the space in "operator <".  This prevents them from appearing as two keywords, and also makes sure "operator <" and
			// "operator<" are always displayed consistently, which will be important for sorting.

			title = SpaceAfterOperatorKeywordRegex.Replace(title, "");


			displayName = (extraScope == null ? title : extraScope + title);
			searchText = Normalize(displayName);

			if (commentType.IsFile)
				{
				endOfDisplayNameQualifiers = FindEndOfQualifiers(displayName, FileSplitSymbolsRegex.Matches(displayName));
				endOfSearchTextQualifiers = FindEndOfQualifiers(searchText, FileSplitSymbolsRegex.Matches(searchText));
				}
			else if (commentType.IsCode)
				{
				endOfDisplayNameQualifiers = FindEndOfQualifiers(displayName, CodeSplitSymbolsRegex.Matches(displayName));
				endOfSearchTextQualifiers = FindEndOfQualifiers(searchText, CodeSplitSymbolsRegex.Matches(searchText));
				}
			else // documentation topic
				{
				if (extraScope == null)
					{
					endOfDisplayNameQualifiers = 0;
					endOfSearchTextQualifiers = 0;
					}
				else
					{
					endOfDisplayNameQualifiers = extraScope.Length;

					// Don't need +1 because only leading separators are removed.  The trailing separator will still be there.
					endOfSearchTextQualifiers = Normalize(extraScope).Length;
					}
				}

			keywords = new List<string>();

			if (endOfDisplayNameQualifiers == 0)
				{  AddKeywords(displayName, isDocumentation: commentType.IsDocumentation);  }
			else
				{  AddKeywords(displayName.Substring(endOfDisplayNameQualifiers), isDocumentation: commentType.IsDocumentation);  }
			}



		// Group: Support Functions
		// __________________________________________________________________________


		/* Function: FindEndOfQualifiers
		 */
		protected int FindEndOfQualifiers (string title, MatchCollection splitSymbols)
			{
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
		protected int AddKeywords (string text, bool isDocumentation)
			{
			text = NormalizeSeparators(text);

			int count = 0;
			int startingIndex = 0;

			for (;;)
				{
				int nextIndex = text.IndexOfAny(NormalizedKeywordSeparators, startingIndex);

				if (nextIndex == -1)
					{  break;  }

				if (nextIndex > startingIndex)
					{
					string keyword = text.Substring(startingIndex, nextIndex - startingIndex);

					if (isDocumentation)
						{
						keyword = LeadingPunctuationRegex.Replace(keyword, "");
						keyword = TrailingPunctuationRegex.Replace(keyword, "");
						}

					if (keyword.Length > 0)
						{
						keywords.Add(keyword);
						count++;
						}
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


		/* Property: WrappedTopic
		 * The <Engine.Topics.Topic> associated with this entry.
		 */
		public Engine.Topics.Topic WrappedTopic
			{
			get
				{  return wrappedTopic;  }
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
		 * The full name of the entry normalized for search, such as "package.package.name".  See <Entry.Normalize()> for the
		 * details.
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

		protected Engine.Topics.Topic wrappedTopic;
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
		static protected Regex.LeadingPunctuation LeadingPunctuationRegex = new Regex.LeadingPunctuation();
		static protected Regex.TrailingPunctuation TrailingPunctuationRegex = new Regex.TrailingPunctuation();

		}
	}