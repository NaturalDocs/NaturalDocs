/* 
 * Class: GregValure.NaturalDocs.Engine.Output.Builders.HTMLSummary
 * ____________________________________________________________________________
 * 
 * A reusable helper class to build JavaScript summary data for <Output.Builders.HTML>.
 * 
 * Topic: Usage
 *		
 *		- Create a HTMLSummary object.
 *		- Call <Build()>.
 *		- The object can be reused on different <Topics> by calling <Build()> again.
 * 
 * Threading: Not Thread Safe
 * 
 *		This class is only designed to be used by one thread at a time.  It has an internal state that is used during a call to
 *		<Build()>, and another <Build()> should not be started until it's completed.  Instead each thread should create its 
 *		own object.
 * 
 * 
 * File: Summary.js
 * 
 *		Each source file that has a content file built for it will also have a JavaScript summary file.  It's in the same location
 *		and has the same file name, only ending with -Summary.js instead of .html.
 *		
 *		Page Title:
 *		
 *			When executed, it will pass the source file's title to <NDFramePage.OnPageTitleLoaded()>.
 *			
 *		Summary:
 *		
 *			When executed, it will also pass the source file's summary to <NDSummary.OnSummaryLoaded()>.
 *			
 *			Summary languages is an array of the languages used in the summary.  They appear in the order in which they should
 *			appear in the output.  Each language is an array with these members:
 *			
 *				nameHTML - The name of the language in HTML.
 *				simpleIdentifier - A simplified version of the name which can be used in CSS classes.
 *				
 *			Summary topic types is an array of the topic types used in the summary.  They appear in the order in which they should
 *			appear in the output.  Each topic type is an array with these members:
 *			
 *				pluralNameHTML - The plural name of the language in HTML.
 *				simpleIdentifier - A simplified version of the name which can be used in CSS classes.
 *			
 *			Summary entries is an array of entries, each of which is an array with these members:
 *			
 *				topicID - A numeric ID for the topic, unique across the whole project.
 *				languageIndex - The topic's language as an index into the languages array.
 *				topicTypeIndex - The topic type as an index into the topic types array.
 *				nameHTML - The name of the topic in HTML.
 *				symbol - The topic's symbol in the hash path.
 *				
 * 
 * File: SummaryToolTips.js
 * 
 *		Each source file that contains content will also have a summary tooltips file which uses the same path, only ending
 *		in -SummaryToolTips.js instead of .html.  This is separate from <Summary.js> so that it can load and the file summary
 *		can be rendered in the browser immediately.  The tooltips can get large so they're loaded in a separate file only after
 *		the summary has been rendered.
 *		
 *		When executed, this file calls <NDSummary.OnSummaryToolTipsLoaded()>.  The tooltips are an object mapping the
 *		topic IDs to a HTML tooltip.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2012 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Text;
using GregValure.NaturalDocs.Engine.Collections;
using GregValure.NaturalDocs.Engine.Languages;
using GregValure.NaturalDocs.Engine.TopicTypes;


namespace GregValure.NaturalDocs.Engine.Output.Builders
	{
	public class HTMLSummary
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: HTMLSummary
		 */
		public HTMLSummary (Builders.HTML htmlBuilder)
			{
			this.htmlBuilder = htmlBuilder;

			output = null;
			fileTitle = null;
			fileHashPath = null;
			topics = null;

			htmlTopic = new HTMLTopic(htmlBuilder);
			usedLanguages = new List<Language>();
			usedTopicTypes = new List<TopicType>();
			}


		/* Function: Build
		 * Builds the JavaScript metadata for the <Topic> and appends it to the passed StringBuilder.
		 */
		public void Build (IList<Topic> topics, string fileTitle, string fileHashPath, Path summaryPath, Path summaryToolTipsPath)
			{
			this.output = new StringBuilder();
			this.fileTitle = fileTitle;
			this.fileHashPath = fileHashPath;
			this.topics = topics;
			
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


			System.IO.StreamWriter summaryFile = htmlBuilder.CreateTextFileAndPath(summaryPath);

			try
				{  summaryFile.Write(output.ToString());  }
			finally
				{  summaryFile.Dispose();  }


			// SummaryToolTips.js

			output.Remove(0, output.Length);

			output.Append(
				"NDSummary.OnSummaryToolTipsLoaded(\"" + fileHashPath.StringEscape() + "\","
				);

			BuildSummaryToolTips();

			#if DONT_SHRINK_FILES
				output.AppendLine();
				output.Append("   ");
			#endif

			output.Append(");");

			System.IO.StreamWriter summaryToolTipsFile = htmlBuilder.CreateTextFileAndPath(summaryToolTipsPath);

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

				output.Append(",\"");
				output.StringEscapeAndAppend( htmlBuilder.BuildWrappedTitle(topic.Title, topic.TopicTypeID) );
				output.Append("\",\"");
				output.StringEscapeAndAppend( Builders.HTML.Source_TopicHashPath(topic, true) );
				output.Append("\"]");

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

			output.Append('{');
			bool first = true;

			for (int topicIndex = 0; topicIndex < topics.Count; topicIndex++)
				{
				Topic topic = topics[topicIndex];
				string toolTipHTML = htmlTopic.BuildToolTip(topic);

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

			output.Append('}');
			}



		// Group: Variables
		// __________________________________________________________________________


		/* var: htmlBuilder
		 * The parent <Output.Builders.HTML> object.
		 */
		protected Builders.HTML htmlBuilder;

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

