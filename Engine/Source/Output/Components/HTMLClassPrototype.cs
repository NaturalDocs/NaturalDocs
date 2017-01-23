/* 
 * Class: CodeClear.NaturalDocs.Engine.Output.Components.HTMLClassPrototype
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

// This file is part of Natural Docs, which is Copyright © 2003-2016 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Text;
using CodeClear.NaturalDocs.Engine.Links;
using CodeClear.NaturalDocs.Engine.Symbols;
using CodeClear.NaturalDocs.Engine.Tokenization;
using CodeClear.NaturalDocs.Engine.Topics;
using CodeClear.NaturalDocs.Engine.CommentTypes;


namespace CodeClear.NaturalDocs.Engine.Output.Components
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

			language = EngineInstance.Languages.FromID(topic.LanguageID);
			parsedPrototype = topic.ParsedClassPrototype;

			if (parsedPrototype.Tokenizer.HasSyntaxHighlighting == false)
				{  language.SyntaxHighlight(parsedPrototype);  }

			if (isToolTip)
			    {  
			    htmlOutput.Append("<div class=\"NDClassPrototype\" id=\"NDClassPrototype" + topic.TopicID + "\">");
			        BuildCurrentClass();
			    htmlOutput.Append("</div>");
			    }
			else
			    {
			    List<Parent> parents = GetParentList();
				List<Topic> children = GetChildList();

			    htmlOutput.Append("<div class=\"NDClassPrototype");

					if (parents != null && parents.Count > 0)
						{  htmlOutput.Append(" HasParents");  }
					if (children != null && children.Count > 0)
						{  htmlOutput.Append(" HasChildren");  }

					htmlOutput.Append("\" id=\"NDClassPrototype" + topic.TopicID + "\">");
				
					if (parents != null)
						{
						foreach (var parent in parents)
							{  BuildParentClass(parent);  }
						}

		        BuildCurrentClass();

					if (children != null)
						{
						// +1 so we never have to see "and 1 other child" which would take up the same amount of space.
						if (children.Count <= MaxExpandedChildren + 1)
							{
							foreach (var child in children)
								{  BuildChildClass(child);  }
							}
						else
							{
							for (int i = 0; i < MaxExpandedChildren; i++)
								{  BuildChildClass(children[i]);  }

							htmlOutput.Append("<a href=\"javascript:NDContentPage.ShowAdditionalChildren('NDClassPrototype" + topic.TopicID + "')\" " +
																		 "class=\"CPAdditionalChildrenNotice\">");

								htmlOutput.EntityEncodeAndAppend(
									Locale.Get("NaturalDocs.Engine", "HTML.AdditionalChildren(number)", children.Count - MaxExpandedChildren)
									);

							htmlOutput.Append("</a><div class=\"CPAdditionalChildren\">");

								for (int i = MaxExpandedChildren; i < children.Count; i++)
									{  BuildChildClass(children[i]);  }

							htmlOutput.Append("</div>");
							}
						}

			    htmlOutput.Append("</div>");
			    }
			}


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
					if (link.Type == LinkType.ClassParent && link.ClassID == topic.ClassID)
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

			int prototypeParentCount = topic.ParsedClassPrototype.NumberOfParents;
			TokenIterator start, end;

			for (int i = 0; i < prototypeParentCount; i++)
				{
				topic.ParsedClassPrototype.GetParentName(i, out start, out end);
				string parentName = start.Tokenizer.TextBetween(start, end);

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
						break;
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


		/* Function: BuildParentClass
		 */
		protected void BuildParentClass (Parent parent)
			{
			CommentType parentCommentType;
			string memberOperator;

			if (parent.targetTopic != null)
				{  
				parentCommentType = EngineInstance.CommentTypes.FromID(parent.targetTopic.CommentTypeID);  
				memberOperator = EngineInstance.Languages.FromID(parent.targetTopic.LanguageID).MemberOperator;
				}
			else
				{  
				parentCommentType = EngineInstance.CommentTypes.FromKeyword("class");
				memberOperator = ".";
				}

			TokenIterator start, end;
			
			string entryClass = "CPEntry Parent";
			if (parentCommentType != null)
				{  entryClass += " T" + parentCommentType.SimpleIdentifier;  }

			if (parent.targetTopic != null)
				{  BuildLinkTag(parent.targetTopic, entryClass);  }
			else
				{  htmlOutput.Append("<div class=\"" + entryClass + "\">");  }

				if (parent.prototypeIndex != -1 && 
					topic.ParsedClassPrototype.GetParentModifiers(parent.prototypeIndex, out start, out end) == true)
				    {
				    htmlOutput.Append("<div class=\"CPModifiers\">");
				    BuildSyntaxHighlightedText(start, end, htmlOutput);
				    htmlOutput.Append("</div>");
				    }

				htmlOutput.Append("<div class=\"CPName\">");

					string name = null;

					if (parent.targetTopic != null)
					    {  name = parent.targetTopic.Symbol.FormatWithSeparator(memberOperator);  }
					else if (parent.link != null)
					    {  name = parent.link.Symbol.FormatWithSeparator(memberOperator);  }
					#if DEBUG
					else
						{  throw new Exception("There was a parent without a target topic or a link associated with it.");  }
					#endif

					BuildWrappedTitle(name, (parentCommentType != null ? parentCommentType.ID : 0), htmlOutput);

					if (parent.prototypeIndex != -1 &&
						topic.ParsedClassPrototype.GetParentTemplateSuffix(parent.prototypeIndex, out start, out end) == true)
						{
						// Include a zero-width space for wrapping
						htmlOutput.Append("&#8203;<span class=\"TemplateSignature\">");
						htmlOutput.EntityEncodeAndAppend( start.Tokenizer.TextBetween(start, end) );
						htmlOutput.Append("</span>");
						}

				htmlOutput.Append("</div>");

			if (parent.targetTopic != null)
				{  htmlOutput.Append("</a>");  }
			else
				{  htmlOutput.Append("</div>");  }
			}


		/* Function: BuildCurrentClass
		 */
		protected void BuildCurrentClass ()
			{
			htmlOutput.Append("<div class=\"CPEntry T" + EngineInstance.CommentTypes.FromID(topic.CommentTypeID).SimpleIdentifier +" Current\">");


			// Pre-prototype lines

			int lineCount = parsedPrototype.NumberOfPrePrototypeLines;
			TokenIterator start, end;

			for (int i = 0; i < lineCount; i++)
				{
				parsedPrototype.GetPrePrototypeLine(i, out start, out end);

				htmlOutput.Append("<div class=\"CPPrePrototypeLine\">");
				BuildSyntaxHighlightedText(start, end);
				htmlOutput.Append("</div>");
				}


			// Keyword and modifiers.  We only show the keyword if it's not "class".

			TokenIterator startKeyword, endKeyword;
			topic.ParsedClassPrototype.GetKeyword(out startKeyword, out endKeyword);
			string keyword = startKeyword.String;

			TokenIterator startModifiers, endModifiers;
			bool hasModifiers = topic.ParsedClassPrototype.GetModifiers(out startModifiers, out endModifiers);

			if (hasModifiers || keyword != "class")
				{
				StringBuilder modifiersOutput = new StringBuilder();
				TokenIterator partial;
				
				bool hasPartial = startModifiers.Tokenizer.FindTokenBetween("partial", EngineInstance.Languages.FromID(topic.LanguageID).CaseSensitive,
																									  startModifiers, endModifiers, out partial);

				if (hasPartial)
					{
					TokenIterator lookahead = partial;
					lookahead.Next();

					if (lookahead < endModifiers && 
						(lookahead.FundamentalType == FundamentalType.Text ||
						 lookahead.Character == '_'))
						{  hasPartial = false;  }

					TokenIterator lookbehind = partial;
					lookbehind.Previous();

					if (lookbehind >= startModifiers &&
						(lookbehind.FundamentalType == FundamentalType.Text ||
						 lookbehind.Character == '_'))
						{  hasPartial = false;  }
					}

				if (hasModifiers && hasPartial)
					{
					if (partial > startModifiers)
						{
						TokenIterator lookbehind = partial;
						lookbehind.PreviousPastWhitespace(PreviousPastWhitespaceMode.EndingBounds);

						BuildSyntaxHighlightedText(startModifiers, lookbehind, modifiersOutput);
						}

					partial.Next();
					partial.NextPastWhitespace();

					if (partial < endModifiers)
						{
						if (modifiersOutput.Length > 0)
							{  modifiersOutput.Append(' ');  }

						BuildSyntaxHighlightedText(partial, endModifiers, modifiersOutput);
						}
					}
				else if (hasModifiers)
					{
					BuildSyntaxHighlightedText(startModifiers, endModifiers, modifiersOutput);
					}

				if (keyword != "class")
					{  
					if (modifiersOutput.Length > 0)
						{  modifiersOutput.Append(' ');  }

					BuildSyntaxHighlightedText(startKeyword, endKeyword, modifiersOutput);  
					}

				if (modifiersOutput.Length > 0)
					{
					htmlOutput.Append("<div class=\"CPModifiers\">");
					htmlOutput.Append(modifiersOutput.ToString());
					htmlOutput.Append("</div>");
					}
				}


			// Name.  We use the fully resolved name in the symbol instead of the prototype name, which may just be the last segment.

			htmlOutput.Append("<div class=\"CPName\">");

			BuildWrappedTitle(topic.Symbol.FormatWithSeparator(this.language.MemberOperator), topic.CommentTypeID, htmlOutput);

			TokenIterator startTemplate, endTemplate;
			if (topic.ParsedClassPrototype.GetTemplateSuffix(out startTemplate, out endTemplate))
				{
				// Include a zero-width space for wrapping
				htmlOutput.Append("&#8203;<span class=\"TemplateSignature\">");
				htmlOutput.EntityEncodeAndAppend( startTemplate.Tokenizer.TextBetween(startTemplate, endTemplate) );
				htmlOutput.Append("</span>");
				}

			htmlOutput.Append("</div>");


			// Post-prototype lines

			lineCount = parsedPrototype.NumberOfPostPrototypeLines;

			for (int i = 0; i < lineCount; i++)
				{
				parsedPrototype.GetPostPrototypeLine(i, out start, out end);

				htmlOutput.Append("<div class=\"CPPostPrototypeLine\">");
				BuildSyntaxHighlightedText(start, end);
				htmlOutput.Append("</div>");
				}

			htmlOutput.Append("</div>");
			}


		/* Function: GetChildList
		 */
		protected List<Topic> GetChildList ()
			{

			// First find all the class parent links that resolve to this one and collect the class IDs.

			IDObjects.NumberSet childClassIDs = null;

			if (links != null)
				{
				foreach (var link in links)
					{
					if (link.Type == LinkType.ClassParent && link.TargetClassID == topic.ClassID)
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

			List<Topic> childTopics = new List<Topic>();

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

			bool caseSensitive = EngineInstance.Languages.FromID(topic.LanguageID).CaseSensitive;

			childTopics.Sort( 
				delegate(Topic a, Topic b)
					{
					return a.Symbol.CompareTo(b.Symbol, !caseSensitive);
					}
				);

			return childTopics;
			}


		/* Function: BuildChildClass
		 */
		protected void BuildChildClass (Topic childTopic)
			{
			CommentType childCommentType = EngineInstance.CommentTypes.FromID(childTopic.CommentTypeID);  
			string memberOperator = EngineInstance.Languages.FromID(childTopic.LanguageID).MemberOperator;

			BuildLinkTag(childTopic, "CPEntry Child T" + childCommentType.SimpleIdentifier);

				htmlOutput.Append("<div class=\"CPName\">");

					BuildWrappedTitle(childTopic.Symbol.FormatWithSeparator(memberOperator), childCommentType.ID, htmlOutput);

				htmlOutput.Append("</div>");

			htmlOutput.Append("</a>");
			}


		// Group: Constants
		// __________________________________________________________________________


		/* Constant: MaxExpandedChildren
		 * The number of children to show by default.
		 */
		protected const int MaxExpandedChildren = 4;



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



		/* __________________________________________________________________________
		 * 
		 * Class: CodeClear.NaturalDocs.Engine.Output.Components.HTMLClassPrototype.Parent
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
			public Topic targetTopic;
			}
		}
	}

