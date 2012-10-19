/* 
 * Class: GregValure.NaturalDocs.Engine.Output.Builders.HTMLElement
 * ____________________________________________________________________________
 * 
 * A base class for helper classes that build certain HTML elements.
 * 
 * Some elements require a lot of context information, so we store it all in an object to make it easier to manage.  They
 * also share some functionality so we can put that in the base class as well.
 * 
 * 
 * Threading: Not Thread Safe
 * 
 *		This class is only designed to be used by one thread at a time.
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2012 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Text;
using GregValure.NaturalDocs.Engine.Languages;
using GregValure.NaturalDocs.Engine.Links;
using GregValure.NaturalDocs.Engine.Symbols;
using GregValure.NaturalDocs.Engine.Tokenization;
using GregValure.NaturalDocs.Engine.Topics;


namespace GregValure.NaturalDocs.Engine.Output.Builders
	{
	public class HTMLElement
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: HTMLElement
		 */
		public HTMLElement (Builders.HTML htmlBuilder)
			{
			this.htmlBuilder = htmlBuilder;

			htmlOutput = null;
			topic = null;
			links = null;
			linkTargets = null;
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
						linkStub.Symbol = SymbolString.FromPlainText_ParenthesesAlreadyRemoved(
																symbolStart.Tokenizer.TextBetween(symbolStart, symbolEnd) );
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
							{  throw new Exception("All links in a topic must be in the list passed to HTMLElement.");  }
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
								{  throw new Exception("All links targets for a topic must be in the list passed to HTMLElement.");  }
							#endif


							// Now build the actual link.  It can't just be the hash path because it would use the iframe's location, so we 
							// also need a relative path back to index.html.

							Path currentOutputFolder = htmlBuilder.Source_OutputFile(topic.FileID).ParentFolder;
							Path indexFile = htmlBuilder.OutputFolder + "/index.html";
							Path pathToIndex = currentOutputFolder.MakeRelative(indexFile);

							output.Append("<a href=\"" + pathToIndex.ToURL() + 
																		'#' + htmlBuilder.Source_OutputFileHashPath(targetTopic.FileID) + 
																		':' + Builders.HTML.Source_TopicHashPath(targetTopic, true) + "\" " +
															"target=\"_top\" " +
															"onmouseover=\"NDContentPage.OnLinkMouseOver(event," + targetTopic.TopicID + ");\" " +
															"onmouseout=\"NDContentPage.OnLinkMouseOut(event);\" " +
														">");

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




		// Group: Variables
		// __________________________________________________________________________


		/* var: htmlBuilder
		 * The parent <Output.Builders.HTML> object.
		 */
		protected Builders.HTML htmlBuilder;

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

		}
	}

