/* 
 * Class: GregValure.NaturalDocs.Engine.Output.Components.JSSummaryData
 * ____________________________________________________________________________
 * 
 * A reusable helper class to build JavaScript summary data for <Output.Builders.HTML>.  See <JavaScript Summary Data>
 * and <JavaScript ToolTip Data> for the output formats.
 * 
 * Topic: Usage
 *		
 *		- Create a JSSummaryData object.
 *		- Call <Build()>.
 *		- The object can be reused on different <Topics> by calling <Build()> again.
 * 
 * Threading: Not Thread Safe
 * 
 *		This class is only designed to be used by one thread at a time.  It has an internal state that is used during a call to
 *		<Build()>, and another <Build()> should not be started until it's completed.  Instead each thread should create its 
 *		own object.
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2013 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Text;
using GregValure.NaturalDocs.Engine.Collections;
using GregValure.NaturalDocs.Engine.Languages;
using GregValure.NaturalDocs.Engine.Links;
using GregValure.NaturalDocs.Engine.Topics;
using GregValure.NaturalDocs.Engine.TopicTypes;


namespace GregValure.NaturalDocs.Engine.Output.Components
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

			htmlTopic = new HTMLTopic(topicPage);
			usedLanguages = new List<Language>();
			usedTopicTypes = new List<TopicType>();
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
			usedTopicTypes.Clear();


			// Summary.js

			output.Append(
				"NDFramePage.OnPageTitleLoaded(\"" + fileHashPath.StringEscape() + "\",\"" + fileTitle.StringEscape() + "\");"
				);

			#if DONT_SHRINK_FILES
				output.AppendLine();
				output.AppendLine();
			#endif

			output.Append("NDSummary.OnSummaryLoaded(\"" + fileHashPath.StringEscape() + "\",");

			BuildLanguageList();
			output.Append(',');

			#if DONT_SHRINK_FILES
				output.AppendLine();
			#endif

			BuildTopicTypeList();
			output.Append(',');

			#if DONT_SHRINK_FILES
				output.AppendLine();
			#endif

			BuildSummaryEntries();

			#if DONT_SHRINK_FILES
				output.AppendLine();
				output.Append("   ");
			#endif

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

			#if DONT_SHRINK_FILES
				output.AppendLine();
				output.Append("   ");
			#endif

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
					{  usedLanguages.Add( Engine.Instance.Languages.FromID(topic.LanguageID) );  }
				}


			// Sort used language list

			usedLanguages.Sort(
				delegate (Language a, Language b)
					{  return string.Compare(a.Name, b.Name);  }
				);

			
			// Build JavaScript

			#if DONT_SHRINK_FILES
				output.AppendLine();
				output.Append("   ");
			#endif

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

					#if DONT_SHRINK_FILES
						output.AppendLine();
						output.Append("   ");
					#endif
					}
				}

			output.Append(']');
			}


		/* Function: BuildTopicTypeList
		 */
		protected void BuildTopicTypeList ()
			{
			// Build used topic type list

			foreach (Topic topic in topics)
				{
				bool found = false;

				for (int i = 0; found == false && i < usedTopicTypes.Count; i++)
					{
					if (usedTopicTypes[i].ID == topic.TopicTypeID)
						{  found = true;  }
					}

				if (!found)
					{  usedTopicTypes.Add( Engine.Instance.TopicTypes.FromID(topic.TopicTypeID) );  }
				}


			// Sort used topic type list

			usedTopicTypes.Sort(
				delegate (TopicType a, TopicType b)
					{  return string.Compare(a.Name, b.Name);  } // xxx should be by topics.txt order
				);


			// Build JavaScript output

			#if DONT_SHRINK_FILES
				output.AppendLine();
				output.Append("   ");
			#endif

			output.Append('[');

			for (int i = 0; i < usedTopicTypes.Count; i++)
				{
				output.Append("[\"");
				output.StringEscapeAndAppend(usedTopicTypes[i].PluralDisplayName.ToHTML());
				output.Append("\",\"");
				output.StringEscapeAndAppend(usedTopicTypes[i].SimpleIdentifier);
				output.Append("\"]");

				if (i != usedTopicTypes.Count - 1)
					{  
					output.Append(',');  
					
					#if DONT_SHRINK_FILES
						output.AppendLine();
						output.Append("   ");
					#endif
					}
				}

			output.Append(']');
			}


		/* Function: BuildSummaryEntries
		 */
		protected void BuildSummaryEntries ()
			{
			#if DONT_SHRINK_FILES
				output.AppendLine();
				output.Append("   ");
			#endif

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

				for (int usedTopicTypeIndex = 0; usedTopicTypeIndex < usedTopicTypes.Count; usedTopicTypeIndex++)
					{
					if (usedTopicTypes[usedTopicTypeIndex].ID == topic.TopicTypeID)
						{  
						output.Append(usedTopicTypeIndex);
						break;
						}
					}

				output.Append(",");

				if (topic.IsEmbedded == false)
					{  
					output.Append('"');
					output.StringEscapeAndAppend( HTMLComponent.BuildWrappedTitle(topic.Title, topic.TopicTypeID) );  
					output.Append('"');
					}
				// Otherwise leave an empty space before the comma.  We don't have to write out "undefined".

				string topicHashPath = Builders.HTML.Source_TopicHashPath(topic, topicPage.IncludeClassInTopicHashPaths);

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

					#if DONT_SHRINK_FILES
						output.AppendLine();
						output.Append("   ");
					#endif
					}
				}

			output.Append(']');
			}


		/* Function: BuildSummaryToolTips
		 */
		protected void BuildSummaryToolTips ()
			{
			#if DONT_SHRINK_FILES
				output.AppendLine();
				output.Append("   ");
			#endif

			bool first = true;

			for (int topicIndex = 0; topicIndex < topics.Count; topicIndex++)
				{
				Topic topic = topics[topicIndex];

				if (topic.IsEmbedded == false)
					{
					string toolTipHTML = htmlTopic.BuildToolTip(topic, links);

					if (toolTipHTML != null)
						{
						if (!first)
							{
							output.Append(',');

							#if DONT_SHRINK_FILES
								output.AppendLine();
								output.Append("   ");
							#endif
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

		/* var: htmlTopic
		 * A <HTMLTopic> to build tooltips.
		 */
		protected HTMLTopic htmlTopic;

		/* var: usedLanguages
		 * A list of the languages used in <topics>.  The order in which they appear here will be the order in which they
		 * appear in the JavaScript array.
		 */
		protected List<Language> usedLanguages;

		/* var: usedTopicTypes
		 * A list of the topic types used in <topics>.  The order in which they appear here will be the order in which they
		 * appear in the JavaScript array.
		 */
		protected List<TopicType> usedTopicTypes;

		}
	}

