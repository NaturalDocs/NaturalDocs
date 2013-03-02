/* 
 * Class: GregValure.NaturalDocs.Engine.Output.Components.HTMLClassPrototype
 * ____________________________________________________________________________
 * 
 * A reusable helper class to build class prototypes for <Output.Builders.HTML>.
 * 
 * Topic: Usage
 *		
 *		- Create a HTMLPrototype object.
 *		- Call <Build()>.
 *		- The object can be reused on different prototypes by calling <Build()> again as long as they come from the same
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

// This file is part of Natural Docs, which is Copyright © 2003-2012 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Text;
using GregValure.NaturalDocs.Engine.Links;
using GregValure.NaturalDocs.Engine.Tokenization;
using GregValure.NaturalDocs.Engine.Topics;


namespace GregValure.NaturalDocs.Engine.Output.Components
	{
	public class HTMLClassPrototype : HTMLComponent
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: HTMLClassPrototype
		 */
		public HTMLClassPrototype (HTMLTopicPage topicPage) : base (topicPage)
			{
			parsedPrototype = null;
			language = null;
			isToolTip = false;
			addLinks = false;
			}


		/* Function: Build
		 * 
		 * Builds the HTML for the <Topic's> prototype and returns it as a string.  If the string is going to be appended to
		 * a StringBuilder, it is more efficient to use the other function.
		 * 
		 * In order to have type links, links must be specified and contain any links that appear in the prototype.
		 * linkTargets must also be specified and contain the target <Topics> of all links.  If you do not need type
		 * links, set both to null.
		 */
		public string Build (Topic topic, bool isToolTip, IList<Link> links, IList<Topic> linkTargets)
			{
			StringBuilder output = new StringBuilder();
			Build(topic, isToolTip, links, linkTargets, output);
			return output.ToString();
			}


		/* Function: Build
		 * 
		 * Builds the HTML for the <Topic's> prototype and appends it to the passed StringBuilder.
		 * 
		 * In order to have type links, links must be specified and contain any links that appear in the prototype.
		 * linkTargets must also be specified and contain the target <Topics> of all links.  If you do not need type
		 * links, set both to null.
		 */
		public void Build (Topic topic, bool isToolTip, IList<Link> links, IList<Topic> linkTargets, StringBuilder output)
			{
			this.topic = topic;
			this.isToolTip = isToolTip;
			this.addLinks = (links != null && linkTargets != null);
			this.links = links;
			this.linkTargets = linkTargets;
			htmlOutput = output;

			language = Engine.Instance.Languages.FromID(topic.LanguageID);
			parsedPrototype = topic.ParsedClassPrototype;

			if (parsedPrototype.Tokenizer.HasSyntaxHighlighting == false)
				{  language.SyntaxHighlight(parsedPrototype);  }

			int parentCount = topic.ParsedClassPrototype.NumberOfParents;

			if (isToolTip)
				{  parentCount = 0;  }

			htmlOutput.Append("<div class=\"NDClassPrototype" + (parentCount > 0 ? " HasParents" : "") + "\">");
				
			for (int i = 0; i < parentCount; i++)
				{  BuildParentClass(i);  }

			BuildCurrentClass();

			htmlOutput.Append("</div>");
			}


		/* Function: BuildParentClass
		 */
		protected void BuildParentClass (int index)
			{
			// xxx replace TClass with actual once links are in
			htmlOutput.Append("<div class=\"CPEntry TClass Parent\">");

			TokenIterator start, end;

			if (topic.ParsedClassPrototype.GetParentModifiers(index, out start, out end))
				{
				htmlOutput.Append("<div class=\"CPModifiers\">");
				BuildSyntaxHighlightedText(start, end, htmlOutput);
				htmlOutput.Append("</div>");
				}

				htmlOutput.Append("<div class=\"CPName\">");

				// xxx replace with symbol once links are in and if it resolved
				topic.ParsedClassPrototype.GetParentName(index, out start, out end);
				string name = start.Tokenizer.TextBetween(start, end);

				BuildWrappedTitle(name, Engine.Instance.TopicTypes.IDFromKeyword("class"), htmlOutput);

				if (topic.ParsedClassPrototype.GetParentTemplateSuffix(index, out start, out end))
					{
					// Include a zero-width space for wrapping
					htmlOutput.Append("&#8203;<span class=\"TemplateSignature\">");
					htmlOutput.EntityEncodeAndAppend( start.Tokenizer.TextBetween(start, end) );
					htmlOutput.Append("</span>");
					}

				htmlOutput.Append("</div>");

			htmlOutput.Append("</div>");
			}


		/* Function: BuildCurrentClass
		 */
		protected void BuildCurrentClass ()
			{
			htmlOutput.Append("<div class=\"CPEntry T" + Engine.Instance.TopicTypes.FromID(topic.TopicTypeID).SimpleIdentifier +" Current\">");


			// Keyword and modifiers.  We only show the keyword if it's not "class".

			TokenIterator startKeyword, endKeyword;
			topic.ParsedClassPrototype.GetKeyword(out startKeyword, out endKeyword);
			string keyword = startKeyword.String;

			TokenIterator startModifiers, endModifiers;
			bool hasModifiers = topic.ParsedClassPrototype.GetModifiers(out startModifiers, out endModifiers);

			if (hasModifiers || keyword != "class")
				{
				htmlOutput.Append("<div class=\"CPModifiers\">");

				if (hasModifiers)
					{
					BuildSyntaxHighlightedText(startModifiers, endModifiers, htmlOutput);

					if (keyword != "class")
						{  htmlOutput.Append(' ');  }
					}

				if (keyword != "class")
					{  BuildSyntaxHighlightedText(startKeyword, endKeyword);  }

				htmlOutput.Append("</div>");
				}


			// Name.  We use the fully resolved name in the symbol instead of the prototype name, which may just be the last segment.

			htmlOutput.Append("<div class=\"CPName\">");

			BuildWrappedTitle(topic.Symbol.FormatWithSeparator(this.language.MemberOperator), topic.TopicTypeID, htmlOutput);

			TokenIterator startTemplate, endTemplate;
			if (topic.ParsedClassPrototype.GetTemplateSuffix(out startTemplate, out endTemplate))
				{
				// Include a zero-width space for wrapping
				htmlOutput.Append("&#8203;<span class=\"TemplateSignature\">");
				htmlOutput.EntityEncodeAndAppend( startTemplate.Tokenizer.TextBetween(startTemplate, endTemplate) );
				htmlOutput.Append("</span>");
				}

			htmlOutput.Append("</div>");

			if (topic.ParsedClassPrototype.GetPostModifiers(out startModifiers, out endModifiers))
				{
				htmlOutput.Append("<div class=\"CPPostModifiers\">");
				BuildSyntaxHighlightedText(startModifiers, endModifiers, htmlOutput);
				htmlOutput.Append("</div>");
				}

			htmlOutput.Append("</div>");
			}



		// Group: Variables
		// __________________________________________________________________________


		/* var: parsedPrototype
		 * The prototype as a <ParsedClassPrototype> object.
		 */
		protected ParsedClassPrototype parsedPrototype;

		/* var: language
		 * The <Languages.Language> of the prototype.
		 */
		protected Languages.Language language;

		/* var: isToolTip
		 */
		protected bool isToolTip;

		/* var: addLinks
		 * Whether to add type links to the prototype.
		 */
		protected bool addLinks;

		}
	}

