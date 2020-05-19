/* 
 * Class: CodeClear.NaturalDocs.Engine.Output.Components.JSSummaryData
 * ____________________________________________________________________________
 * 
 * A helper class to build JavaScript summary data for <Output.Builders.HTML>.  See <JavaScript Summary Data>
 * and <JavaScript ToolTip Data> for the output formats.
 * 
 * Topic: Usage
 *		
 *		- Create a JSSummaryData object.
 *		- Call <Build()>.
 * 
 * Threading: Not Thread Safe
 * 
 *		This class is only designed to be used by one thread at a time.  It has an internal state that is used during a call to
 *		<Build()>, and another <Build()> should not be started until it's completed.  Instead each thread should create its 
 *		own object.
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2020 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Text;
using CodeClear.NaturalDocs.Engine.Collections;
using CodeClear.NaturalDocs.Engine.Languages;
using CodeClear.NaturalDocs.Engine.Links;
using CodeClear.NaturalDocs.Engine.Topics;
using CodeClear.NaturalDocs.Engine.CommentTypes;


namespace CodeClear.NaturalDocs.Engine.Output.Components
	{
	public class JSSummaryData
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: JSSummaryData
		 */
		public JSSummaryData (HTMLTopicPage topicPage)
			{
			this.topicPage = topicPage;

			output = null;
			fileTitle = null;
			fileHashPath = null;
			topics = null;

			tooltipBuilder = new HTML.Components.Tooltip( new HTML.Context(HTMLBuilder, topicPage) );
			usedLanguages = new List<Language>();
			usedCommentTypes = new List<CommentType>();

			htmlComponent = new HTMLComponent(topicPage);
			}


		/* Function: Build
		 * 
		 * Builds the JavaScript metadata for the <Topic> and appends it to the passed StringBuilder.
		 * 
		 * Parameters:
		 * 
		 *		topics - The <Topics> that appear in this file.
		 *		links - A list of <Links> that includes everything that appears in the topic bodies.
		 *		fileTitle - The title of the file.
		 *		fileHashPath - The hash path of the file.
		 *		summaryPath - A path to the summary metadata file to build.
		 *		summaryToolTipsPath - A path to the summary tooltips metadata file to build.
		 */
		public void Build (IList<Topic> topics, IList<Link> links, string fileTitle, string fileHashPath, Path summaryPath, Path summaryToolTipsPath)
			{
			this.output = new StringBuilder();
			this.fileTitle = fileTitle;
			this.fileHashPath = fileHashPath;
			this.topics = topics;
			this.links = links;
			
			usedLanguages.Clear();
			usedCommentTypes.Clear();


			// Summary.js

			output.Append(
				"NDFramePage.OnPageTitleLoaded(\"" + fileHashPath.StringEscape() + "\",\"" + fileTitle.StringEscape() + "\");"
				);

			if (!EngineInstance.Config.ShrinkFiles)
				{
				output.AppendLine();
				output.AppendLine();
				}

			output.Append("NDSummary.OnSummaryLoaded(\"" + fileHashPath.StringEscape() + "\",");

			BuildLanguageList();
			output.Append(',');

			if (!EngineInstance.Config.ShrinkFiles)
				{  output.AppendLine();  }

			BuildCommentTypeList();
			output.Append(',');

			if (!EngineInstance.Config.ShrinkFiles)
				{  output.AppendLine();  }

			BuildSummaryEntries();

			if (!EngineInstance.Config.ShrinkFiles)
				{
				output.AppendLine();
				output.Append("   ");
				}

			output.Append(");");


			System.IO.StreamWriter summaryFile = HTMLBuilder.CreateTextFileAndPath(summaryPath);

			try
				{  summaryFile.Write(output.ToString());  }
			finally
				{  summaryFile.Dispose();  }


			// SummaryToolTips.js

			output.Remove(0, output.Length);

			output.Append(
				"NDSummary.OnToolTipsLoaded(\"" + fileHashPath.StringEscape() + "\",{"
				);

			BuildSummaryToolTips();

			if (!EngineInstance.Config.ShrinkFiles)
				{
				output.AppendLine();
				output.Append("   ");
				}

			output.Append("});");

			System.IO.StreamWriter summaryToolTipsFile = HTMLBuilder.CreateTextFileAndPath(summaryToolTipsPath);

			try
				{  summaryToolTipsFile.Write(output.ToString());  }
			finally
				{  summaryToolTipsFile.Dispose();  }
			}


		/* Function: BuildLanguageList
		 */
		protected void BuildLanguageList ()
			{
			// Build used language list

			foreach (Topic topic in topics)
				{
				bool found = false;

				for (int i = 0; found == false && i < usedLanguages.Count; i++)
					{
					if (usedLanguages[i].ID == topic.LanguageID)
						{  found = true;  }
					}

				if (!found)
					{  usedLanguages.Add( EngineInstance.Languages.FromID(topic.LanguageID) );  }
				}


			// Sort used language list

			usedLanguages.Sort(
				delegate (Language a, Language b)
					{  return string.Compare(a.Name, b.Name);  }
				);

			
			// Build JavaScript

			if (!EngineInstance.Config.ShrinkFiles)
				{
				output.AppendLine();
				output.Append("   ");
				}

			output.Append('[');

			for (int i = 0; i < usedLanguages.Count; i++)
				{
				output.Append("[\"");
				output.StringEscapeAndAppend(usedLanguages[i].Name.ToHTML());
				output.Append("\",\"");
				output.StringEscapeAndAppend(usedLanguages[i].SimpleIdentifier);
				output.Append("\"]");

				if (i != usedLanguages.Count - 1)
					{  
					output.Append(',');  

					if (!EngineInstance.Config.ShrinkFiles)
						{
						output.AppendLine();
						output.Append("   ");
						}
					}
				}

			output.Append(']');
			}


		/* Function: BuildCommentTypeList
		 */
		protected void BuildCommentTypeList ()
			{
			// Build used comment type list

			foreach (Topic topic in topics)
				{
				bool found = false;

				for (int i = 0; found == false && i < usedCommentTypes.Count; i++)
					{
					if (usedCommentTypes[i].ID == topic.CommentTypeID)
						{  found = true;  }
					}

				if (!found)
					{  usedCommentTypes.Add( EngineInstance.CommentTypes.FromID(topic.CommentTypeID) );  }
				}


			// Sort used comment type list

			usedCommentTypes.Sort(
				delegate (CommentType a, CommentType b)
					{  return string.Compare(a.Name, b.Name);  } // xxx should be by comments.txt order
				);


			// Build JavaScript output

			if (!EngineInstance.Config.ShrinkFiles)
				{
				output.AppendLine();
				output.Append("   ");
				}

			output.Append('[');

			for (int i = 0; i < usedCommentTypes.Count; i++)
				{
				output.Append("[\"");
				output.StringEscapeAndAppend(usedCommentTypes[i].PluralDisplayName.ToHTML());
				output.Append("\",\"");
				output.StringEscapeAndAppend(usedCommentTypes[i].SimpleIdentifier);
				output.Append("\"]");

				if (i != usedCommentTypes.Count - 1)
					{  
					output.Append(',');  
					
					if (!EngineInstance.Config.ShrinkFiles)
						{
						output.AppendLine();
						output.Append("   ");
						}
					}
				}

			output.Append(']');
			}


		/* Function: BuildSummaryEntries
		 */
		protected void BuildSummaryEntries ()
			{
			if (!EngineInstance.Config.ShrinkFiles)
				{
				output.AppendLine();
				output.Append("   ");
				}

			output.Append('[');

			for (int topicIndex = 0; topicIndex < topics.Count; topicIndex++)
				{
				Topic topic = topics[topicIndex];

				output.Append('[');
				output.Append(topic.TopicID);
				output.Append(',');

				for (int usedLanguageIndex = 0; usedLanguageIndex < usedLanguages.Count; usedLanguageIndex++)
					{
					if (usedLanguages[usedLanguageIndex].ID == topic.LanguageID)
						{  
						output.Append(usedLanguageIndex);
						break;
						}
					}

				output.Append(',');

				for (int usedCommentTypeIndex = 0; usedCommentTypeIndex < usedCommentTypes.Count; usedCommentTypeIndex++)
					{
					if (usedCommentTypes[usedCommentTypeIndex].ID == topic.CommentTypeID)
						{  
						output.Append(usedCommentTypeIndex);
						break;
						}
					}

				output.Append(",");

				if (topic.IsEmbedded == false)
					{  
					output.Append('"');
					output.StringEscapeAndAppend( htmlComponent.BuildWrappedTitle(topic.Title, topic.CommentTypeID) );  
					output.Append('"');
					}
				// Otherwise leave an empty space before the comma.  We don't have to write out "undefined".

				string topicHashPath = HTMLBuilder.Source_TopicHashPath(topic, topicPage.IncludeClassInTopicHashPaths);

				if (topicHashPath != null)
					{
					output.Append(",\"");
					output.StringEscapeAndAppend(topicHashPath);
					output.Append('"');
					}
					
				output.Append(']');

				if (topicIndex < topics.Count - 1)
					{  
					output.Append(',');  

					if (!EngineInstance.Config.ShrinkFiles)
						{
						output.AppendLine();
						output.Append("   ");
						}
					}
				}

			output.Append(']');
			}


		/* Function: BuildSummaryToolTips
		 */
		protected void BuildSummaryToolTips ()
			{
			if (!EngineInstance.Config.ShrinkFiles)
				{
				output.AppendLine();
				output.Append("   ");
				}

			bool first = true;

			for (int topicIndex = 0; topicIndex < topics.Count; topicIndex++)
				{
				Topic topic = topics[topicIndex];

				if (topic.IsEmbedded == false)
					{
					string toolTipHTML = tooltipBuilder.BuildToolTip(topic, links);

					if (toolTipHTML != null)
						{
						if (!first)
							{
							output.Append(',');

							if (!EngineInstance.Config.ShrinkFiles)
								{
								output.AppendLine();
								output.Append("   ");
								}
							}

						output.Append(topic.TopicID);
						output.Append(":\"");
						output.StringEscapeAndAppend(toolTipHTML);
						output.Append('"');

						first = false;
						}
					}
				}
			}



		// Group: Properties
		// __________________________________________________________________________


		/* Property: HTMLBuilder
		 * The <Builders.HTML> associated with this object.
		 */
		public Builders.HTML HTMLBuilder
			{
			get
				{  return topicPage.HTMLBuilder;  }
			}

		/* Property: EngineInstance
		 * The <Engine.Instance> associated with this object.
		 */
		public Engine.Instance EngineInstance
			{
			get
				{  return HTMLBuilder.EngineInstance;  }
			}



		// Group: Variables
		// __________________________________________________________________________


		/* var: topicPage
		 * The <HTMLTopicPage> associated with this summary file.
		 */
		protected HTMLTopicPage topicPage;

		/* var: output
		 * The JavaScript being generated.
		 */
		protected StringBuilder output;

		/* var: fileTitle
		 */
		protected string fileTitle;

		/* var: fileHashPath
		 */
		protected string fileHashPath;

		/* var: topics
		 * The <Topic> list for the file we're building.
		 */
		protected IList<Topic> topics;

		/* var: links
		 * A list of <Links> that includes everything appearing in <topics>.
		 */
		protected IList<Link> links;

		/* var: tooltipBuilder
		 * A <HTML.Components.Tooltip> to build tooltips.
		 */
		protected HTML.Components.Tooltip tooltipBuilder;

		/* var: usedLanguages
		 * A list of the languages used in <topics>.  The order in which they appear here will be the order in which they
		 * appear in the JavaScript array.
		 */
		protected List<Language> usedLanguages;

		/* var: usedCommentTypes
		 * A list of the comment types used in <topics>.  The order in which they appear here will be the order in which they
		 * appear in the JavaScript array.
		 */
		protected List<CommentType> usedCommentTypes;

		/* var: htmlComponent
		 * A private <HTMLComponent> object used for building wrapped titles.
		 */
		protected HTMLComponent htmlComponent;

		}
	}

