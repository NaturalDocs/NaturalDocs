/* 
 * Class: GregValure.NaturalDocs.Engine.Output.Builders.HTMLTopic
 * ____________________________________________________________________________
 * 
 * A reusable helper class to build <Topics> for <Output.Builders.HTML>.
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
 *		- Call <Build()>.
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

// This file is part of Natural Docs, which is Copyright © 2003-2011 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Text;
using System.Text.RegularExpressions;
using GregValure.NaturalDocs.Engine.Tokenization;


namespace GregValure.NaturalDocs.Engine.Output.Builders
	{
	public class HTMLTopic
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: HTMLTopic
		 */
		public HTMLTopic (Builders.HTML htmlBuilder)
			{
			this.htmlBuilder = htmlBuilder;

			html = null;
			topic = null;
			parsedPrototype = null;
			language = null;
			htmlPrototypeBuilder = null;
			}


		/* Function: Build
		 * Builds the HTML for the <Topic> and appends it to the passed StringBuilder.  If desired, you can add a CSS
		 * class to include in the HTML.
		 */
		public void Build (Topic topic, StringBuilder output, string extraClass = null)
			{

			// Setup

			this.topic = topic;
			this.html = output;

			language = Engine.Instance.Languages.FromID(topic.LanguageID);

			if (topic.Prototype != null)
				{
				parsedPrototype = language.ParsePrototype(topic.Prototype, topic.TopicTypeID, true);

				if (htmlPrototypeBuilder == null)
					{  htmlPrototypeBuilder = new HTMLPrototype(htmlBuilder);  }
				}


			// Core

			string simpleTopicTypeName = Instance.TopicTypes.FromID(topic.TopicTypeID).SimpleIdentifier;
			string simpleLanguageName = language.SimpleIdentifier;

			html.Append(
				"<div class=\"CTopic T" + simpleTopicTypeName + " L" + simpleLanguageName + 
											(extraClass == null ? "" : ' ' + extraClass) + "\">" +

					"\r\n ");
					BuildTitle();

					#if SHOW_NDMARKUP
						if (topic.Body != null)
							{
							html.Append(
							"\r\n " +
							"<div class=\"CBodyNDMarkup\">" +
								topic.Body.ToHTML() +
							"</div>");
							}
					#endif

					if (topic.Prototype != null)
						{
						html.Append("\r\n ");
						htmlPrototypeBuilder.Build(topic, true, html);
						}

					if (topic.Body != null)
						{
						html.Append("\r\n ");
						BuildBody();
						}

				html.Append(
				"\r\n" +
				"</div>"
				);
			}


		/* Function: BuildTitle
		 */
		protected void BuildTitle ()
			{
			MatchCollection splitSymbols = null;

			if (htmlBuilder.IsFileTopicType(topic.TopicTypeID))
				{  splitSymbols = FileSplitSymbolsRegex.Matches(topic.Title);  }
			else if (htmlBuilder.IsCodeTopicType(topic.TopicTypeID))
				{  splitSymbols = CodeSplitSymbolsRegex.Matches(topic.Title);  }

			int splitCount = (splitSymbols == null ? 0 : splitSymbols.Count);


			// Don't count separators on the end of the string.

			if (splitCount > 0)
				{
				int endOfString = topic.Title.Length;

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

			html.Append("<div class=\"CTitle\">");

			if (splitCount == 0)
				{
				html.Append( topic.Title.ToHTML() );
				}
			else
				{
				int appendedSoFar = 0;
				html.Append("<span class=\"qualifier\">");

				for (int i = 0; i < splitCount; i++)
					{
					int endOfSection = splitSymbols[i].Index + splitSymbols[i].Length;
					string titleSection = topic.Title.Substring(appendedSoFar, endOfSection - appendedSoFar);
					html.Append( titleSection.ToHTML() );

					if (i < splitCount - 1)
						{
						// Insert a zero-width space for wrapping.  We have to put the final one outside the closing </span> or 
						// Webkit browsers won't wrap on it.
						html.Append("&#8203;");
						}

					appendedSoFar = endOfSection;
					}

				html.Append("</span>&#8203;");  // zero-width space for wrapping

				html.Append( topic.Title.Substring(appendedSoFar).ToHTML() );
				}

			html.Append("</div>");
			}


		/* Function: BuildBody
		 */
		protected void BuildBody ()
			{
			html.Append("<div class=\"CBody\">");

			NDMarkup.Iterator iterator = new NDMarkup.Iterator(topic.Body);

			bool underParameterHeading = false;
			string parameterListSymbol = null;

			while (iterator.IsInBounds)
				{
				switch (iterator.Type)
					{
					case NDMarkup.Iterator.ElementType.Text:
						if (topic.Body.IndexOf("  ", iterator.RawTextIndex, iterator.Length) == -1)
							{  iterator.AppendTo(html);  }
						else
							{  html.Append( iterator.String.ConvertMultipleWhitespaceChars() );  }
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
						iterator.AppendTo(html);
						break;

					case NDMarkup.Iterator.ElementType.HeadingTag:
						if (iterator.IsOpeningTag)
							{  
							html.Append("<div class=\"CHeading\">");
							underParameterHeading = (iterator.Property("type") == "parameters");
							}
						else
							{  html.Append("</div>");  }
						break;

					case NDMarkup.Iterator.ElementType.PreTag:
						// Because we can assume the NDMarkup is valid, we can assume it's an opening tag and that we will run
						// into a closing tag.

						html.Append("<pre>");

						for (;;)
							{
							iterator.Next();

							if (iterator.Type == NDMarkup.Iterator.ElementType.PreTag)
								{  break;  }
							else
								{  
								// Includes PreLineBreakTags
								iterator.AppendTo(html);  
								}
							}

						html.Append("</pre>");
						break;

					case NDMarkup.Iterator.ElementType.DefinitionListTag:
						if (iterator.IsOpeningTag)
							{  html.Append("<table class=\"CDefinitionList\">");  }
						else
							{  html.Append("</table>");  }
						break;

					case NDMarkup.Iterator.ElementType.DefinitionListEntryTag:
						if (iterator.IsOpeningTag)
							{  
							html.Append("<tr><td class=\"CDLEntry\">");
							parameterListSymbol = null;

							if (underParameterHeading)
								{
								NDMarkup.Iterator temp = iterator;
								temp.Next();

								StringBuilder symbol = new StringBuilder();

								while (temp.IsInBounds && temp.Type != NDMarkup.Iterator.ElementType.DefinitionListEntryTag)
									{
									if (temp.Type == NDMarkup.Iterator.ElementType.Text)
										{  temp.AppendTo(symbol);  }

									temp.Next();  
									}

								if (symbol.Length > 0)
									{  parameterListSymbol = symbol.ToString();  }
								}
							}
						else
							{  
							if (parameterListSymbol != null && topic.Prototype != null)
								{
								TokenIterator start, end;
								int matchedParameter = -1;

								for (int i = 0; i < parsedPrototype.NumberOfParameters; i++)	
									{
									parsedPrototype.GetParameterName(i, out start, out end);

									if (parsedPrototype.Tokenizer.ContainsTextBetween(parameterListSymbol, true, start, end)) //xxx
										{
										matchedParameter = i;
										break;
										}
									}

								if (matchedParameter != -1)
									{
									TokenIterator extensionStart, extensionEnd;
									parsedPrototype.GetFullParameterType(matchedParameter, out start, out end, 
																										  out extensionStart, out extensionEnd);

									if (start < end || extensionStart < extensionEnd)
										{
										html.Append("<div class=\"CDLEntryType\">");
									
										htmlBuilder.BuildTypeLinkedAndSyntaxHighlightedText(start, end, html);
										htmlBuilder.BuildTypeLinkedAndSyntaxHighlightedText(extensionStart, extensionEnd, html);

										html.Append("</div>");
										}
									}
								}

							html.Append("</td>");
							}
						break;

					case NDMarkup.Iterator.ElementType.DefinitionListDefinitionTag:
						if (iterator.IsOpeningTag)
							{  html.Append("<td class=\"CDLDefinition\">");  }
						else
							{  html.Append("</td></tr>");  }
						break;

					case NDMarkup.Iterator.ElementType.LinkTag:
						string type = iterator.Property("type");

						if (type == "email")
							{  BuildEMailLink(iterator);  }
						else if (type == "url")
							{  BuildURLLink(iterator);  }
						else // type == "naturaldocs"
							{  BuildNaturalDocsLink(iterator);  }

						break;

					case NDMarkup.Iterator.ElementType.ImageTag: // xxx
						html.Append( "<i>" + iterator.String.ToHTML() + "</i>" );
						break;
					}

				iterator.Next();
				}

			html.Append("</div>");
			}


		/* Function: BuildEMailLink
		 */
		protected void BuildEMailLink (NDMarkup.Iterator iterator)
			{
			string address = iterator.Property("target");
			int atIndex = address.IndexOf('@');
			int cutPoint1 = atIndex / 2;
			int cutPoint2 = (atIndex+1) + ((address.Length - (atIndex+1)) / 2);
			
			html.Append("<a href=\"#\" onclick=\"javascript:location.href='ma\\u0069'+'lto\\u003a'+'");
			html.Append( EMailSegmentForJavaScriptString( address.Substring(0, cutPoint1) ));
			html.Append("'+'");
			html.Append( EMailSegmentForJavaScriptString( address.Substring(cutPoint1, atIndex - cutPoint1) ));
			html.Append("'+'\\u0040'+'");
			html.Append( EMailSegmentForJavaScriptString( address.Substring(atIndex + 1, cutPoint2 - (atIndex + 1)) ));
			html.Append("'+'");
			html.Append( EMailSegmentForJavaScriptString( address.Substring(cutPoint2, address.Length - cutPoint2) ));
			html.Append("';return false;\">");

			string text = iterator.Property("text");

			if (text != null)
				{  html.EntityEncodeAndAppend(text);  }
			else
				{
				html.Append( EMailSegmentForHTML( address.Substring(0, cutPoint1) ));
				html.Append("<span style=\"display: none\">[xxx]</span>");
				html.Append( EMailSegmentForHTML( address.Substring(cutPoint1, atIndex - cutPoint1) ));
				html.Append("<span>&#64;</span>");
				html.Append( EMailSegmentForHTML( address.Substring(atIndex + 1, cutPoint2 - (atIndex + 1)) ));
				html.Append("<span style=\"display: none\">[xxx]</span>");
				html.Append( EMailSegmentForHTML( address.Substring(cutPoint2, address.Length - cutPoint2) ));
				}

			html.Append("</a>");
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
			html.Append("<a href=\"");
				html.EntityEncodeAndAppend(target);
			html.Append("\" target=\"_top\">");

			string text = iterator.Property("text");

			if (text != null)
				{  html.EntityEncodeAndAppend(text);  }
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

					html.EntityEncodeAndAppend( target.Substring(0, endOfProtocolIndex) );
					html.Append("&#8203;");  // Zero width space
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

					html.EntityEncodeAndAppend( target.Substring(startIndex, breakIndex - startIndex) );
					html.Append("&#8203;");  // Zero width space
					html.EntityEncodeAndAppend(target[breakIndex]);

					startIndex = breakIndex + 1;
					}

				html.EntityEncodeAndAppend( target.Substring(startIndex) );
				}

			html.Append("</a>");
			}


		/* Function: BuildNaturalDocsLink
		 */
		protected void BuildNaturalDocsLink (NDMarkup.Iterator iterator)
			{
			// xxx
			html.EntityEncodeAndAppend(iterator.Property("originaltext"));
			}



		// Group: Variables
		// __________________________________________________________________________


		/* var: htmlBuilder
		 * The parent <Output.Builders.HTML> object.
		 */
		protected Builders.HTML htmlBuilder;

		/* var: html
		 * The StringBuilder we want to append the prototype to.
		 */
		protected StringBuilder html;

		/* var: topic
		 * The <Topic> that contains the prototype we're building.
		 */
		protected Topic topic;

		/* var: parsedPrototype
		 * The prototype as a <ParsedPrototype> object.
		 */
		protected ParsedPrototype parsedPrototype;

		/* var: language
		 * The <Languages.Language> of the prototype.
		 */
		protected Languages.Language language;

		/* var: htmlPrototypeBuilder
		 * A <HTMLPrototype> object for building prototypes, or null if one hasn't been created yet.  Note that you
		 * should always check <Topic.Prototype> for null instead of this, as this may still contain an object from a
		 * previous run.
		 */
		protected HTMLPrototype htmlPrototypeBuilder;



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

		static protected Regex.Output.HTML.FileSplitSymbols FileSplitSymbolsRegex = new Regex.Output.HTML.FileSplitSymbols();
		static protected Regex.Output.HTML.CodeSplitSymbols CodeSplitSymbolsRegex = new Regex.Output.HTML.CodeSplitSymbols();

		}
	}

