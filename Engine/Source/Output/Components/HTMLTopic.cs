/* 
 * Class: CodeClear.NaturalDocs.Engine.Output.Components.HTMLTopic
 * ____________________________________________________________________________
 * 
 * A reusable helper class to build <Topics> and tooltips for <Output.Builders.HTML>.
 * 
 * Why a Separate Class?:
 * 
 *		When each section of the topic is created (title, prototype, body, etc.) there's a lot of context information that needs
 *		to be passed back and forth.  Some of it isn't obvious, such as the body needing access to the <ParsedPrototype>
 *		in order to add types under documented parameters.  These can't be instance variables in <Builders.HTML> because
 *		it needs to support multiple concurrent building threads.  Passing these structures around individually becomes
 *		unwieldy.  If you bundle the context up into a single object to pass around, then you might as well put the functions
 *		in it as well, so here we are.
 * 
 * 
 * Topic: Usage
 *		
 *		- Create a HTMLTopic object.
 *		- Call <Build()> or <BuildToolTip()>.
 *		- The object can be reused on different <Topics> by calling <Build()> again as long as they come from the same 
 *		  <HTMLTopicPage>.
 * 
 * 
 * Threading: Not Thread Safe
 * 
 *		This class is only designed to be used by one thread at a time.  It has an internal state that is used during a call to
 *		<Build()>, and another <Build()> should not be started until it's completed.  Instead each thread should create its 
 *		own object.
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2018 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Text;
using CodeClear.NaturalDocs.Engine.Links;
using CodeClear.NaturalDocs.Engine.Prototypes;
using CodeClear.NaturalDocs.Engine.Tokenization;
using CodeClear.NaturalDocs.Engine.Topics;


namespace CodeClear.NaturalDocs.Engine.Output.Components
	{
	public class HTMLTopic : HTMLComponent
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: HTMLTopic
		 */
		public HTMLTopic (HTMLTopicPage topicPage) : base (topicPage)
			{
			cachedHTMLPrototypeBuilder = null;
			cachedHTMLClassPrototypeBuilder = null;
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
			try
				{

				// Setup

				this.topic = topic;
				this.links = links;
				this.linkTargets = linkTargets;
				this.htmlOutput = output;
				this.embeddedTopics = embeddedTopics;
				this.embeddedTopicIndex = embeddedTopicIndex;
				this.isToolTip = false;


				// Core

				string simpleCommentTypeName = EngineInstance.CommentTypes.FromID(topic.CommentTypeID).SimpleIdentifier;
				string simpleLanguageName = EngineInstance.Languages.FromID(topic.LanguageID).SimpleIdentifier;
				string topicHashPath = HTMLBuilder.Source_TopicHashPath(topic, topicPage.IncludeClassInTopicHashPaths);

				if (topicHashPath != null)
					{  htmlOutput.Append("<a name=\"" + topicHashPath.EntityEncode() + "\"></a>");  }

				htmlOutput.Append(
					"<a name=\"Topic" + topic.TopicID + "\"></a>" +
					"<div class=\"CTopic T" + simpleCommentTypeName + " L" + simpleLanguageName + 
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
							BuildPrototype();
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

			catch (Exception e)
				{
				if (topic != null)
					{
					StringBuilder task = new StringBuilder();

					if (string.IsNullOrEmpty(topic.Title) == false)
						{  task.Append("Building Topic: \"" + topic.Title + "\"");  }
					else
						{
						task.Append("Building Topic ID: " + topic.TopicID + ", Title: ");

						if (topic.Title == null)
							{  task.Append("(null)");  }
						else // empty
							{  task.Append("\"\"");  }
						}

					if (topic.FileID > 0)
						{
						var file = EngineInstance.Files.FromID(topic.FileID);

						if (file == null)
							{  task.Append(" from file ID " + topic.FileID);  }
						else
							{  task.Append(" from file \"" + file.FileName + "\"");  }

						if (topic.CommentLineNumber == topic.CodeLineNumber)
							{  
							if (topic.CommentLineNumber > 0)
								{  task.Append(" line " + topic.CommentLineNumber);  }
							}
						else if (topic.CommentLineNumber > 0)
							{
							if (topic.CodeLineNumber > 0)
								{  task.Append(" lines " + topic.CommentLineNumber + " and " + topic.CodeLineNumber);  }
							else
								{  task.Append(" line " + topic.CommentLineNumber);  }
							}
						else if (topic.CodeLineNumber > 0)
							{  task.Append(" line " + topic.CodeLineNumber);  }
						}

					e.AddNaturalDocsTask(task.ToString());
					}

				throw;
				}

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
			this.isToolTip = true;
			this.links = links;
			this.linkTargets = null;

			// Core

			string simpleCommentTypeName = EngineInstance.CommentTypes.FromID(topic.CommentTypeID).SimpleIdentifier;
			string simpleLanguageName = EngineInstance.Languages.FromID(topic.LanguageID).SimpleIdentifier;

			// No line breaks and indentation because this will be embedded in JavaScript strings.
			htmlOutput.Append("<div class=\"NDToolTip T" + simpleCommentTypeName + " L" + simpleLanguageName + "\">");

				if (topic.Prototype != null)
					{  BuildPrototype();  }

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
			BuildWrappedTitle(topic.Title, topic.CommentTypeID, htmlOutput);
			htmlOutput.Append("</div>");
			}


		/* Function: BuildPrototype
		 */
		protected void BuildPrototype ()
			{
			bool builtPrototype = false;

			if (EngineInstance.CommentTypes.FromID(topic.CommentTypeID).Flags.ClassHierarchy)
				{
				ParsedClassPrototype parsedClassPrototype = topic.ParsedClassPrototype;

				if (parsedClassPrototype != null)
					{
					if (cachedHTMLClassPrototypeBuilder == null)
						{  cachedHTMLClassPrototypeBuilder = new HTMLClassPrototype(topicPage);  }

					if (isToolTip)
						{  cachedHTMLClassPrototypeBuilder.Build(topic, true, null, null, htmlOutput);  }
					else
						{  cachedHTMLClassPrototypeBuilder.Build(topic, false, links, linkTargets, htmlOutput);  }

					builtPrototype = true;
					}
				}

			if (builtPrototype == false)
				{
				if (cachedHTMLPrototypeBuilder == null)
					{  cachedHTMLPrototypeBuilder = new HTMLPrototype(topicPage);  }

				if (isToolTip)
					{  cachedHTMLPrototypeBuilder.Build(topic, null, null, htmlOutput);  }
				else
					{  cachedHTMLPrototypeBuilder.Build(topic, links, linkTargets, htmlOutput);  }
				}
			}


		/* Function: BuildBody
		 */
		protected void BuildBody ()
			{
			htmlOutput.Append("<div class=\"CBody\">");

			NDMarkup.Iterator iterator = new NDMarkup.Iterator(topic.Body);

			bool underParameterHeading = false;
			string parameterListSymbol = null;
			string altParameterListSymbol = null;

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
								preLanguage = EngineInstance.Languages.FromName(preLanguageName);  
								}

							if (preLanguage == null)
								{  preLanguage = EngineInstance.Languages.FromID(topic.LanguageID);  }

							Tokenizer code = new Tokenizer(textCode, tabWidth: EngineInstance.Config.TabWidth);
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

								string topicHashPath = HTMLBuilder.Source_TopicHashPath(embeddedTopics[embeddedTopicIndex], topicPage.IncludeClassInTopicHashPaths);

								if (topicHashPath != null)
									{  htmlOutput.Append("<a name=\"" + topicHashPath.EntityEncode() + "\"></a>");  }

								htmlOutput.Append("<a name=\"Topic" + embeddedTopics[embeddedTopicIndex].TopicID + "\"></a>");

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

								if (symbol.Length > 0)
									{  parameterListSymbol = symbol.ToString();  }
								else
									{  parameterListSymbol = null;  }

								if (firstNonSymbolIndex > 0)
									{  
									symbol.Remove(0, firstNonSymbolIndex);
									altParameterListSymbol = symbol.ToString();
									}
								else
									{  altParameterListSymbol = null;  }

								}
							}
						else // closing tag
							{  
							// See if parameterListSymbol matches any of the prototype parameter names
							if ( (parameterListSymbol != null || altParameterListSymbol != null) && topic.Prototype != null)
								{
								TokenIterator start, end;
								int matchedParameter = -1;

								for (int i = 0; i < topic.ParsedPrototype.NumberOfParameters; i++)	
									{
									topic.ParsedPrototype.GetParameterName(i, out start, out end);

									if ( (parameterListSymbol != null && topic.ParsedPrototype.Tokenizer.EqualsTextBetween(parameterListSymbol, true, start, end)) ||
										 (altParameterListSymbol != null && topic.ParsedPrototype.Tokenizer.EqualsTextBetween(altParameterListSymbol, true, start, end)) )
										{
										matchedParameter = i;
										break;
										}
									}

								// If so, include the type under the entry in the HTML
								if (matchedParameter != -1)
									{
									topic.ParsedPrototype.BuildFullParameterType(matchedParameter, out start, out end);

									if (start < end && 
										// Don't include single symbol types
										 !(end.RawTextIndex - start.RawTextIndex == 1 &&
										   (start.Character == '$' || start.Character == '@' || start.Character == '%')) )
										{
										htmlOutput.Append("<div class=\"CDLParameterType\">");
										BuildTypeLinkedAndSyntaxHighlightedText(start, end);
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
						if (iterator.Property("type") == "standalone")
							{  htmlOutput.Append("<p>");  }

						htmlOutput.Append(iterator.Property("originaltext").ToHTML());

						if (iterator.Property("type") == "standalone")
							{  htmlOutput.Append("</p>");  }
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
			List<LinkInterpretation> linkInterpretations = EngineInstance.Comments.NaturalDocsParser.LinkInterpretations(fullLink.Text,
																					  Comments.Parsers.NaturalDocs.LinkInterpretationFlags.AllowNamedLinks |
																					  Comments.Parsers.NaturalDocs.LinkInterpretationFlags.AllowPluralsAndPossessives |
																					  Comments.Parsers.NaturalDocs.LinkInterpretationFlags.FromOriginalText,
																					  out ignore);

			linkInterpretation = linkInterpretations[ fullLink.TargetInterpretationIndex ];


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

			BuildLinkTag(targetTopic, null, htmlOutput);
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
		protected HTMLPrototype cachedHTMLPrototypeBuilder;

		/* var: cachedHTMLClassPrototypeBuilder
		 * A <HTMLClassPrototype> object for building prototypes, or null if one hasn't been created yet.  Since 
		 * this class can be reused to build multiple <Topics>, and <HTMLClassPrototypeBuilders> can be reused 
		 * to build multiple prototypes, one is stored with the class so it can be reused between runs.
		 */
		protected HTMLClassPrototype cachedHTMLClassPrototypeBuilder;

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

