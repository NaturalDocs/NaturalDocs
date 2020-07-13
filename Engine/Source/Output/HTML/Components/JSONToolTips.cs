/* 
 * Class: CodeClear.NaturalDocs.Engine.Output.HTML.Components.JSONToolTips
 * ____________________________________________________________________________
 * 
 * A helper class to build JSON tooltip data for an output page.  It can also save it to a JavaScript file as documented in
 * <JavaScript ToolTip Data>.
 * 
 * Topic: Usage
 *		
 *		- Call <ConvertToJSON()> to convert a topic list to JSON.
 *		- If desired, call <BuildDataFileForSummary()> to create the output file.
 *		- The object may be reused to convert another topic list.
 * 
 * Threading: Not Thread Safe
 * 
 *		This class is only designed to be used by one thread at a time.  Each thread should create its own object to use 
 *		independently.
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2020 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Text;


namespace CodeClear.NaturalDocs.Engine.Output.HTML.Components
	{
	public class JSONToolTips : Component
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: JSONToolTips
		 */
		public JSONToolTips (Context context) : base (context)
			{
			topics = null;
			links = null;
			tooltipsJSON = null;

			tooltipBuilder = new Tooltip(context);
			addWhitespace = (EngineInstance.Config.ShrinkFiles == false);
			}


		/* Function: ConvertToJSON
		 * 
		 * Converts the topic list to JSON.  After calling this function <ToolTipsJSON> will be available.
		 * 
		 * Parameters:
		 * 
		 *		topics - The <Engine.Topics.Topics> that appear in this file.
		 *		context - The <Context> of the file.  Must include the topic page.
		 *		links - A list of <Engine.Links.Links> that contain any which will be found in the tooltip.
		 */
		public void ConvertToJSON (IList<Engine.Topics.Topic> topics, IList<Engine.Links.Link> links, Context context)
			{
			#if DEBUG
			if (context.TopicPage == null)
				{  throw new Exception("The topic page must be specified when creating a JSONSummary object.");  }
			#endif

			this.context = context;
			tooltipBuilder.Context = context;

			this.topics = topics;
			this.links = links;

			addWhitespace = (EngineInstance.Config.ShrinkFiles == false);

			BuildToolTipsJSON();
			}


		/* Function: BuildDataFileForSummary
		 * 
		 * Takes the JSON created by <ConvertToJSON()> and saves it as a JavaScript file for summaries as documented in 
		 * <JavaScript ToolTip Data>.  It will use the file name specified in <HTMLTopicsPage.SummaryToolTipsFile>.  Note that you 
		 * have to call <ConvertToJSON()> prior to calling this function.
		 */
		public void BuildDataFileForSummary ()
			{
			StringBuilder output = new StringBuilder();

			output.Append("NDSummary.OnToolTipsLoaded(\"" + context.TopicPage.OutputFileHashPath.StringEscape() + "\",");

			if (addWhitespace)
				{  output.AppendLine();  }

			output.Append(tooltipsJSON);
			output.Append(");");

			System.IO.StreamWriter summaryToolTipsFile = context.Builder.CreateTextFileAndPath(context.TopicPage.SummaryToolTipsFile);

			try
				{  summaryToolTipsFile.Write(output.ToString());  }
			finally
				{  summaryToolTipsFile.Dispose();  }
			}



		// Group: Support Functions
		// __________________________________________________________________________


		/* Function: BuildToolTipsJSON
		 * 
		 * Builds <tooltipsJSON> from <topics>.
		 * 
		 * If <Config.Manager.ShrinkFiles> is false, every entry will be indented at least once and appear on its own line.  The final line
		 * will not be followed by a line break.
		 */
		protected void BuildToolTipsJSON ()
			{
			StringBuilder json = new StringBuilder();

			if (addWhitespace)
				{  json.Append(' ', IndentWidth);  }

			json.Append('{');

			if (addWhitespace)
				{  json.AppendLine();  }

			// Keep track of the first tooltip with this bool because not all topics get tooltips, and thus being at index zero doesn't 
			// necessarily mean we're at the first actual entry.
			bool firstOutputEntry = true;
			
			for (int topicIndex = 0; topicIndex < topics.Count; topicIndex++)
				{
				var topic = topics[topicIndex];

				// Skip embedded topics
				if (topic.IsEmbedded)
					{  continue;  }

				string tooltipHTML = tooltipBuilder.BuildToolTip(topic, context, links);

				// Will be null if the topic doesn't have a prototype or summary, and thus have nothing to show
				if (tooltipHTML == null)
					{  continue;  }

				if (!firstOutputEntry)
					{
					json.Append(',');

					if (addWhitespace)
						{  json.AppendLine();  }
					}

				if (addWhitespace)
					{  json.Append(' ', IndentWidth * 2);  }

				json.Append(topic.TopicID);
				json.Append(':');

				json.Append('"');
				json.StringEscapeAndAppend(tooltipHTML);
				json.Append('"');

				firstOutputEntry = false;
				}

			if (addWhitespace)
				{
				json.AppendLine();
				json.Append(' ', IndentWidth);
				}

			json.Append('}');

			tooltipsJSON = json.ToString();
			}



		// Group: Properties
		// __________________________________________________________________________


		/* Property: Topics
		 * 
		 * The list of <Engine.Topics.Topics> the summary data was built for.
		 */
		public IList<Engine.Topics.Topic> Topics
			{
			get
				{  return topics;  }
			}


		/* Property: ToolTipsJSON
		 * 
		 * The tooltips for <Topics> as a JSON array.  It will be in the format described in <JavaScript ToolTip Data>.
		 * 
		 * If <Config.Manager.ShrinkFiles> is false, every entry will be indented at least once and appear on its own line.  The final line
		 * will not be followed by a line break.
		 */
		public string ToolTipsJSON
			{
			get
				{  return tooltipsJSON;  }
			}



		// Group: Variables
		// __________________________________________________________________________


		/* var: topics
		 * The <Engine.Topics.Topics> for the file we're building.
		 */
		protected IList<Engine.Topics.Topic> topics;

		/* var: links
		 * A list of <Links> that contain any which will appear in the tooltips.
		 */
		protected IList<Engine.Links.Link> links;

		/* var: tooltipsJSON
		 * The tooltips for <topics> as a JSON array, or null if it hasn't been generated yet.  It will be in the format described in
		 * <JavaScript ToolTip Data>.
		 */
		protected string tooltipsJSON;

		/* var: tooltipBuilder
		 * A <HTML.Components.Tooltip> to build tooltips.
		 */
		protected Tooltip tooltipBuilder;

		/* var: addWhitespace
		 * Whether additional whitespace and line breaks should be added to the JSON output to make it more readable.
		 */
		protected bool addWhitespace;



		// Group: Constants
		// __________________________________________________________________________


		/* Constant: IndentWidth
		 * The number of spaces to indent each level by when building the output with extra whitespace.
		 */
		protected const int IndentWidth = 3;

		}
	}

