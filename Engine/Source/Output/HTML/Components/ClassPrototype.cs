/*
 * Class: CodeClear.NaturalDocs.Engine.Output.HTML.Components.ClassPrototype
 * ____________________________________________________________________________
 *
 * A reusable class for building HTML class prototypes.
 *
 *
 * Threading: Not Thread Safe
 *
 *		This class is only designed to be used by one thread at a time.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2026 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Text;
using CodeClear.NaturalDocs.Engine.CommentTypes;
using CodeClear.NaturalDocs.Engine.Languages;
using CodeClear.NaturalDocs.Engine.Links;
using CodeClear.NaturalDocs.Engine.Prototypes;
using CodeClear.NaturalDocs.Engine.Symbols;
using CodeClear.NaturalDocs.Engine.Tokenization;
using CodeClear.NaturalDocs.Engine.Topics;


namespace CodeClear.NaturalDocs.Engine.Output.HTML.Components
	{
	public class ClassPrototype : HTML.Components.FormattedText
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: ClassPrototype
		 */
		public ClassPrototype (Context context) : base (context)
			{
			parsedClassPrototype = null;
			language = null;

			links = null;
			linkTargets = null;
			isToolTip = false;
			}


		/* Function: BuildClassPrototype
		 *
		 * Builds the HTML for the passed prototype.
		 *
		 * In order to have an inheritance diagram, links must contain the all the ClassParent links from the prototype's class
		 * to its parents, and all the ClassParent links from child classes to the prototype's class.
		 *
		 * Requirements:
		 *
		 *		- The <Context>'s topic and page must be set.
		 */
		public string BuildClassPrototype (ParsedClassPrototype parsedPrototype, Context context, bool isToolTip, IList<Link> links = null,
														 IList<Topics.Topic> linkTargets = null)
			{
			StringBuilder output = new StringBuilder();
			AppendClassPrototype(parsedPrototype, context, isToolTip, output, links, linkTargets);
			return output.ToString();
			}


		/* Function: AppendClassPrototype
		 *
		 * Builds the HTML for the passed prototype and appends it to the passed StringBuilder.
		 *
		 * In order to have an inheritance diagram, links must contain the all the ClassParent links from the prototype's class
		 * to its parents, and all the ClassParent links from child classes to the prototype's class.
		 *
		 * Requirements:
		 *
		 *		- The <Context>'s topic and page must be set.
		 */
		public void AppendClassPrototype (ParsedClassPrototype parsedPrototype, Context context, bool isToolTip, StringBuilder output,
														  IList<Link> links = null, IList<Topics.Topic> linkTargets = null)
			{
			this.parsedClassPrototype = parsedPrototype;
			this.context = context;
			this.language = EngineInstance.Languages.FromID(context.Topic.LanguageID);

			this.links = links;
			this.linkTargets = linkTargets;
			this.isToolTip = isToolTip;

			if (parsedPrototype.Tokenizer.HasSyntaxHighlighting == false)
				{  language.Parser.SyntaxHighlight(parsedPrototype);  }

			if (isToolTip)
			    {
			    output.Append("<div class=\"NDClassPrototype\" id=\"NDClassPrototype" + context.Topic.TopicID + "\">");
			        AppendCurrentClassPrototype(output);
			    output.Append("</div>");
			    }
			else
			    {
			    List<Parent> parents = GetParentList();
				List<Topics.Topic> children = GetChildList();


				// Main div

			    output.Append("<div class=\"NDClassPrototype");

				if (parents != null && parents.Count > 0)
					{  output.Append(" HasParents");  }
				if (children != null && children.Count > 0)
					{  output.Append(" HasChildren");  }

				output.Append("\" id=\"NDClassPrototype" + context.Topic.TopicID + "\">");


				// Parents

				if (parents != null)
					{
					foreach (var parent in parents)
						{  AppendParentClassPrototype(parent, output);  }
					}


				// Current class

		        AppendCurrentClassPrototype(output);


				// Children

				if (children != null)
					{
					// +1 so we never have to see "and 1 other child" which would take up the same amount of space.
					if (children.Count <= MaxExpandedChildren + 1)
						{
						foreach (var child in children)
							{  AppendChildClassPrototype(child, output);  }
						}
					else
						{
						for (int i = 0; i < MaxExpandedChildren; i++)
							{  AppendChildClassPrototype(children[i], output);  }

						output.Append("<a href=\"javascript:NDContentPage.ShowAdditionalChildren('NDClassPrototype" + context.Topic.TopicID + "')\" " +
																		"class=\"CPAdditionalChildrenNotice\">");

							output.EntityEncodeAndAppend(
								Locale.Get("NaturalDocs.Engine", "HTML.AdditionalChildren(number)", children.Count - MaxExpandedChildren)
								);

						output.Append("</a><div class=\"CPAdditionalChildren\">");

							for (int i = MaxExpandedChildren; i < children.Count; i++)
								{  AppendChildClassPrototype(children[i], output);  }

						output.Append("</div>");
						}
					}

			    output.Append("</div>");
			    }
			}



		// Group: Support Functions
		// __________________________________________________________________________


		/* Function: GetParentList
		 */
		protected List<Parent> GetParentList ()
			{

			// First separate out all the class parent links that apply to this class.

			List<Link> parentLinks = null;

			if (links != null)
				{
				foreach (var link in links)
					{
					if (link.Type == LinkType.ClassParent && link.ClassID == context.Topic.ClassID)
						{
						if (parentLinks == null)
							{  parentLinks = new List<Link>();  }

						parentLinks.Add(link);
						}
					}
				}

			// We don't have to worry about parents appearing in the prototype if there aren't any class parent links
			// because there would have been one generated for each of them in the parsing stage.
			if (parentLinks == null)
				{  return null;  }


			// Now make entries for all the parents in the prototype.  Note that it's possible for there to be class parent
			// links yet no parents in the prototype.  Some languages define them separately, and some allow classes to
			// be defined across multiple files and the parents may only appear in one.

			List<Parent> parents = new List<Parent>();

			int prototypeParentCount = parsedClassPrototype.NumberOfParents;
			TokenIterator start, end;

			for (int i = 0; i < prototypeParentCount; i++)
				{
				parsedClassPrototype.GetParentName(i, out start, out end);
				string parentName = start.TextBetween(end);

				Parent parent = new Parent();
				parent.prototypeIndex = i;
				parent.prototypeSymbol = SymbolString.FromPlainText_NoParameters(parentName);

				parents.Add(parent);
				}


			// Now we make one pass where we merge the class parent links with the prototype parents, if any.  Since
			// the links have been generated from the prototype, we don't have to do anything other than simple symbol
			// matching.  We don't have to worry about things like StringBuilder versus System.Text.StringBuilder yet.

			for (int i = 0; i < parentLinks.Count; /* don't auto-increment */)
				{
				bool foundMatch = false;

				foreach (var parent in parents)
					{
					if (parent.prototypeSymbol == parentLinks[i].Symbol)
						{
						if (parent.link == null)
							{  parent.link = parentLinks[i];  }

						foundMatch = true;

						// Keep going, don't break on the first match.  It's possible for multiple prototype parents to share
						// the same link, such as IList and IList<T>.
						}
					}

				if (foundMatch)
					{  parentLinks.RemoveAt(i);  }
				else
					{  i++;  }
				}


			// Now we do a second pass where we match links by their targets.  This is so if there's two links, one to
			// StringBuilder and one to System.Text.StringBuilder, and they both resolve to the same topic only one
			// will appear.  However, if neither resolve then just include them both.  We won't try to guess whether
			// partial symbol matches are probably the same parent.

			foreach (var parentLink in parentLinks)
				{
				bool found = false;

				if (parentLink.IsResolved)
					{
					foreach (var parent in parents)
						{
						if (parent.link != null && parent.link.TargetTopicID == parentLink.TargetTopicID)
							{
							found = true;
							break;
							}
						}
					}
				// If the link wasn't resolved we just leave found as false so it gets added.

				if (!found)
					{
					Parent newParent = new Parent();
					newParent.link = parentLink;
					parents.Add(newParent);
					}
				}


			// Still not done.  Now go through the link targets and find the matches for each resolved link.

			foreach (var parent in parents)
				{
				if (parent.link != null && parent.link.IsResolved)
					{
					foreach (var linkTarget in linkTargets)
						{
						if (linkTarget.TopicID == parent.link.TargetTopicID)
							{
							parent.targetTopic = linkTarget;
							break;
							}
						}
					}
				}

			return parents;
			}


		/* Function: GetChildList
		 */
		protected List<Topics.Topic> GetChildList ()
			{

			// First find all the class parent links that resolve to this one and collect the class IDs.

			IDObjects.NumberSet childClassIDs = null;

			if (links != null)
				{
				foreach (var link in links)
					{
					if (link.Type == LinkType.ClassParent && link.TargetClassID == context.Topic.ClassID)
						{
						if (childClassIDs == null)
							{  childClassIDs = new IDObjects.NumberSet();  }

						childClassIDs.Add(link.ClassID);
						}
					}
				}

			if (childClassIDs == null)
				{  return null;  }


			// Now find the topics that define those classes.

			List<Topics.Topic> childTopics = new List<Topics.Topic>();

			foreach (var linkTarget in linkTargets)
				{
				if (linkTarget.DefinesClass && childClassIDs.Contains(linkTarget.ClassID))
					{
					childTopics.Add(linkTarget);
					childClassIDs.Remove(linkTarget.ClassID);
					}
				}

			if (childTopics.Count == 0)
				{  return null;  }


			// Now sort the child topics by symbol.

			childTopics.Sort(
				delegate(Topics.Topic a, Topics.Topic b)
					{
					return a.Symbol.CompareTo(b.Symbol, !language.CaseSensitive);
					}
				);

			return childTopics;
			}


		/* Function: AppendParentClassPrototype
		 */
		protected void AppendParentClassPrototype (Parent parent, StringBuilder output)
			{
			CommentType parentCommentType;
			string memberOperator;

			if (parent.targetTopic != null)
				{
				parentCommentType = EngineInstance.CommentTypes.FromID(parent.targetTopic.CommentTypeID);
				memberOperator = language.MemberOperator;
				}
			else
				{
				parentCommentType = EngineInstance.CommentTypes.FromKeyword("class", language.ID);
				memberOperator = ".";
				}


			// Main tag

			string entryClass = "CPEntry Parent";
			if (parentCommentType != null)
				{  entryClass += " T" + parentCommentType.SimpleIdentifier;  }

			if (parent.targetTopic != null)
				{  AppendOpeningLinkTag(parent.targetTopic, output, entryClass);  }
			else
				{  output.Append("<div class=\"" + entryClass + "\">");  }


			// Modifiers

			TokenIterator start, end;

			if (parent.prototypeIndex != -1 &&
				parsedClassPrototype.GetParentModifiers(parent.prototypeIndex, out start, out end) == true)
				{
				output.Append("<div class=\"CPModifiers\">");
				AppendSyntaxHighlightedText(start, end, output);
				output.Append("</div>");
				}


			// Name

			output.Append("<div class=\"CPName\">");

			string name = null;

			if (parent.targetTopic != null)
				{  name = parent.targetTopic.Symbol.FormatWithSeparator(memberOperator);  }
			else if (parent.link != null)
				{  name = parent.link.Symbol.FormatWithSeparator(memberOperator);  }
			#if DEBUG
			else
				{  throw new Exception("There was a parent without a target topic or a link associated with it.");  }
			#endif

			WrappedTitleMode wrappedTitleMode;

			if (parentCommentType.IsFile == true)
				{  wrappedTitleMode = WrappedTitleMode.File;  }
			else if (parentCommentType.IsCode == true)
				{  wrappedTitleMode = WrappedTitleMode.Code;  }
			else
				{  wrappedTitleMode = WrappedTitleMode.None;  }

			AppendWrappedTitle(name, wrappedTitleMode, output);


			// Template suffix

			if (parent.prototypeIndex != -1 &&
				parsedClassPrototype.GetParentTemplateSuffix(parent.prototypeIndex, out start, out end) == true)
				{
				// Include preceding whitespace if present, or a zero-width space for wrapping if not
				if (!start.PreviousPastWhitespace(PreviousPastWhitespaceMode.EndingBounds))
					{  output.Append("&#8203;");  }

				output.Append("<span class=\"TemplateSignature\">");
				AppendSyntaxHighlightedText(start, end, output);
				output.Append("</span>");
				}

			output.Append("</div>");

			if (parent.targetTopic != null)
				{  output.Append("</a>");  }
			else
				{  output.Append("</div>");  }
			}


		/* Function: AppendCurrentClassPrototype
		 */
		protected void AppendCurrentClassPrototype (StringBuilder output)
			{

			// Main tag

			string simpleTypeIdentifier = EngineInstance.CommentTypes.FromID(context.Topic.CommentTypeID).SimpleIdentifier;
			output.Append("<div class=\"CPEntry T" + simpleTypeIdentifier +" Current\">");


			// Pre-prototype lines

			int lineCount = parsedClassPrototype.NumberOfPrePrototypeLines;
			TokenIterator start, end;

			for (int i = 0; i < lineCount; i++)
				{
				parsedClassPrototype.GetPrePrototypeLine(i, out start, out end);

				output.Append("<div class=\"CPPrePrototypeLine\">");
				AppendSyntaxHighlightedText(start, end, output);
				output.Append("</div>");
				}


			// Keyword and modifiers.  We only show the keyword if it's not "class", and we exclude "partial" from the list of modifiers.

			TokenIterator startKeyword, endKeyword;
			parsedClassPrototype.GetKeyword(out startKeyword, out endKeyword);
			string keyword = startKeyword.String;

			TokenIterator startModifiers, endModifiers;
			bool hasModifiers = parsedClassPrototype.GetModifiers(out startModifiers, out endModifiers);

			if (hasModifiers || keyword != "class")
				{
				StringBuilder modifiersOutput = new StringBuilder();
				TokenIterator partial;

				bool hasPartial = startModifiers.Tokenizer.FindTokenBetween("partial", language.CaseSensitive, startModifiers, endModifiers,
																										out partial);

				// Make sure "partial" is a keyword and not part of a longer identifier
				if (hasPartial)
					{  hasPartial = partial.IsStandaloneWord();  }

				// Add the modifiers sans-"partial"
				if (hasModifiers && hasPartial)
					{
					if (partial > startModifiers)
						{
						TokenIterator lookbehind = partial;
						lookbehind.PreviousPastWhitespace(PreviousPastWhitespaceMode.EndingBounds);

						AppendSyntaxHighlightedText(startModifiers, lookbehind, modifiersOutput);
						}

					partial.Next();
					partial.NextPastWhitespace();

					if (partial < endModifiers)
						{
						if (modifiersOutput.Length > 0)
							{  modifiersOutput.Append(' ');  }

						AppendSyntaxHighlightedText(partial, endModifiers, modifiersOutput);
						}
					}
				else if (hasModifiers)
					{
					AppendSyntaxHighlightedText(startModifiers, endModifiers, modifiersOutput);
					}

				// Add the keyword if it isn't "class"
				if (keyword != "class")
					{
					if (modifiersOutput.Length > 0)
						{  modifiersOutput.Append(' ');  }

					AppendSyntaxHighlightedText(startKeyword, endKeyword, modifiersOutput);
					}

				if (modifiersOutput.Length > 0)
					{
					output.Append("<div class=\"CPModifiers\">");
					output.Append(modifiersOutput.ToString());
					output.Append("</div>");
					}
				}


			// Name.  We use the fully resolved name in the symbol instead of the prototype name, which may just be the last segment.

			output.Append("<div class=\"CPName\">");

			AppendWrappedTitle(context.Topic.Symbol.FormatWithSeparator(language.MemberOperator), WrappedTitleMode.Code, output);

			TokenIterator startTemplate, endTemplate;
			if (parsedClassPrototype.GetTemplateSuffix(out startTemplate, out endTemplate))
				{
				// Include preceding whitespace if present, or a zero-width space for wrapping if not
				if (!startTemplate.PreviousPastWhitespace(PreviousPastWhitespaceMode.EndingBounds))
					{  output.Append("&#8203;");  }

				output.Append("<span class=\"TemplateSignature\">");
				AppendSyntaxHighlightedText(startTemplate, endTemplate, output);
				output.Append("</span>");
				}

			output.Append("</div>");


			// Post-prototype lines

			lineCount = parsedClassPrototype.NumberOfPostPrototypeLines;

			for (int i = 0; i < lineCount; i++)
				{
				parsedClassPrototype.GetPostPrototypeLine(i, out start, out end);

				output.Append("<div class=\"CPPostPrototypeLine\">");
				AppendSyntaxHighlightedText(start, end, output);
				output.Append("</div>");
				}

			output.Append("</div>");
			}


		/* Function: AppendChildClassPrototype
		 */
		protected void AppendChildClassPrototype (Topics.Topic childTopic, StringBuilder output)
			{
			CommentType childCommentType = EngineInstance.CommentTypes.FromID(childTopic.CommentTypeID);
			string memberOperator = language.MemberOperator;

			AppendOpeningLinkTag(childTopic, output, "CPEntry Child T" + childCommentType.SimpleIdentifier);

				output.Append("<div class=\"CPName\">");

					AppendWrappedTitle(childTopic.Symbol.FormatWithSeparator(memberOperator), WrappedTitleMode.Code, output);

				output.Append("</div>");

			output.Append("</a>");
			}


		// Group: Constants
		// __________________________________________________________________________


		/* Constant: MaxExpandedChildren
		 * The number of children to show by default.
		 */
		protected const int MaxExpandedChildren = 4;



		// Group: Variables
		// __________________________________________________________________________


		/* var: parsedClassPrototype
		 * The prototype as a <ParsedClassPrototype> object.
		 */
		protected ParsedClassPrototype parsedClassPrototype;

		/* var: language
		 * The <Language> associated with the prototype.
		 */
		protected Language language;

		/* var: links
		 * A list of <Links> that contain any which will appear in the prototype, or null if links aren't needed.
		 */
		protected IList<Link> links;

		/* var: linkTargets
		 * A list of <Topics> that contain the targets of any resolved links appearing in <links>, or null if links aren't needed.
		 */
		protected IList<Topics.Topic> linkTargets;

		/* var: isToolTip
		 */
		protected bool isToolTip;


		/* __________________________________________________________________________
		 *
		 * Class: CodeClear.NaturalDocs.Engine.Output.HTML.Components.ClassPrototype.Parent
		 * __________________________________________________________________________
		 */
		protected class Parent
			{
			public Parent ()
				{
				prototypeIndex = -1;
				prototypeSymbol = default(SymbolString);
				link = null;
				targetTopic = null;
				}

			/* var: prototypeIndex
			 * The parent index in a <ParsedClassPrototype>, or -1 if not.
			 */
			public int prototypeIndex;

			/* var: prototypeSymbol
			 * The parent name as a <SymbolString> if it appears in a <ParsedClassPrototype>, or null if not.
			 */
			public SymbolString prototypeSymbol;

			/* var: link
			 * The class parent <Link> for this parent, or null if none.
			 */
			public Link link;

			/* var: targetTopic
			 * The <Topic> that serves as the target of <link>, or null if none.
			 */
			public Topics.Topic targetTopic;
			}
		}
	}
