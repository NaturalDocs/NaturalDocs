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

// This file is part of Natural Docs, which is Copyright © 2003-2011 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Text;
using System.Text.RegularExpressions;
using GregValure.NaturalDocs.Engine.Collections;
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
			languageParser = null;
			htmlPrototypeBuilder = null;
			isToolTip = false;
			}


		/* Function: Build
		 * Builds the HTML for the <Topic> and appends it to the passed StringBuilder.  If desired, you can add a CSS
		 * class to include in the HTML.  If you're building a series of Topics, pass a usedAnchors <StringSet> to make
		 * sure there's no duplicates generated.  Generated anchors will be added to the set automatically.
		 */
		public void Build (Topic topic, StringBuilder output, string extraClass = null, StringSet usedAnchors = null)
			{

			// Setup

			this.topic = topic;
			this.html = output;
			isToolTip = false;

			language = Engine.Instance.Languages.FromID(topic.LanguageID);

			// Reuse the parser if we can.
			if (languageParser == null || languageParser.Language != language)
				{  languageParser = language.GetParser();  }

			if (topic.Prototype != null)
				{
				parsedPrototype = languageParser.ParsePrototype(topic.Prototype, topic.TopicTypeID, true);

				if (htmlPrototypeBuilder == null)
					{  htmlPrototypeBuilder = new HTMLPrototype(htmlBuilder);  }
				}


			// Core

			string simpleTopicTypeName = Instance.TopicTypes.FromID(topic.TopicTypeID).SimpleIdentifier;
			string simpleLanguageName = language.SimpleIdentifier;

			html.Append(
				"<a name=\"" + Builders.HTML.Source_Anchor(topic, true, usedAnchors) + "\"></a>" +
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


		/* Function: BuildToolTip
		 * Builds the HTML for the <Topic's> tooltip and returns it as a string.  If the topic shoudn't have a tooltip it will
		 * return null.
		 */
		public string BuildToolTip (Topic topic)
			{
			if (topic.Prototype == null && topic.Summary == null)
				{  return null;  }


			// Setup

			this.topic = topic;
			this.html = new StringBuilder();
			isToolTip = true;

			language = Engine.Instance.Languages.FromID(topic.LanguageID);

			// Reuse the parser if we can.
			if (languageParser == null || languageParser.Language != language)
				{  languageParser = language.GetParser();  }

			if (topic.Prototype != null)
				{
				parsedPrototype = languageParser.ParsePrototype(topic.Prototype, topic.TopicTypeID, true);

				if (htmlPrototypeBuilder == null)
					{  htmlPrototypeBuilder = new HTMLPrototype(htmlBuilder);  }
				}


			// Core

			string simpleTopicTypeName = Instance.TopicTypes.FromID(topic.TopicTypeID).SimpleIdentifier;
			string simpleLanguageName = language.SimpleIdentifier;

			// No line breaks and indentation because this will be embedded in JavaScript strings.
			html.Append("<div class=\"NDToolTip T" + simpleTopicTypeName + " L" + simpleLanguageName + "\">");

				if (topic.Prototype != null)
					{  htmlPrototypeBuilder.Build(topic, false, html);  }

				if (topic.Summary != null)
					{  BuildSummary();  }

			html.Append("</div>");

			return html.ToString();
			}


		/* Function: BuildTitle
		 */
		protected void BuildTitle ()
			{
			html.Append("<div class=\"CTitle\">");
			htmlBuilder.BuildWrappedTitle(topic.Title, topic.TopicTypeID, html);
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

						html.Append("<pre>");

						if (preType == "code")
							{
							Languages.Language preLanguage = null;

							if (preLanguageName != null)
								{  preLanguage = Engine.Instance.Languages.FromName(preLanguageName);  }

							if (preLanguage == null)
								{  preLanguage = language;  }

							Tokenizer code = new Tokenizer(textCode);
							preLanguage.GetParser().SyntaxHighlight(code);
							htmlBuilder.BuildSyntaxHighlightedText(code.FirstToken, code.LastToken, html);
							}
						else
							{  
							string htmlCode = textCode.EntityEncode();
							htmlCode = StringExtensions.LineBreakRegex.Replace(htmlCode, "<br />");
							html.Append(htmlCode);
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
							if (parameterListSymbol != null && topic.Prototype != null)
								{
								TokenIterator start, end;
								int matchedParameter = -1;

								for (int i = 0; i < parsedPrototype.NumberOfParameters; i++)	
									{
									parsedPrototype.GetParameterName(i, out start, out end);

									if (parsedPrototype.Tokenizer.EqualsTextBetween(parameterListSymbol, true, start, end))
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

									if (start < end && 
										// Don't include single symbol types
										 (end.RawTextIndex - start.RawTextIndex > 1 ||
										   (start.Character != '$' && start.Character != '@' && start.Character != '%')) )
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
						string linkType = iterator.Property("type");

						if (linkType == "email")
							{  BuildEMailLink(iterator);  }
						else if (linkType == "url")
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


		/* Function: BuildSummary
		 */
		protected void BuildSummary ()
			{
			html.Append("<div class=\"TTSummary\">");

			NDMarkup.Iterator iterator = new NDMarkup.Iterator(topic.Summary);

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

					case NDMarkup.Iterator.ElementType.BoldTag:
					case NDMarkup.Iterator.ElementType.ItalicsTag:
					case NDMarkup.Iterator.ElementType.UnderlineTag:
					case NDMarkup.Iterator.ElementType.LTEntityChar:
					case NDMarkup.Iterator.ElementType.GTEntityChar:
					case NDMarkup.Iterator.ElementType.AmpEntityChar:
					case NDMarkup.Iterator.ElementType.QuoteEntityChar:
						iterator.AppendTo(html);
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
			
			if (!isToolTip)
				{
				html.Append("<a href=\"#\" onclick=\"javascript:location.href='ma\\u0069'+'lto\\u003a'+'");
				html.Append( EMailSegmentForJavaScriptString( address.Substring(0, cutPoint1) ));
				html.Append("'+'");
				html.Append( EMailSegmentForJavaScriptString( address.Substring(cutPoint1, atIndex - cutPoint1) ));
				html.Append("'+'\\u0040'+'");
				html.Append( EMailSegmentForJavaScriptString( address.Substring(atIndex + 1, cutPoint2 - (atIndex + 1)) ));
				html.Append("'+'");
				html.Append( EMailSegmentForJavaScriptString( address.Substring(cutPoint2, address.Length - cutPoint2) ));
				html.Append("';return false;\">");
				}

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

			if (!isToolTip)
				{  html.Append("</a>");  }
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
				html.Append("<a href=\"");
					html.EntityEncodeAndAppend(target);
				html.Append("\" target=\"_top\">");
				}

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

			if (!isToolTip)
				{  html.Append("</a>");  }
			}


		/* Function: BuildNaturalDocsLink
		 */
		protected void BuildNaturalDocsLink (NDMarkup.Iterator iterator)
			{
			// xxx
			// remember to check istooltip
			html.EntityEncodeAndAppend(iterator.Property("originaltext"));
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

		/* var: languageParser
		 * A <Languages.Parser> associated with <language>.
		 */
		protected Languages.Parser languageParser;

		/* var: htmlPrototypeBuilder
		 * A <HTMLPrototype> object for building prototypes, or null if one hasn't been created yet.  Note that you
		 * should always check <Topic.Prototype> for null instead of this, as this may still contain an object from a
		 * previous run.
		 */
		protected HTMLPrototype htmlPrototypeBuilder;

		/* var: isToolTip
		 * Whether we're building a tooltip instead of a full topic.
		 */
		protected bool isToolTip;



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

