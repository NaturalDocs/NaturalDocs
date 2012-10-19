/* 
 * Class: GregValure.NaturalDocs.Engine.Output.Builders.HTMLTopic
 * ____________________________________________________________________________
 * 
 * A reusable helper class to build <Topics> and tooltips for <Output.Builders.HTML>.
 * 
 * Why a Separate Class?:
 * 
 *		When each section of the topic is created (title, prototype, body, etc.) there's a lot of context information that needs
 *		to be passed back and forth.  Some of it isn't obvious, such as the body needing access to the <ParsedPrototype>
 *		in order to add types under documented parameters.  These can't be instance variables in <Builder.HTML> because
 *		it needs to support multiple concurrent building threads.  Passing these structures around individually becomes
 *		unwieldy.  If you bundle the context up into a single object to pass around, then you might as well put the functions
 *		in it as well, so here we are.
 * 
 * 
 * Topic: Usage
 *		
 *		- Create a HTMLTopic object.
 *		- Call <Build()> or <BuildToolTip()>.
 *		- The object can be reused on different <Topics> by calling <Build()> again.
 * 
 * 
 * Threading: Not Thread Safe
 * 
 *		This class is only designed to be used by one thread at a time.  It has an internal state that is used during a call to
 *		<Build()>, and another <Build()> should not be started until it's completed.  Instead each thread should create its 
 *		own object.
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2012 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using GregValure.NaturalDocs.Engine.Collections;
using GregValure.NaturalDocs.Engine.Links;
using GregValure.NaturalDocs.Engine.Tokenization;
using GregValure.NaturalDocs.Engine.Topics;


namespace GregValure.NaturalDocs.Engine.Output.Builders
	{
	public class HTMLTopic : HTMLElement
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: HTMLTopic
		 */
		public HTMLTopic (Builders.HTML htmlBuilder) : base (htmlBuilder)
			{
			cachedHTMLPrototypeBuilder = null;
			isToolTip = false;
			}


		/* Function: Build
		 * 
		 * Builds the HTML for the <Topic> and appends it to the passed StringBuilder.
		 * 
		 * Parameters:
		 * 
		 *		topic - The <Topic> to build.
		 *		links - A list of <Links> that must contain any links found in the <Topic>.
		 *		linkTargets - A list of <Topics> that must contain any topics used as targets in the links.
		 *		output - The StringBuilder that the output will be appended to.
		 *		embeddedTopics - A list of <Topics> that contains any embedded <Topics> defined by this one.
		 *		embeddedTopicIndex - The index into embeddedTopics to start at.
		 *		extraClass - If specified, this string will be added to the CTopic div as an extra CSS class.
		 */
		public void Build (Topic topic, IList<Link> links, IList<Topic> linkTargets, StringBuilder output, 
										IList<Topic> embeddedTopics, int embeddedTopicIndex = 0, string extraClass = null)
			{

			// Setup

			this.topic = topic;
			this.links = links;
			this.linkTargets = linkTargets;
			this.htmlOutput = output;
			this.embeddedTopics = embeddedTopics;
			this.embeddedTopicIndex = embeddedTopicIndex;
			isToolTip = false;


			// Core

			string simpleTopicTypeName = Instance.TopicTypes.FromID(topic.TopicTypeID).SimpleIdentifier;
			string simpleLanguageName = Instance.Languages.FromID(topic.LanguageID).SimpleIdentifier;

			htmlOutput.Append(
				"<a name=\"" + Builders.HTML.Source_TopicHashPath(topic, true).EntityEncode() + "\"></a>" +
				"<a name=\"Topic" + topic.TopicID + "\"></a>" +
				"<div class=\"CTopic T" + simpleTopicTypeName + " L" + simpleLanguageName + 
											(extraClass == null ? "" : ' ' + extraClass) + "\">" +

					"\r\n ");
					BuildTitle();

					#if SHOW_NDMARKUP
						if (topic.Body != null)
							{
							htmlOutput.Append(
							"\r\n " +
							"<div class=\"CBodyNDMarkup\">" +
								topic.Body.ToHTML() +
							"</div>");
							}
					#endif

					if (topic.Prototype != null)
						{
						htmlOutput.Append("\r\n ");

						if (cachedHTMLPrototypeBuilder == null)
							{  cachedHTMLPrototypeBuilder = new Builders.HTMLPrototype(htmlBuilder);  }

						cachedHTMLPrototypeBuilder.Build(topic, links, linkTargets, htmlOutput);
						}

					if (topic.Body != null)
						{
						htmlOutput.Append("\r\n ");
						BuildBody();
						}

				htmlOutput.Append(
				"\r\n" +
				"</div>"
				);
			}


		/* Function: BuildToolTip
		 * 
		 * Builds the HTML for the <Topic's> tooltip and returns it as a string.  If the topic shoudn't have a tooltip it will
		 * return null.
		 * 
		 * Parameters:
		 * 
		 *		topic - The <Topic> to build the tooltip for.
		 *		links - A list of <Links> that must contain any links found in the <Topic>.
		 */
		public string BuildToolTip (Topic topic, IList<Link> links)
			{
			if (topic.Prototype == null && topic.Summary == null)
				{  return null;  }


			// Setup

			this.topic = topic;
			this.htmlOutput = new StringBuilder();
			isToolTip = true;
			this.links = links;
			this.linkTargets = null;

			// Core

			string simpleTopicTypeName = Instance.TopicTypes.FromID(topic.TopicTypeID).SimpleIdentifier;
			string simpleLanguageName = Instance.Languages.FromID(topic.LanguageID).SimpleIdentifier;

			// No line breaks and indentation because this will be embedded in JavaScript strings.
			htmlOutput.Append("<div class=\"NDToolTip T" + simpleTopicTypeName + " L" + simpleLanguageName + "\">");

				if (topic.Prototype != null)
					{  
					if (cachedHTMLPrototypeBuilder == null)
						{  cachedHTMLPrototypeBuilder = new Builders.HTMLPrototype(htmlBuilder);  }

					cachedHTMLPrototypeBuilder.Build(topic, null, null, htmlOutput);  
					}

				if (topic.Summary != null)
					{  BuildSummary();  }

			htmlOutput.Append("</div>");

			return htmlOutput.ToString();
			}


		/* Function: BuildTitle
		 */
		protected void BuildTitle ()
			{
			htmlOutput.Append("<div class=\"CTitle\">");
			htmlBuilder.BuildWrappedTitle(topic.Title, topic.TopicTypeID, htmlOutput);
			htmlOutput.Append("</div>");
			}


		/* Function: BuildBody
		 */
		protected void BuildBody ()
			{
			htmlOutput.Append("<div class=\"CBody\">");

			NDMarkup.Iterator iterator = new NDMarkup.Iterator(topic.Body);

			bool underParameterHeading = false;
			string parameterListSymbol = null;

			while (iterator.IsInBounds)
				{
				switch (iterator.Type)
					{
					case NDMarkup.Iterator.ElementType.Text:
						if (topic.Body.IndexOf("  ", iterator.RawTextIndex, iterator.Length) == -1)
							{  iterator.AppendTo(htmlOutput);  }
						else
							{  htmlOutput.Append( iterator.String.ConvertMultipleWhitespaceChars() );  }
						break;

					case NDMarkup.Iterator.ElementType.ParagraphTag:
					case NDMarkup.Iterator.ElementType.BulletListTag:
					case NDMarkup.Iterator.ElementType.BulletListItemTag:
					case NDMarkup.Iterator.ElementType.BoldTag:
					case NDMarkup.Iterator.ElementType.ItalicsTag:
					case NDMarkup.Iterator.ElementType.UnderlineTag:
					case NDMarkup.Iterator.ElementType.LTEntityChar:
					case NDMarkup.Iterator.ElementType.GTEntityChar:
					case NDMarkup.Iterator.ElementType.AmpEntityChar:
					case NDMarkup.Iterator.ElementType.QuoteEntityChar:
						iterator.AppendTo(htmlOutput);
						break;

					case NDMarkup.Iterator.ElementType.HeadingTag:
						if (iterator.IsOpeningTag)
							{  
							htmlOutput.Append("<div class=\"CHeading\">");
							underParameterHeading = (iterator.Property("type") == "parameters");
							}
						else
							{  htmlOutput.Append("</div>");  }
						break;

					case NDMarkup.Iterator.ElementType.PreTag:

						string preType = iterator.Property("type");
						string preLanguageName = iterator.Property("language");

						iterator.Next();
						NDMarkup.Iterator startOfCode = iterator;

						// Because we can assume the NDMarkup is valid, we can assume we were on an opening tag and that we will 
						// run into a closing tag before the end of the text.  We can also assume the next pre tag is a closing tag.

						while (iterator.Type != NDMarkup.Iterator.ElementType.PreTag)
							{  iterator.Next();  }

						string ndMarkupCode = topic.Body.Substring(startOfCode.RawTextIndex, iterator.RawTextIndex - startOfCode.RawTextIndex);
						string textCode = NDMarkupCodeToText(ndMarkupCode);

						htmlOutput.Append("<pre>");

						if (preType == "code")
							{
							Languages.Language preLanguage = null;

							if (preLanguageName != null)
								{  
								// This can return null if the language name is unrecognized.
								preLanguage = Engine.Instance.Languages.FromName(preLanguageName);  
								}

							if (preLanguage == null)
								{  preLanguage = Engine.Instance.Languages.FromID(topic.LanguageID);  }

							Tokenizer code = new Tokenizer(textCode);
							preLanguage.SyntaxHighlight(code);
							BuildSyntaxHighlightedText(code.FirstToken, code.LastToken);
							}
						else
							{  
							string htmlCode = textCode.EntityEncode();
							htmlCode = StringExtensions.LineBreakRegex.Replace(htmlCode, "<br />");
							htmlOutput.Append(htmlCode);
							}

						htmlOutput.Append("</pre>");
						break;

					case NDMarkup.Iterator.ElementType.DefinitionListTag:
						if (iterator.IsOpeningTag)
							{  htmlOutput.Append("<table class=\"CDefinitionList\">");  }
						else
							{  htmlOutput.Append("</table>");  }
						break;

					case NDMarkup.Iterator.ElementType.DefinitionListEntryTag:
					case NDMarkup.Iterator.ElementType.DefinitionListSymbolTag:
						if (iterator.IsOpeningTag)
							{  
							htmlOutput.Append("<tr><td class=\"CDLEntry\">");
							parameterListSymbol = null;

							// Create anchors for symbols.  We are assuming there are enough embedded topics for each <ds>
							// tag and that they follow their parent topic in order.
							if (iterator.Type == NDMarkup.Iterator.ElementType.DefinitionListSymbolTag)
								{
								#if DEBUG
									if (embeddedTopics == null || embeddedTopicIndex >= embeddedTopics.Count ||
										 embeddedTopics[embeddedTopicIndex].IsEmbedded == false)
										{  throw new Exception ("There are not enough embedded topics to build the definition list.");  }
								#endif

								htmlOutput.Append(
									"<a name=\"" + Builders.HTML.Source_TopicHashPath(embeddedTopics[embeddedTopicIndex], true).EntityEncode() + "\"></a>" +
									"<a name=\"Topic" + embeddedTopics[embeddedTopicIndex].TopicID + "\"></a>");

								embeddedTopicIndex++;
								}

							// If we're using a Parameters: heading, store the entry symbol in parameterListSymbol
							if (underParameterHeading)
								{
								NDMarkup.Iterator temp = iterator;
								temp.Next();

								StringBuilder symbol = new StringBuilder();

								while (temp.IsInBounds && 
											temp.Type != NDMarkup.Iterator.ElementType.DefinitionListEntryTag &&
											temp.Type != NDMarkup.Iterator.ElementType.DefinitionListSymbolTag)
									{
									if (temp.Type == NDMarkup.Iterator.ElementType.Text)
										{  temp.AppendTo(symbol);  }

									temp.Next();  
									}

								// If the entry name starts with any combination of $, @, or % characters, strip them off.
								int firstNonSymbolIndex = 0;
								while (firstNonSymbolIndex < symbol.Length)
									{
									char charAtIndex = symbol[firstNonSymbolIndex];

									if (charAtIndex != '$' && charAtIndex != '@' && charAtIndex != '%')
										{  break;  }

									firstNonSymbolIndex++;
									}

								if (firstNonSymbolIndex > 0)
									{  symbol.Remove(0, firstNonSymbolIndex);  }

								if (symbol.Length > 0)
									{  parameterListSymbol = symbol.ToString();  }
								}
							}
						else
							{  
							// See if parameterListSymbol matches any of the prototype parameter names
							if (parameterListSymbol != null && topic.Prototype != null)
								{
								TokenIterator start, end;
								int matchedParameter = -1;

								for (int i = 0; i < topic.ParsedPrototype.NumberOfParameters; i++)	
									{
									topic.ParsedPrototype.GetParameterName(i, out start, out end);

									if (topic.ParsedPrototype.Tokenizer.EqualsTextBetween(parameterListSymbol, true, start, end))
										{
										matchedParameter = i;
										break;
										}
									}

								// If so, include the type under the entry in the HTML
								if (matchedParameter != -1)
									{
									TokenIterator extensionStart, extensionEnd;
									topic.ParsedPrototype.GetFullParameterType(matchedParameter, out start, out end, 
																													out extensionStart, out extensionEnd);

									if (start < end && 
										// Don't include single symbol types
										 (end.RawTextIndex - start.RawTextIndex > 1 ||
										   (start.Character != '$' && start.Character != '@' && start.Character != '%')) )
										{
										htmlOutput.Append("<div class=\"CDLEntryType\">");
									
										BuildTypeLinkedAndSyntaxHighlightedText(start, end);
										BuildTypeLinkedAndSyntaxHighlightedText(extensionStart, extensionEnd);

										htmlOutput.Append("</div>");
										}
									}
								}

							htmlOutput.Append("</td>");
							}
						break;

					case NDMarkup.Iterator.ElementType.DefinitionListDefinitionTag:
						if (iterator.IsOpeningTag)
							{  htmlOutput.Append("<td class=\"CDLDefinition\">");  }
						else
							{  htmlOutput.Append("</td></tr>");  }
						break;

					case NDMarkup.Iterator.ElementType.LinkTag:
						string linkType = iterator.Property("type");

						if (linkType == "email")
							{  BuildEMailLink(iterator);  }
						else if (linkType == "url")
							{  BuildURLLink(iterator);  }
						else // type == "naturaldocs"
							{  BuildNaturalDocsLink(iterator);  }

						break;

					case NDMarkup.Iterator.ElementType.ImageTag: // xxx
						htmlOutput.Append( "<i>" + iterator.String.ToHTML() + "</i>" );
						break;
					}

				iterator.Next();
				}

			htmlOutput.Append("</div>");
			}


		/* Function: BuildSummary
		 */
		protected void BuildSummary ()
			{
			htmlOutput.Append("<div class=\"TTSummary\">");

			NDMarkup.Iterator iterator = new NDMarkup.Iterator(topic.Summary);

			while (iterator.IsInBounds)
				{
				switch (iterator.Type)
					{
					case NDMarkup.Iterator.ElementType.Text:
						if (topic.Body.IndexOf("  ", iterator.RawTextIndex, iterator.Length) == -1)
							{  iterator.AppendTo(htmlOutput);  }
						else
							{  htmlOutput.Append( iterator.String.ConvertMultipleWhitespaceChars() );  }
						break;

					case NDMarkup.Iterator.ElementType.BoldTag:
					case NDMarkup.Iterator.ElementType.ItalicsTag:
					case NDMarkup.Iterator.ElementType.UnderlineTag:
					case NDMarkup.Iterator.ElementType.LTEntityChar:
					case NDMarkup.Iterator.ElementType.GTEntityChar:
					case NDMarkup.Iterator.ElementType.AmpEntityChar:
					case NDMarkup.Iterator.ElementType.QuoteEntityChar:
						iterator.AppendTo(htmlOutput);
						break;

					case NDMarkup.Iterator.ElementType.LinkTag:
						string linkType = iterator.Property("type");

						if (linkType == "email")
							{  BuildEMailLink(iterator);  }
						else if (linkType == "url")
							{  BuildURLLink(iterator);  }
						else // type == "naturaldocs"
							{  BuildNaturalDocsLink(iterator);  }

						break;
					}

				iterator.Next();
				}

			htmlOutput.Append("</div>");
			}


		/* Function: BuildEMailLink
		 */
		protected void BuildEMailLink (NDMarkup.Iterator iterator)
			{
			string address = iterator.Property("target");
			int atIndex = address.IndexOf('@');
			int cutPoint1 = atIndex / 2;
			int cutPoint2 = (atIndex+1) + ((address.Length - (atIndex+1)) / 2);
			
			if (!isToolTip)
				{
				htmlOutput.Append("<a href=\"#\" onclick=\"javascript:location.href='ma\\u0069'+'lto\\u003a'+'");
				htmlOutput.Append( EMailSegmentForJavaScriptString( address.Substring(0, cutPoint1) ));
				htmlOutput.Append("'+'");
				htmlOutput.Append( EMailSegmentForJavaScriptString( address.Substring(cutPoint1, atIndex - cutPoint1) ));
				htmlOutput.Append("'+'\\u0040'+'");
				htmlOutput.Append( EMailSegmentForJavaScriptString( address.Substring(atIndex + 1, cutPoint2 - (atIndex + 1)) ));
				htmlOutput.Append("'+'");
				htmlOutput.Append( EMailSegmentForJavaScriptString( address.Substring(cutPoint2, address.Length - cutPoint2) ));
				htmlOutput.Append("';return false;\">");
				}

			string text = iterator.Property("text");

			if (text != null)
				{  htmlOutput.EntityEncodeAndAppend(text);  }
			else
				{
				htmlOutput.Append( EMailSegmentForHTML( address.Substring(0, cutPoint1) ));
				htmlOutput.Append("<span style=\"display: none\">[xxx]</span>");
				htmlOutput.Append( EMailSegmentForHTML( address.Substring(cutPoint1, atIndex - cutPoint1) ));
				htmlOutput.Append("<span>&#64;</span>");
				htmlOutput.Append( EMailSegmentForHTML( address.Substring(atIndex + 1, cutPoint2 - (atIndex + 1)) ));
				htmlOutput.Append("<span style=\"display: none\">[xxx]</span>");
				htmlOutput.Append( EMailSegmentForHTML( address.Substring(cutPoint2, address.Length - cutPoint2) ));
				}

			if (!isToolTip)
				{  htmlOutput.Append("</a>");  }
			}

		/* Function: EMailSegmentForJavaScriptString
		 */
		protected string EMailSegmentForJavaScriptString (string segment)
			{
			segment = segment.StringEscape();
			segment = segment.Replace(".", "\\u002e");
			return segment;
			}

		/* Function: EMailSegmentForHTML
		 */
		protected string EMailSegmentForHTML (string segment)
			{
			segment = segment.EntityEncode();
			segment = segment.Replace(".", "&#46;");
			return segment;
			}

		/* Function: BuildURLLink
		 */
		protected void BuildURLLink (NDMarkup.Iterator iterator)
			{
			string target = iterator.Property("target");

			if (!isToolTip)
				{
				htmlOutput.Append("<a href=\"");
					htmlOutput.EntityEncodeAndAppend(target);
				htmlOutput.Append("\" target=\"_top\">");
				}

			string text = iterator.Property("text");

			if (text != null)
				{  htmlOutput.EntityEncodeAndAppend(text);  }
			else
				{
				int startIndex = 0;
				int breakIndex;

				// Skip the protocol and any following slashes since we don't want a break after every slash in http:// or
				// file:///.

				int endOfProtocolIndex = target.IndexOf(':');

				if (endOfProtocolIndex != -1)
					{
					do
						{  endOfProtocolIndex++;  }
					while (endOfProtocolIndex < target.Length && target[endOfProtocolIndex] == '/');

					htmlOutput.EntityEncodeAndAppend( target.Substring(0, endOfProtocolIndex) );
					htmlOutput.Append("&#8203;");  // Zero width space
					startIndex = endOfProtocolIndex;
					}

				for (;;)
					{
					breakIndex = target.IndexOfAny(BreakURLCharacters, startIndex);

					if (breakIndex == -1)
						{
						if (target.Length - startIndex > MaxUnbrokenURLCharacters)
							{  breakIndex = startIndex + MaxUnbrokenURLCharacters;  }
						else
							{  break;  }
						}
					else if (breakIndex - startIndex > MaxUnbrokenURLCharacters)
						{  breakIndex = startIndex + MaxUnbrokenURLCharacters;  }

					htmlOutput.EntityEncodeAndAppend( target.Substring(startIndex, breakIndex - startIndex) );
					htmlOutput.Append("&#8203;");  // Zero width space
					htmlOutput.EntityEncodeAndAppend(target[breakIndex]);

					startIndex = breakIndex + 1;
					}

				htmlOutput.EntityEncodeAndAppend( target.Substring(startIndex) );
				}

			if (!isToolTip)
				{  htmlOutput.Append("</a>");  }
			}


		/* Function: BuildNaturalDocsLink
		 */
		protected void BuildNaturalDocsLink (NDMarkup.Iterator iterator)
			{
			// Create a link object with the identifying properties needed to look it up in the list of links.

			Link linkStub = new Link();
			linkStub.Type = LinkType.NaturalDocs;
			linkStub.Text = iterator.Property("originaltext");
			linkStub.Context = topic.BodyContext;
			linkStub.ContextID = topic.BodyContextID;
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
				{  throw new Exception("All links in a topic must be in the list passed to HTMLTopic.");  }
			#endif


			// If it didn't resolve, we just output the original text and we're done.

			if (!fullLink.IsResolved)
				{
				htmlOutput.EntityEncodeAndAppend(iterator.Property("originaltext"));
				return;
				}


			// If it did resolve, find the interpretation that was used.  If it was a named link it would affect the link text.

			LinkInterpretation linkInterpretation = null;

			string ignore;
			List<LinkInterpretation> linkInterpretations = Instance.Comments.NaturalDocsParser.LinkInterpretations(fullLink.Text,
																					  Comments.Parsers.NaturalDocs.LinkInterpretationFlags.AllowNamedLinks |
																					  Comments.Parsers.NaturalDocs.LinkInterpretationFlags.AllowPluralsAndPossessives |
																					  Comments.Parsers.NaturalDocs.LinkInterpretationFlags.FromOriginalText,
																					  out ignore);

			linkInterpretation = linkInterpretations[ CodeDB.Manager.GetInterpretationIndex(fullLink.TargetScore) ];


			// If it's a tooltip, that's all we need.  We don't need to find the Topic because we're not creating an actual link;
			// you can't click on tooltips.  We just needed to know what the text should be.

			if (isToolTip)
				{
				htmlOutput.EntityEncodeAndAppend(linkInterpretation.Text);
				return;
				}


			// If it's not a tooltip we need to find Topic it resolved to.

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
				{  throw new Exception("All links targets for a topic must be in the list passed to HTMLTopic.");  }
			#endif


			// Now build the actual link.  It can't just be the hash path because it would use the iframe's location, so we also
			// need a relative path back to index.html.

			Path currentOutputFolder = htmlBuilder.Source_OutputFile(topic.FileID).ParentFolder;
			Path indexFile = htmlBuilder.OutputFolder + "/index.html";
			Path pathToIndex = currentOutputFolder.MakeRelative(indexFile);

			htmlOutput.Append("<a href=\"" + pathToIndex.ToURL() + 
														'#' + htmlBuilder.Source_OutputFileHashPath(targetTopic.FileID) + 
														':' + Builders.HTML.Source_TopicHashPath(targetTopic, true) + "\" " +
											"target=\"_top\" " +
											"onmouseover=\"NDContentPage.OnLinkMouseOver(event," + targetTopic.TopicID + ");\" " +
											"onmouseout=\"NDContentPage.OnLinkMouseOut(event);\" " +
										">");

			htmlOutput.EntityEncodeAndAppend(linkInterpretation.Text);
			htmlOutput.Append("</a>");
			}


		/* Function: NDMarkupCodeToText
		 * Converts code sections in <NDMarkup> back to plain text, decoding entity chars and converting line breaks
		 * to \n.
		 */
		protected string NDMarkupCodeToText (string input)
			{
			string output = input.Replace("<br>", "\n");
			output = output.EntityDecode();
			return output;
			}



		// Group: Variables
		// __________________________________________________________________________


		/* var: cachedHTMLPrototypeBuilder
		 * A <HTMLPrototype> object for building prototypes, or null if one hasn't been created yet.  Since this
		 * class can be reused to build multiple <Topics>, and <HTMLPrototypeBuilders> can be reused to build
		 * multiple prototypes, one is stored with the class so it can be reused between runs.
		 */
		protected Builders.HTMLPrototype cachedHTMLPrototypeBuilder;

		/* var: isToolTip
		 * Whether we're building a tooltip instead of a full topic.
		 */
		protected bool isToolTip;

		/* var: embeddedTopics
		 * A list of <Topics> that contains any that are embedded in the one we are building.
		 */
		protected IList<Topic> embeddedTopics;

		/* var: embeddedTopicIndex
		 * The entry in <embeddedTopics> where the next one resides.
		 */
		protected int embeddedTopicIndex;



		// Group: Static Variables
		// __________________________________________________________________________


		/* var: breakURLCharacters
		 * An array of characters that cause an inline URL to wrap.
		 */
		static protected char[] BreakURLCharacters = { '.', '/', '#', '?', '&' };

		/* var: maxUnbrokenURLCharacters
		 * The longest stretch between <breakURLCharacters> that can occur unbroken in an inline URL.  Formatting attempts
		 * to break on those characters as it looks cleaner, but this limit forces it to happen if they don't occur.
		 */
		protected const int MaxUnbrokenURLCharacters = 35;

		}
	}

