/* 
 * Class: CodeClear.NaturalDocs.Engine.Output.HTML.Components.Tooltip
 * ____________________________________________________________________________
 * 
 * A reusable class for building HTML tooltips.
 * 
 * 
 * Threading: Not Thread Safe
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
using CodeClear.NaturalDocs.Engine.Links;
using CodeClear.NaturalDocs.Engine.Prototypes;


namespace CodeClear.NaturalDocs.Engine.Output.HTML.Components
	{
	public class Tooltip : HTML.Components.FormattedText
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: Tooltip
		 */
		public Tooltip (Context context) : base (context)
			{
			// These are created on first use since they may not be needed
			prototypeBuilder = null;
			classPrototypeBuilder = null;
			}


		/* Function: BuildToolTip
		 * 
		 * Builds the HTML for the topic's tooltip and returns it as a string.  If the topic shoudn't have a tooltip it will return null.
		 * 
		 * Parameters:
		 * 
		 *		topic - The topic to build the tooltip for.
		 *		context - The context of the page the tooltip is being built for.  The topic will automatically replace the context's topic
		 *					  so you can just pass the context of the page, if any.
		 *		links - A list of <Links> that must contain any links found in the topic.
		 */
		public string BuildToolTip (Topics.Topic topic, Context context, IList<Link> links)
			{
			if (topic.Prototype == null && topic.Summary == null)
				{  return null;  }
	
			StringBuilder output = new StringBuilder();
			AppendToolTip(topic, context, links, output);
			return output.ToString();
			}


		/* Function: AppendToolTip
		 * 
		 * Builds the HTML for the topic's tooltip and appends it to the passed StringBuilder.  If the topic shoudn't have a tooltip it will
		 * return false.
		 * 
		 * Parameters:
		 * 
		 *		topic - The topic to build the tooltip for.
		 *		context - The context of the page the tooltip is being built for.  The topic will automatically replace the context's topic
		 *					  so you can just pass the context of the page, if any.
		 *		links - A list of <Links> that must contain any links found in the topic.
		 */
		public bool AppendToolTip (Topics.Topic topic, Context context, IList<Link> links, StringBuilder output)
			{
			if (topic.Prototype == null && topic.Summary == null)
				{  return false;  }

			this.context = context;
			this.context.Topic = topic;
			this.links = links;

			string simpleCommentTypeName = EngineInstance.CommentTypes.FromID(topic.CommentTypeID).SimpleIdentifier;
			string simpleLanguageName = EngineInstance.Languages.FromID(topic.LanguageID).SimpleIdentifier;

			// No line breaks and indentation because this will be embedded in JavaScript strings.
			output.Append("<div class=\"NDToolTip T" + simpleCommentTypeName + " L" + simpleLanguageName + "\">");

				if (topic.Prototype != null)
					{  AppendPrototype(output);  }

				if (topic.Summary != null)
					{  AppendSummary(output);  }

			output.Append("</div>");

			return true;
			}



		// Group: Support Functions
		// __________________________________________________________________________


		/* Function: AppendPrototype
		 * Covers both prototypes and class prototypes.
		 */
		protected void AppendPrototype (StringBuilder output)
			{
			var commentType = EngineInstance.CommentTypes.FromID(context.Topic.CommentTypeID);
			bool builtPrototype = false;

			if (commentType.Flags.ClassHierarchy)
				{
				ParsedClassPrototype parsedClassPrototype = context.Topic.ParsedClassPrototype;

				if (parsedClassPrototype != null)
					{
					if (classPrototypeBuilder == null)
						{  classPrototypeBuilder = new HTML.Components.ClassPrototype(context);  }

					classPrototypeBuilder.AppendClassPrototype(parsedClassPrototype, context, true, output);

					builtPrototype = true;
					}
				}

			if (builtPrototype == false)
				{
				if (prototypeBuilder == null)
					{  prototypeBuilder = new HTML.Components.Prototype(context);  }

				prototypeBuilder.AppendPrototype(context.Topic.ParsedPrototype, context, output);
				}
			}


		/* Function: AppendSummary
		 */
		protected void AppendSummary (StringBuilder output)
			{
			output.Append("<div class=\"TTSummary\">");

			string summary = context.Topic.Summary;
			NDMarkup.Iterator iterator = new NDMarkup.Iterator(summary);

			while (iterator.IsInBounds)
				{
				switch (iterator.Type)
					{
					case NDMarkup.Iterator.ElementType.Text:
						// Preserve multiple whitespace chars, but skip the extra processing if there aren't any
						if (summary.IndexOf("  ", iterator.RawTextIndex, iterator.Length) != -1)
							{  output.Append( iterator.String.ConvertMultipleWhitespaceChars() );  }
						else
							{  iterator.AppendTo(output);  }
						break;

					case NDMarkup.Iterator.ElementType.BoldTag:
					case NDMarkup.Iterator.ElementType.ItalicsTag:
					case NDMarkup.Iterator.ElementType.UnderlineTag:
					case NDMarkup.Iterator.ElementType.LTEntityChar:
					case NDMarkup.Iterator.ElementType.GTEntityChar:
					case NDMarkup.Iterator.ElementType.AmpEntityChar:
					case NDMarkup.Iterator.ElementType.QuoteEntityChar:
						// These the NDMarkup directly matches the HTML tags
						iterator.AppendTo(output);
						break;

					case NDMarkup.Iterator.ElementType.LinkTag:
						string linkType = iterator.Property("type");

						if (linkType == "email")
							{  AppendEMailLink(iterator, output);  }
						else if (linkType == "url")
							{  AppendURLLink(iterator, output);  }
						else // type == "naturaldocs"
							{  AppendNaturalDocsLink(iterator, output);  }

						break;
					}

				iterator.Next();
				}

			output.Append("</div>");
			}


		/* Function: AppendEMailLink
		 */
		protected void AppendEMailLink (NDMarkup.Iterator iterator, StringBuilder output)
			{
			string text = iterator.Property("text");

			if (text != null)
				{  output.EntityEncodeAndAppend(text);  }
			else
				{
				string address = iterator.Property("target");
				int atIndex = address.IndexOf('@');
				int cutPoint1 = atIndex / 2;
				int cutPoint2 = (atIndex+1) + ((address.Length - (atIndex+1)) / 2);

				output.Append( EMailSegmentForHTML( address.Substring(0, cutPoint1) ));
				output.Append("<span style=\"display: none\">[xxx]</span>");
				output.Append( EMailSegmentForHTML( address.Substring(cutPoint1, atIndex - cutPoint1) ));
				output.Append("<span>&#64;</span>");
				output.Append( EMailSegmentForHTML( address.Substring(atIndex + 1, cutPoint2 - (atIndex + 1)) ));
				output.Append("<span style=\"display: none\">[xxx]</span>");
				output.Append( EMailSegmentForHTML( address.Substring(cutPoint2, address.Length - cutPoint2) ));
				}
			}

		/* Function: EMailSegmentForHTML
		 */
		protected string EMailSegmentForHTML (string segment)
			{
			segment = segment.EntityEncode();
			segment = segment.Replace(".", "&#46;");
			return segment;
			}

		/* Function: AppendURLLink
		 */
		protected void AppendURLLink (NDMarkup.Iterator iterator, StringBuilder output)
			{
			string text = iterator.Property("text");

			if (text != null)
				{  output.EntityEncodeAndAppend(text);  }
			else
				{
				string target = iterator.Property("target");

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

					output.EntityEncodeAndAppend( target.Substring(0, endOfProtocolIndex) );
					output.Append("&#8203;");  // Zero width space
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

					output.EntityEncodeAndAppend( target.Substring(startIndex, breakIndex - startIndex) );
					output.Append("&#8203;");  // Zero width space
					output.EntityEncodeAndAppend(target[breakIndex]);

					startIndex = breakIndex + 1;
					}

				output.EntityEncodeAndAppend( target.Substring(startIndex) );
				}
			}


		/* Function: AppendNaturalDocsLink
		 */
		protected void AppendNaturalDocsLink (NDMarkup.Iterator iterator, StringBuilder output)
			{
			// Create a link object with the identifying properties needed to look it up in the list of links.

			Link linkStub = new Link();
			linkStub.Type = LinkType.NaturalDocs;
			linkStub.Text = iterator.Property("originaltext");
			linkStub.Context = context.Topic.BodyContext;
			linkStub.ContextID = context.Topic.BodyContextID;
			linkStub.FileID = context.Topic.FileID;
			linkStub.ClassString = context.Topic.ClassString;
			linkStub.ClassID = context.Topic.ClassID;
			linkStub.LanguageID = context.Topic.LanguageID;


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
				{  throw new Exception("All links in a topic must be in the list passed to Tooltip.");  }
			#endif


			// If it didn't resolve, we just output the original text and we're done.

			if (!fullLink.IsResolved)
				{
				output.EntityEncodeAndAppend(iterator.Property("originaltext"));
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


			// Since it's a tooltip, that's all we need.  We don't need to find the Topic because we're not creating an actual link;
			// you can't click on tooltips.  We just needed to know what the text should be.

			output.EntityEncodeAndAppend(linkInterpretation.Text);
			}



		// Group: Variables
		// __________________________________________________________________________


		/* var: links
		 * A list of <Links> that contain any which will appear in the prototype, or null if links aren't needed.
		 */
		protected IList<Link> links;

		/* var: prototypeBuilder
		 * An object for building prototypes, or null if one hasn't been created yet.  Since this
		 * class can be reused to build multiple topics, and these objects can be reused to build
		 * multiple prototypes, one is stored with the class so it can be reused between runs.
		 */
		protected HTML.Components.Prototype prototypeBuilder;

		/* var: classPrototypeBuilder
		 * An object for building class prototypes, or null if one hasn't been created yet.  Since this
		 * class can be reused to build multiple topics, and these objects can be reused to build
		 * multiple prototypes, one is stored with the class so it can be reused between runs.
		 */
		protected HTML.Components.ClassPrototype classPrototypeBuilder;



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

