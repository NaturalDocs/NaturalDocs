/* 
 * Class: GregValure.NaturalDocs.Engine.Output.Components.HTMLComponent
 * ____________________________________________________________________________
 * 
 * A base class for HTML components.
 * 
 * Some components require a lot of context information, so we store it all in an object to make it easier to manage.
 * They also share some functionality so we can put that in the base class as well.
 * 
 * 
 * Threading: Not Thread Safe
 * 
 *		This class is only designed to be used by one thread at a time.
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2013 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using GregValure.NaturalDocs.Engine.Languages;
using GregValure.NaturalDocs.Engine.Links;
using GregValure.NaturalDocs.Engine.Symbols;
using GregValure.NaturalDocs.Engine.Tokenization;
using GregValure.NaturalDocs.Engine.Topics;


namespace GregValure.NaturalDocs.Engine.Output.Components
	{
	public class HTMLComponent
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: HTMLComponent
		 */
		public HTMLComponent (HTMLTopicPage topicPage)
			{
			this.topicPage = topicPage;

			htmlOutput = null;
			topic = null;
			links = null;
			linkTargets = null;
			}


		/* Function: BuildLinkTag
		 * Constructs an <a> tag from the current <HTMLTopicPage> to the passed <Topic>.  It will NOT include the link text or the
		 * closing tag, only the opening <a> tag.  If output is null it will be appended to <htmlOutput>.
		 */
		protected void BuildLinkTag (Topic targetTopic, string cssClass = null, StringBuilder output = null)
			{
			#if DEBUG
			if (output == null && htmlOutput == null)
				{  throw new Exception("Tried to call BuildLinkTag() without setting the output parameter or the htmlOutput variable.");  }
			#endif

			if (output == null)
				{  output = htmlOutput;  }


			// The link can't be only the hash path because it would use the iframe's location.  We need a relative path back to index.html
			// to append it to.

			Path currentOutputFolder = topicPage.OutputFile.ParentFolder;
			Path indexFile = HTMLBuilder.OutputFolder + "/index.html";
			Path pathToIndex = currentOutputFolder.MakeRelative(indexFile);

			HTMLTopicPage targetTopicPage = topicPage.GetLinkTarget(targetTopic);

			output.Append("<a ");
			
			if (cssClass != null)
				{  output.Append("class=\"" + cssClass + "\" ");  }

			output.Append("href=\"" + pathToIndex.ToURL() + 
											'#' + targetTopicPage.OutputFileHashPath + 
											':' + Builders.HTML.Source_TopicHashPath(targetTopic, targetTopicPage.IncludeClassInTopicHashPaths) + "\" " +
										"target=\"_top\" " +
										"onmouseover=\"NDContentPage.OnLinkMouseOver(event," + targetTopic.TopicID + ");\" " +
										"onmouseout=\"NDContentPage.OnLinkMouseOut(event);\" " +
									">");
			}


		/* Function: BuildSyntaxHighlightedText
		 * Formats the text between the two iterators with syntax highlighting.  If output is null it will be appended to
		 * <htmlOutput>.
		 */
		protected void BuildSyntaxHighlightedText (TokenIterator iterator, TokenIterator end, StringBuilder output = null)
			{
			if (output == null)
				{  output = htmlOutput;  }

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
							output.Append("<span class=\"SHKeyword\">");
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

					output.EntityEncodeAndAppend(iterator.Tokenizer.TextBetween(startStretch, endStretch));

					if (stretchType != SyntaxHighlightingType.Null)
						{  output.Append("</span>");  }

					iterator = endStretch;
					}
				}
			}


		/* Function: BuildTypeLinkedAndSyntaxHighlightedText
		 * 
		 * Formats the text between the iterators with syntax highlighting and links for any tokens marked with 
		 * <PrototypeParsingType.Type> and <PrototypeParsingType.TypeQualifier>.
		 * 
		 * Parameters: 
		 *		start - The first token of the text to convert.
		 *		end - The end of the text to convert, which is one token past the last one included.
		 *		extendTypeSearch - If true, it will search beyond the bounds of the iterators to get the complete type.  
		 *												This allows you to format only a portion of the link with this function yet still have 
		 *												the link go to the complete destination.
		 *		output - The StringBuilder to append the output to.  If null, it will used <htmlOutput>.
		 *		
		 * Variables Required:
		 *		- <topic>, <links>, and <linkTargets> must be set.
		 *		- <htmlOutput> must be set if the output parameter is not set.
		 */
		protected void BuildTypeLinkedAndSyntaxHighlightedText (TokenIterator start, TokenIterator end,
																														 bool extendTypeSearch = false,
																														 StringBuilder output = null)
			{
			#if DEBUG
			if (topic == null)
				{  throw new Exception("Tried to call BuildTypeLinkedAndSyntaxHighlightedText() without setting the topic variable.");  }
			if (links == null)
				{  throw new Exception("Tried to call BuildTypeLinkedAndSyntaxHighlightedText() without setting the links variable.");  }
			if (linkTargets == null)
				{  throw new Exception("Tried to call BuildTypeLinkedAndSyntaxHighlightedText() without setting the linkTargets variable.");  }
			if (output == null && htmlOutput == null)
				{  throw new Exception("Tried to call BuildTypeLinkedAndSyntaxHighlightedText() without setting the output parameter or the htmlOutput variable.");  }
			#endif

			if (output == null)
				{  output = htmlOutput;  }

			Language language = Engine.Instance.Languages.FromID(topic.LanguageID);


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
						BuildSyntaxHighlightedText(textStart, textEnd, output);
						}

					else
						{
						// Create a link object with the identifying properties needed to look it up in the list of links.

						Link linkStub = new Link();
						linkStub.Type = LinkType.Type;
						linkStub.Symbol = SymbolString.FromPlainText_NoParameters( symbolStart.Tokenizer.TextBetween(symbolStart, symbolEnd) );
						linkStub.Context = topic.PrototypeContext;
						linkStub.ContextID = topic.PrototypeContextID;
						linkStub.FileID = topic.FileID;
						linkStub.ClassString = topic.ClassString;
						linkStub.ClassID = topic.ClassID;
						linkStub.LanguageID = topic.LanguageID;


						// Find the actual link so we know if it resolved to anything.

						Link fullLink = null;

						foreach (Link link in links)
							{
							if (link.SameIDPropertiesAs(linkStub))
								{
								fullLink = link;
								break;
								}
							}

						#if DEBUG
						if (fullLink == null)
							{  throw new Exception("All links in a topic must be in the list passed to HTMLComponent.");  }
						#endif


						// If it didn't resolve, we just output the original text.

						if (!fullLink.IsResolved)
							{
							BuildSyntaxHighlightedText(textStart, textEnd, output);
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
								{  throw new Exception("All links targets for a topic must be in the list passed to HTMLComponent.");  }
							#endif

							BuildLinkTag(targetTopic, null, output);
							BuildSyntaxHighlightedText(textStart, textEnd, output);
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

					BuildSyntaxHighlightedText(startText, iterator, output);
					}
				}
			}



		// Group: Static Functions
		// __________________________________________________________________________


		/* Function: BuildWrappedTitle
		 * Builds a title with zero-width spaces added so that long identifiers wrap.  Will also add a span surrounding the qualifiers
		 * with a "Qualifier" CSS class.  The HTML will be appended to the StringBuilder, but you must provide your own surrounding
		 * div if required.
		 */
		static public void BuildWrappedTitle (string title, int topicTypeID, StringBuilder output)
			{
			MatchCollection splitSymbols = null;
			var topicType = Engine.Instance.TopicTypes.FromID(topicTypeID);

			if (topicType.Flags.File == true)
				{  splitSymbols = FileSplitSymbolsRegex.Matches(title);  }
			else if (topicType.Flags.Code == true)
				{  splitSymbols = CodeSplitSymbolsRegex.Matches(title);  }

			int splitCount = (splitSymbols == null ? 0 : splitSymbols.Count);


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


		/* Function: BuildWrappedTitle
		 * Builds a title with zero-width spaces added so that long identifiers wrap.  Will also add a span surrounding the qualifiers
		 * with a "Qualifier" CSS class.  The HTML will be returned as a string, but you must provide your own surrounding div if
		 * required.  If the string will be directly appended to a StringBuilder, it is more efficient to use the other form.
		 */
		static public string BuildWrappedTitle (string title, int topicTypeID)
			{
			StringBuilder temp = new StringBuilder();
			BuildWrappedTitle(title, topicTypeID, temp);
			return temp.ToString();
			}



		// Group: Properties
		// __________________________________________________________________________


		/* Property: TopicPage
		 * The <HTMLTopicPage> this component appears in.
		 */
		public HTMLTopicPage TopicPage
			{
			get
				{  return topicPage;  }
			}


		/* Property: HTMLBuilder
		 * The <Builders.HTML> associated with this component.
		 */
		public Builders.HTML HTMLBuilder
			{
			get
				{  return topicPage.HTMLBuilder;  }
			}



		// Group: Variables
		// __________________________________________________________________________


		/* var: topicPage
		 * The <HTMLTopicPage> that this object appears in.
		 */
		protected HTMLTopicPage topicPage;

		/* var: htmlOutput
		 * The StringBuilder we want to append the output to.
		 */
		protected StringBuilder htmlOutput;

		/* var: topic
		 * The <Topic> for whatever element we're building.
		 */
		protected Topic topic;

		/* var: links
		 * A list of <Links> that will contain any links needed by <topic>.
		 */
		protected IList<Link> links;

		/* var: linkTargets
		 * A list of <Topics> that will contain any topics used as targets in <links>.
		 */
		protected IList<Topic> linkTargets;



		// Group: Static Variables
		// __________________________________________________________________________


		static protected Regex.Output.HTML.FileSplitSymbols FileSplitSymbolsRegex = new Regex.Output.HTML.FileSplitSymbols();

		static protected Regex.Output.HTML.CodeSplitSymbols CodeSplitSymbolsRegex = new Regex.Output.HTML.CodeSplitSymbols();

		}
	}

