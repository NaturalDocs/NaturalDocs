/*
 * Class: CodeClear.NaturalDocs.Engine.Output.HTML.Components.JSONSummary
 * ____________________________________________________________________________
 *
 * A helper class to build JSON summary data for an output page.  It can also save it to a JavaScript file as documented in
 * <JavaScript Summary Data>.
 *
 * Topic: Usage
 *
 *		- Call <ConvertToJSON()> to convert a topic list to JSON.
 *		- If desired, call <BuildDataFile()> to create the output file.
 *		- The object may be reused to convert another topic list.
 *
 * Threading: Not Thread Safe
 *
 *		This class is only designed to be used by one thread at a time.  Each thread should create its own object to use
 *		independently.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2023 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Text;
using CodeClear.NaturalDocs.Engine.Languages;
using CodeClear.NaturalDocs.Engine.CommentTypes;


namespace CodeClear.NaturalDocs.Engine.Output.HTML.Components
	{
	public class JSONSummary : Component
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: JSONSummary
		 */
		public JSONSummary (Context context) : base (context)
			{
			topics = null;
			topicsSummaryJSON = null;

			usedLanguages = new List<Language>();
			usedLanguagesJSON = null;

			usedCommentTypes = new List<CommentType>();
			usedCommentTypesJSON = null;

			formattedTextBuilder = new FormattedText(context);
			addWhitespace = (EngineInstance.Config.ShrinkFiles == false);
			}


		/* Function: ConvertToJSON
		 *
		 * Converts the topic list to JSON.  After calling this function the individual <properties> like <UsedLanguages> and
		 * <CommentTypesJSON> will be available.
		 *
		 * Parameters:
		 *
		 *		topics - The <Engine.Topics.Topics> that appear in this file.
		 *		context - The <Context> of the file.  Must include the page.
		 */
		public void ConvertToJSON (IList<Engine.Topics.Topic> topics, Context context)
			{
			#if DEBUG
			if (context.Page.IsNull)
				{  throw new Exception("The page must be specified when creating a JSONSummary object.");  }
			#endif

			this.context = context;
			formattedTextBuilder.Context = context;

			this.topics = topics;

			addWhitespace = (EngineInstance.Config.ShrinkFiles == false);

			BuildUsedLanguages();
			BuildUsedLanguagesJSON();

			BuildUsedCommentTypes();
			BuildUsedCommentTypesJSON();

			BuildTopicsSummaryJSON();
			}


		/* Function: BuildDataFile
		 *
		 * Takes the JSON created by <ConvertToJSON()> and saves it as a JavaScript file as documented in <JavaScript Summary Data>.
		 * It will use the file name specified in <HTMLTopicsPage.SummaryFile>.  Note that you have to call <ConvertToJSON()> prior to
		 * calling this function.
		 */
		public void BuildDataFile (string pageTitle)
			{
			StringBuilder output = new StringBuilder();

			output.Append(
				"NDFramePage.OnPageTitleLoaded(\"" + context.HashPath.StringEscape() + "\",\"" + pageTitle.StringEscape() + "\");"
				);

			if (addWhitespace)
				{
				output.AppendLine();
				output.AppendLine();
				}

			output.Append("NDSummary.OnSummaryLoaded(\"" + context.HashPath.StringEscape() + "\",");

			if (addWhitespace)
				{  output.AppendLine();  }

			output.Append(usedLanguagesJSON);
			output.Append(',');

			if (addWhitespace)
				{
				output.AppendLine();
				output.AppendLine();
				}

			output.Append(usedCommentTypesJSON);
			output.Append(',');

			if (addWhitespace)
				{
				output.AppendLine();
				output.AppendLine();
				}

			output.Append(topicsSummaryJSON);
			output.Append(");");

			WriteTextFile(context.SummaryFile, output.ToString());
			}



		// Group: Support Functions
		// __________________________________________________________________________


		/* Function: BuildUsedLanguages
		 * Builds <usedLanguages> from <topics>.
		 */
		protected void BuildUsedLanguages ()
			{
			usedLanguages.Clear();

			if (topics.Count == 0)
				{  return;  }


			// Since it's incredibly likely that every topic in the list is the same language, we store lastLanguageID to make an easy
			// comparison so we don't have to search the used languages array every time if we don't have to.

			int lastLanguageID = topics[0].LanguageID;
			usedLanguages.Add( EngineInstance.Languages.FromID(lastLanguageID) );

			for (int i = 1; i < topics.Count; i++)
				{
				if (topics[i].LanguageID == lastLanguageID)
					{  continue;  }

				lastLanguageID = topics[i].LanguageID;
				bool alreadyAdded = false;

				foreach (var usedLanguage in usedLanguages)
					{
					if (usedLanguage.ID == lastLanguageID)
						{
						alreadyAdded = true;
						break;
						}
					}

				if (!alreadyAdded)
					{  usedLanguages.Add( EngineInstance.Languages.FromID(lastLanguageID) );  }
				}


			// Sort used language list.  This isn't strictly necessary but it makes the output more consistent.

			if (usedLanguages.Count > 1)
				{
				usedLanguages.Sort(
					delegate (Language a, Language b)
						{  return string.Compare(a.Name, b.Name);  }
					);
				}
			}


		/* Function: BuildUsedLanguagesJSON
		 *
		 * Builds <usedLanguagesJSON> from <usedLanguages>.  <BuildUsedLanguages()> must be called before this.
		 *
		 * If <Config.Manager.ShrinkFiles> is false, every entry will be indented at least once and appear on its own line.  The final line
		 * will not be followed by a line break.
		 */
		protected void BuildUsedLanguagesJSON ()
			{
			#if DEBUG
			if (usedLanguages == null || (usedLanguages.Count == 0 && topics.Count > 0))
				{  throw new Exception("Can't call BuildUsedLanguagesJSON() without building the language list.");  }
			#endif

			StringBuilder json = new StringBuilder();

			if (addWhitespace)
				{  json.Append(' ', IndentWidth);  }

			json.Append('[');

			if (addWhitespace)
				{  json.AppendLine();  }

			for (int i = 0; i < usedLanguages.Count; i++)
				{
				if (addWhitespace)
					{  json.Append(' ', IndentWidth * 2);  }

				json.Append("[\"");
				json.StringEscapeAndAppend(usedLanguages[i].Name.ToHTML());
				json.Append("\",\"");
				json.StringEscapeAndAppend(usedLanguages[i].SimpleIdentifier);
				json.Append("\"]");

				if (i != usedLanguages.Count - 1)
					{  json.Append(',');  }

				if (addWhitespace)
					{  json.AppendLine();  }
				}

			if (addWhitespace)
				{  json.Append(' ', IndentWidth);  }

			json.Append(']');

			usedLanguagesJSON = json.ToString();
			}


		/* Function: BuildUsedCommentTypes
		 * Builds <usedCommentTypes> from <topics>.
		 */
		protected void BuildUsedCommentTypes ()
			{
			usedCommentTypes.Clear();

			IDObjects.NumberSet usedCommentTypeIDs = new IDObjects.NumberSet();

			foreach (var topic in topics)
				{
				if (!usedCommentTypeIDs.Contains(topic.CommentTypeID))
					{
					usedCommentTypes.Add( EngineInstance.CommentTypes.FromID(topic.CommentTypeID) );
					usedCommentTypeIDs.Add(topic.CommentTypeID);
					}
				}

			// Sort used comment type list.  This isn't strictly necessary but it makes the output more consistent.

			usedCommentTypes.Sort(
				delegate (CommentType a, CommentType b)
					{  return string.Compare(a.Name, b.Name);  }
				);
			}


		/* Function: BuildUsedCommentTypesJSON
		 *
		 * Builds <usedCommentTypesJSON> from <usedCommentTypes>.  <BuildUsedCommentTypes()> must be called before this.
		 *
		 * If <Config.Manager.ShrinkFiles> is false, every entry will be indented at least once and appear on its own line.  The final line
		 * will not be followed by a line break.
		 */
		protected void BuildUsedCommentTypesJSON ()
			{
			#if DEBUG
			if (usedCommentTypes == null || (usedCommentTypes.Count == 0 && topics.Count > 0))
				{  throw new Exception("Can't call BuildUsedCommenttypesJSON() without building the used comment types list.");  }
			#endif

			StringBuilder json = new StringBuilder();

			if (addWhitespace)
				{  json.Append(' ', IndentWidth);  }

			json.Append('[');

			if (addWhitespace)
				{  json.AppendLine();  }

			for (int i = 0; i < usedCommentTypes.Count; i++)
				{
				if (addWhitespace)
					{  json.Append(' ', IndentWidth * 2);  }

				json.Append("[\"");
				json.StringEscapeAndAppend(usedCommentTypes[i].PluralDisplayName.ToHTML());
				json.Append("\",\"");
				json.StringEscapeAndAppend(usedCommentTypes[i].SimpleIdentifier);
				json.Append("\"]");

				if (i != usedCommentTypes.Count - 1)
					{  json.Append(',');  }

				if (addWhitespace)
					{  json.AppendLine();  }
				}

			if (addWhitespace)
				{  json.Append(' ', IndentWidth);  }

			json.Append(']');

			usedCommentTypesJSON = json.ToString();
			}


		/* Function: BuildTopicsSummaryJSON
		 *
		 * Builds <topicsSummaryJSON> from <topics>.  <BuildUsedLanguages()> and <BuildUsedCommentTypes()> must be called
		 * before this.
		 *
		 * If <Config.Manager.ShrinkFiles> is false, every entry will be indented at least once and appear on its own line.  The final line
		 * will not be followed by a line break.
		 */
		protected void BuildTopicsSummaryJSON ()
			{
			#if DEBUG
			if (usedLanguages == null || (usedLanguages.Count == 0 && topics.Count > 0) ||
				usedCommentTypes == null || (usedCommentTypes.Count == 0 && topics.Count > 0))
				{  throw new Exception("Can't call BuildTopicsSummaryJSON() without building the hused languages and used comment types lists.");  }
			#endif

			StringBuilder json = new StringBuilder();

			if (addWhitespace)
				{  json.Append(' ', IndentWidth);  }

			json.Append('[');

			if (addWhitespace)
				{  json.AppendLine();  }

			for (int topicIndex = 0; topicIndex < topics.Count; topicIndex++)
				{
				var topic = topics[topicIndex];
				var topicContext = new Context(context.Target, context.Page, topic);

				if (addWhitespace)
					{  json.Append(' ', IndentWidth * 2);  }


				// Topic ID, Language Index, Comment Type Index

				json.Append('[');
				json.Append(topic.TopicID);
				json.Append(',');

				for (int usedLanguageIndex = 0; usedLanguageIndex < usedLanguages.Count; usedLanguageIndex++)
					{
					if (usedLanguages[usedLanguageIndex].ID == topic.LanguageID)
						{
						json.Append(usedLanguageIndex);
						break;
						}
					}

				json.Append(',');

				for (int usedCommentTypeIndex = 0; usedCommentTypeIndex < usedCommentTypes.Count; usedCommentTypeIndex++)
					{
					if (usedCommentTypes[usedCommentTypeIndex].ID == topic.CommentTypeID)
						{
						json.Append(usedCommentTypeIndex);
						break;
						}
					}

				json.Append(",");


				// Name.  Leave as undefined for embedded topics

				if (topic.IsEmbedded == false)
					{
					var commentType = EngineInstance.CommentTypes.FromID(topic.CommentTypeID);
					FormattedText.WrappedTitleMode mode;

					if (commentType.IsFile)
						{  mode = FormattedText.WrappedTitleMode.File;  }
					else if (commentType.IsCode)
						{  mode = FormattedText.WrappedTitleMode.Code;  }
					else
						{  mode = FormattedText.WrappedTitleMode.None;  }

					json.Append('"');
					json.StringEscapeAndAppend( formattedTextBuilder.BuildWrappedTitle(topic.Title, mode) );
					json.Append('"');
					}
				// Otherwise leave an empty space before the comma.  We don't have to write out "undefined".


				// Hash Path

				string topicHashPath = topicContext.TopicOnlyHashPath;

				if (topicHashPath != null)
					{
					json.Append(",\"");
					json.StringEscapeAndAppend(topicHashPath);
					json.Append('"');
					}

				json.Append(']');

				if (topicIndex < topics.Count - 1)
					{  json.Append(',');  }

				if (addWhitespace)
					{  json.AppendLine();  }
				}

			if (addWhitespace)
				{  json.Append(' ', IndentWidth);  }

			json.Append(']');

			topicsSummaryJSON = json.ToString();
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


		/* Property: TopicsSummaryJSON
		 *
		 * The summary of <Topics> as a JSON array.  It will be in the format described in <JavaScript Summary Data>.
		 *
		 * If <Config.Manager.ShrinkFiles> is false, every entry will be indented at least once and appear on its own line.  The final line
		 * will not be followed by a line break.
		 */
		public string TopicsSummaryJSON
			{
			get
				{  return topicsSummaryJSON;  }
			}


		/* Property: UsedLanguages
		 *
		 * A list of the languages used in <Topics>.
		 */
		public List<Language> UsedLanguages
			{
			get
				{
				return (usedLanguages.Count > 0 ? usedLanguages : null);
				}
			}


		/* Property: UsedLanguagesJSON
		 *
		 * A list of the languages used in <Topics> as a JSON array.  It will be in the format described in <JavaScript Summary Data>.
		 *
		 * If <Config.Manager.ShrinkFiles> is false, every entry will be indented at least once and appear on its own line.  The final line
		 * will not be followed by a line break.
		 */
		public string UsedLanguagesJSON
			{
			get
				{  return usedLanguagesJSON;  }
			}


		/* Property: UsedCommentTypes
		 *
		 * A list of the comment types used in <Topics>.
		 */
		public List<CommentType> UsedCommentTypes
			{
			get
				{
				return (usedCommentTypes.Count > 0 ? usedCommentTypes : null);
				}
			}


		/* Property: UsedCommentTypesJSON
		 *
		 * A list of the comment types used in <Topics> as a JSON array.  It will be in the format described in <JavaScript Summary Data>.
		 *
		 * If <Config.Manager.ShrinkFiles> is false, every entry will be indented at least once and appear on its own line.  The final line
		 * will not be followed by a line break.
		 */
		public string UsedCommentTypesJSON
			{
			get
				{  return usedCommentTypesJSON;  }
			}



		// Group: Variables
		// __________________________________________________________________________


		/* var: topics
		 * The <Engine.Topics.Topics> for the file we're building.
		 */
		protected IList<Engine.Topics.Topic> topics;

		/* var: topicsSummaryJSON
		 * The summary of <topics> as a JSON array, or null if it hasn't been generated yet.  It will be in the format described in
		 * <JavaScript Summary Data>.
		 */
		protected string topicsSummaryJSON;

		/* var: usedLanguages
		 * A list of the languages used in <topics>.  It will be empty if it hasn't been generated yet.
		 */
		protected List<Language> usedLanguages;

		/* var: usedLanguagesJSON
		 * A list of the languages used in <topics> as a JSON array, or null if it hasn't been generated yet.  It will be in the format
		 * described in <JavaScript Summary Data>.
		 */
		protected string usedLanguagesJSON;

		/* var: usedCommentTypes
		 * A list of the comment types used in <topics>.  It will be empty if it hasn't been generated yet.
		 */
		protected List<CommentType> usedCommentTypes;

		/* var: usedCommentTypesJSON
		 * A list of the comment types used in <topics> as a JSON array, or null if it hasn't been generated yet.  It will be in the
		 * format described in <JavaScript Summary Data>.
		 */
		protected string usedCommentTypesJSON;

		/* var: formattedTextBuilder
		 * A <FormattedText> object used for building wrapped titles.
		 */
		protected FormattedText formattedTextBuilder;

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
