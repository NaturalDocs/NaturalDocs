/* 
 * Class: CodeClear.NaturalDocs.Engine.Output.Formats.HTML.Components.FormattedText
 * ____________________________________________________________________________
 * 
 * A reusable class for generating formatted text such as syntax highlighting.
 * 
 * 
 * Multithreading: Not Thread Safe
 * 
 *		This class is only designed to be used by one thread at a time.
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2020 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using CodeClear.NaturalDocs.Engine.Languages;
using CodeClear.NaturalDocs.Engine.Links;
using CodeClear.NaturalDocs.Engine.Symbols;
using CodeClear.NaturalDocs.Engine.Tokenization;
using CodeClear.NaturalDocs.Engine.Topics;


namespace CodeClear.NaturalDocs.Engine.Output.Formats.HTML.Components
	{
	public class FormattedText : Component
		{

		// Group: Types
		// __________________________________________________________________________


		/* Enum: WrappedTitleMode
		 * 
		 * The way in which <AppendWrappedTitle()> and <BuildWrappedTitle()> break long symbols.
		 * 
		 * Code - Wrap symbols based on code separators like . and ::.
		 * File - Wrap symbols based on file separators like / and \.
		 * None - Do not apply wrapping.
		 */
		public enum WrappedTitleMode : byte
			{  Code, File, None  }



		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: FormattedText
		 */
		public FormattedText (Context context) : base (context)
			{
			}


		/* Function: AppendOpeningLinkTag
		 * 
		 * Constructs an <a> tag from the <Context's> <HTMLTopicPage> to the passed <Topic> and appends it to the passed StringBuilder.
		 * 
		 * Requirements:
		 * 
		 *		- The <Context>'s topic page must be set.
		 */
		public void AppendOpeningLinkTag (Topic targetTopic, StringBuilder output, string extraCSSClass = null)
			{
			#if DEBUG
			if (Context.TopicPage == null)
				{  throw new Exception("Tried to call AppendOpeningLinkTag without setting the context's topic page.");  }
			#endif

			// The URL can't be only the hash path because it would use the iframe's location.  We need a relative path back to index.html
			// to append it to.

			Path currentOutputFolder = Context.TopicPage.OutputFile.ParentFolder;
			Path indexFile = Context.Builder.OutputFolder + "/index.html";
			Path pathToIndex = indexFile.MakeRelativeTo(currentOutputFolder);

			Output.Components.HTMLTopicPage targetTopicPage = Context.TopicPage.GetLinkTarget(targetTopic);

			output.Append("<a ");
			
			if (extraCSSClass != null)
				{  output.Append("class=\"" + extraCSSClass + "\" ");  }

			string topicHashPath = Context.Builder.Source_TopicHashPath(targetTopic, targetTopicPage.IncludeClassInTopicHashPaths);

			output.Append("href=\"" + pathToIndex.ToURL() + 
											'#' + targetTopicPage.OutputFileHashPath.EntityEncode() + 
											(topicHashPath != null ? ':' + topicHashPath.EntityEncode() : "") + "\" " +
										"target=\"_top\" " +
										"onmouseover=\"NDContentPage.OnLinkMouseOver(event," + targetTopic.TopicID + ");\" " +
										"onmouseout=\"NDContentPage.OnLinkMouseOut(event);\" " +
									">");
			}


		/* Function: AppendSyntaxHighlightedText
		 * 
		 * Appends the text between the two iterators to the passed StringBuilder as HTML with syntax highlighting applied.  The
		 * syntax highlighting is based on the set <Tokenization.SyntaxHighlightingTypes>.
		 * 
		 * If excludeKeywords is set, it will ignore any <SyntaxHighlightingType.Keyword> sections.  This is useful in situations where
		 * you know the text is an identifier but it may be accidentally marked as a keyword by basic language support highlighting.
		 */
		public void AppendSyntaxHighlightedText (TokenIterator start, TokenIterator end, StringBuilder output, bool excludeKeywords = false)
			{
			TokenIterator iterator = start;

			while (iterator < end)
				{
				if (iterator.FundamentalType == FundamentalType.LineBreak)
					{
					output.Append("<br />");
					iterator.Next();
					}
				else
					{
					TokenIterator startStretch = iterator;
					TokenIterator endStretch = iterator;
					endStretch.Next();

					SyntaxHighlightingType stretchType = startStretch.SyntaxHighlightingType;

					for (;;)
						{
						if (endStretch == end || endStretch.FundamentalType == FundamentalType.LineBreak)
							{  break;  }
						else if (endStretch.SyntaxHighlightingType == stretchType)
							{  endStretch.Next();  }

						// We can include unhighlighted whitespace if there's content of the same type beyond it.  This prevents
						// unnecessary span tags.
						else if (stretchType != SyntaxHighlightingType.Null &&
								   endStretch.SyntaxHighlightingType == SyntaxHighlightingType.Null &&
								   endStretch.FundamentalType == FundamentalType.Whitespace)
							{
							TokenIterator lookahead = endStretch;

							do 
								{  lookahead.Next();  }
							while (lookahead.SyntaxHighlightingType == SyntaxHighlightingType.Null &&
									  lookahead.FundamentalType == FundamentalType.Whitespace &&
									  lookahead < end);

							if (lookahead < end && lookahead.SyntaxHighlightingType == stretchType)
								{
								endStretch = lookahead;
								endStretch.Next();
								}
							else
								{  break;  }
							}

						else
							{  break;  }
						}

					switch (stretchType)
						{
						case SyntaxHighlightingType.Comment:
							output.Append("<span class=\"SHComment\">");
							break;
						case SyntaxHighlightingType.Keyword:
							if (!excludeKeywords)
								{  output.Append("<span class=\"SHKeyword\">");  }
							break;
						case SyntaxHighlightingType.Number:
							output.Append("<span class=\"SHNumber\">");
							break;
						case SyntaxHighlightingType.String:
							output.Append("<span class=\"SHString\">");
							break;
						case SyntaxHighlightingType.PreprocessingDirective:
							output.Append("<span class=\"SHPreprocessingDirective\">");
							break;
						case SyntaxHighlightingType.Metadata:
							output.Append("<span class=\"SHMetadata\">");
							break;
						case SyntaxHighlightingType.Null:
							break;
						default:
							// Add this just in case there's an unaccounted for type in the future.  This prevents the spans from
							// being unbalanced until we handle it.
							output.Append("<span>");
							break;
						}

					output.EntityEncodeAndAppend(startStretch.TextBetween(endStretch));

					if (stretchType != SyntaxHighlightingType.Null &&
						!(stretchType == SyntaxHighlightingType.Keyword && excludeKeywords) )
						{  output.Append("</span>");  }

					iterator = endStretch;
					}
				}
			}


		/* Function: AppendSyntaxHighlightedTextWithTypeLinks
		 * 
		 * Formats the text between the iterators with syntax highlighting and links for any tokens marked with 
		 * <PrototypeParsingType.Type> and <PrototypeParsingType.TypeQualifier>.  Appends the result to the passed StringBuilder.
		 * 
		 * Parameters: 
		 * 
		 *		start - The first token of the text to convert.
		 *		end - The end of the text to convert, which is one token past the last one included.
		 *		output - The StringBuilder to append the output to.
		 *		
		 *		links - A list of <Links> that should  contain any appearing in the code.
		 *		linkTargets - A list of <Topics> that should contain any used as targets in the list of links.
		 *		
		 *		extendTypeSearch - If true, it will search beyond the bounds of the iterators to get the complete type.  This allows you to 
		 *									 format only a portion of the link with this function yet still have the link go to the complete destination.
		 *
		 * Requirements:
		 * 
		 *		- The <Context>'s topic and topic page must be set.
		 */
		public void AppendSyntaxHighlightedTextWithTypeLinks (TokenIterator start, TokenIterator end, StringBuilder output, 
																						  IList<Link> links, IList<Topic> linkTargets,
																						  bool extendTypeSearch = false)
			{
			#if DEBUG
			if (Context.Topic == null)
				{  throw new Exception("Tried to call AppendSyntaxtHighlightedTextWithTypeLinks without setting the context's topic.");  }
			if (Context.TopicPage == null)
				{  throw new Exception("Tried to call AppendSyntaxtHighlightedTextWithTypeLinks without setting the context's topic page.");  }
			if (links == null)
				{  throw new Exception("Tried to call AppendSyntaxtHighlightedTextWithTypeLinks without setting the links variable.");  }
			if (linkTargets == null)
				{  throw new Exception("Tried to call AppendSyntaxtHighlightedTextWithTypeLinks without setting the linkTargets variable.");  }
			#endif

			Language language = EngineInstance.Languages.FromID(Context.Topic.LanguageID);


			// Find each Type/TypeQualifier stretch in the text

			TokenIterator iterator = start;

			while (iterator < end)
				{
				if (iterator.PrototypeParsingType == PrototypeParsingType.Type ||
					 iterator.PrototypeParsingType == PrototypeParsingType.TypeQualifier)
					{
					TokenIterator textStart = iterator;
					TokenIterator textEnd = iterator;

					do
						{  textEnd.Next();  }
					while (textEnd < end &&
								(textEnd.PrototypeParsingType == PrototypeParsingType.Type ||
								 textEnd.PrototypeParsingType == PrototypeParsingType.TypeQualifier) );

					TokenIterator symbolStart = textStart;
					TokenIterator symbolEnd = textEnd;


					// Extend past start and end if the flag is set

					if (extendTypeSearch && symbolStart == start)
						{
						TokenIterator temp = symbolStart;
						temp.Previous();

						while (temp.IsInBounds &&
									(temp.PrototypeParsingType == PrototypeParsingType.Type ||
										temp.PrototypeParsingType == PrototypeParsingType.TypeQualifier))
							{
							symbolStart = temp;
							temp.Previous();
							}
						}

					if (extendTypeSearch && symbolEnd == end)
						{
						while (symbolEnd.IsInBounds &&
									(symbolEnd.PrototypeParsingType == PrototypeParsingType.Type ||
										symbolEnd.PrototypeParsingType == PrototypeParsingType.TypeQualifier))
							{  symbolEnd.Next();  }
						}


					// Built in types don't get links

					if (language.IsBuiltInType(symbolStart, symbolEnd))
						{
						AppendSyntaxHighlightedText(textStart, textEnd, output);
						}

					else
						{
						// Create a link object with the identifying properties needed to look it up in the list of links.

						Link linkStub = new Link();
						linkStub.Type = LinkType.Type;
						linkStub.Symbol = SymbolString.FromPlainText_NoParameters( symbolStart.TextBetween(symbolEnd) );
						linkStub.Context = Context.Topic.PrototypeContext;
						linkStub.ContextID = Context.Topic.PrototypeContextID;
						linkStub.FileID = Context.Topic.FileID;
						linkStub.ClassString = Context.Topic.ClassString;
						linkStub.ClassID = Context.Topic.ClassID;
						linkStub.LanguageID = Context.Topic.LanguageID;


						// Find the actual link so we know if it resolved to anything.

						Link fullLink = null;

						foreach (Link link in links)
							{
							if (link.SameIdentifyingPropertiesAs(linkStub))
								{
								fullLink = link;
								break;
								}
							}

						#if DEBUG
						if (fullLink == null)
							{  throw new Exception("All links in a topic must be in the list passed to AppendSyntaxtHighlightedTextWithTypeLinks.");  }
						#endif


						// If it didn't resolve, we just output the original text.

						if (!fullLink.IsResolved)
							{
							AppendSyntaxHighlightedText(textStart, textEnd, output);
							}

						else
							{
							// If it did resolve, find Topic it resolved to.

							Topic targetTopic = null;

							foreach (Topic linkTarget in linkTargets)
								{
								if (linkTarget.TopicID == fullLink.TargetTopicID)
									{
									targetTopic = linkTarget;
									break;
									}
								}

							#if DEBUG
							if (targetTopic == null)
								{  throw new Exception("All links targets for a topic must be in the list passed to AppendSyntaxtHighlightedTextWithTypeLinks.");  }
							#endif

							AppendOpeningLinkTag(targetTopic, output);
							AppendSyntaxHighlightedText(textStart, textEnd, output);
							output.Append("</a>");
							}
						}

					iterator = textEnd;
					}

				else // not on a type
					{
					TokenIterator startText = iterator;

					do
						{  iterator.Next();  }
					while (iterator < end && 
								iterator.PrototypeParsingType != PrototypeParsingType.Type &&
								iterator.PrototypeParsingType != PrototypeParsingType.TypeQualifier);

					AppendSyntaxHighlightedText(startText, iterator, output);
					}
				}
			}


		/* Function: BuildWrappedTitle
		 * Returns the title as HTML with zero-width spaces added so that long identifiers wrap.  It will also add a span surrounding 
		 * the qualifiers with a "Qualifier" CSS class.
		 */
		public string BuildWrappedTitle (string title, WrappedTitleMode mode)
			{
			if (mode == WrappedTitleMode.None)
				{  return title.ToHTML();  }
			else
				{
				StringBuilder output = new StringBuilder();
				AppendWrappedTitle(title, mode, output);
				return output.ToString();
				}
			}


		/* Function: AppendWrappedTitle
		 * Appends the title to the passed StringBuilder as HTML with zero-width spaces added so that long identifiers wrap.  It will
		 * also add a span surrounding the qualifiers with a "Qualifier" CSS class.
		 */
		public void AppendWrappedTitle (string title, WrappedTitleMode mode, StringBuilder output)
			{
			MatchCollection splitSymbols = null;
			int splitCount = 0;

			if (mode == WrappedTitleMode.Code)
				{  
				splitSymbols = CodeSplitSymbolsRegex.Matches(title);
				splitCount = splitSymbols.Count;
				}
			else if (mode == WrappedTitleMode.File)
				{  
				splitSymbols = FileSplitSymbolsRegex.Matches(title);
				splitCount = splitSymbols.Count;
				}


			// Don't count separators on the end of the string.

			if (splitCount > 0)
				{
				int endOfString = title.Length;

				for (int i = splitCount - 1; i >= 0; i--)
					{
					if (splitSymbols[i].Index + splitSymbols[i].Length == endOfString)
						{
						splitCount--;
						endOfString = splitSymbols[i].Index;
						}
					else
						{  break;  }
					}
				}


			// Build the HTML.

			if (splitCount == 0)
				{
				output.Append(title.ToHTML());
				}
			else
				{
				int appendedSoFar = 0;
				output.Append("<span class=\"Qualifier\">");

				for (int i = 0; i < splitCount; i++)
					{
					int endOfSection = splitSymbols[i].Index + splitSymbols[i].Length;
					string titleSection = title.Substring(appendedSoFar, endOfSection - appendedSoFar);
					output.Append( titleSection.ToHTML() );

					if (i < splitCount - 1)
						{
						// Insert a zero-width space for wrapping.  We have to put the final one outside the closing </span> or 
						// Webkit browsers won't wrap on it.
						output.Append("&#8203;");
						}

					appendedSoFar = endOfSection;
					}

				output.Append("</span>&#8203;");  // zero-width space for wrapping

				output.Append( title.Substring(appendedSoFar).ToHTML() );
				}
			}



		// Group: Static Variables
		// __________________________________________________________________________


		static protected Regex.FileSplitSymbols FileSplitSymbolsRegex = new Regex.FileSplitSymbols();
		static protected Regex.CodeSplitSymbols CodeSplitSymbolsRegex = new Regex.CodeSplitSymbols();

		}
	}

