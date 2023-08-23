/*
 * Struct: CodeClear.NaturalDocs.Engine.Output.HTML.Context
 * ____________________________________________________________________________
 *
 * A struct that contains the context in which a HTML component is being built, such as which <Topic> it's for and which
 * <PageLocation> it appears in.
 *
 *
 * Multithreading: Not Thread Safe
 *
 *		This class is only designed to be used by one thread at a time.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2023 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using CodeClear.NaturalDocs.Engine.Topics;


namespace CodeClear.NaturalDocs.Engine.Output.HTML
	{
	public struct Context
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: Context
		 * Creates a context with the passed parameters.
		 */
		public Context (HTML.Target target, PageLocation page = default, Topic topic = null)
			{
			this.target = target;
			this.page = page;
			this.topic = topic;
			}

		/* Constructor: Context
		 * Creates a context that includes a source file page.
		 */
		public Context (HTML.Target target, int fileID, Topic topic = null)
			{
			this.target = target;
			this.page = new PageLocation(fileID);
			this.topic = topic;
			}

		/* Constructor: Context
		 * Creates a context that includes a class page.
		 */
		public Context (HTML.Target target, int classID, Symbols.ClassString classString, Topic topic = null)
			{
			#if DEBUG
			if (classString != null && classID == 0)
				{  throw new Exception("Can't create a Context from a class string when its ID isn't known.");  }
			if (classID != 0 && classString == null)
				{  throw new Exception("Can't create a Context from a class ID when its string isn't known.");  }
			#endif

			this.target = target;
			this.page = new PageLocation(classID, classString);
			this.topic = topic;
			}



		// Group: Properties
		// __________________________________________________________________________


		/* Property: Target
		 * The <HTML.Target> associated with this context.
		 */
		public HTML.Target Target
			{
			get
				{  return target;  }
			}


		/* Property: Page
		 * The <PageLocation> associated with this context, or null if it's not relevant.
		 */
		public PageLocation Page
			{
			get
				{  return page;  }
			}


		/* Property: Topic
		 * The <Topic> associated with this context, or null if it's not relevant.
		 */
		public Topic Topic
			{
			get
				{  return topic;  }
			set
				{  topic = value;  }
			}



		// Group: Path Properties
		// __________________________________________________________________________


		/* Property: OutputFile
		 * The output file of the <Page>.
		 */
		public Path OutputFile
			{
			get
				{
				if (target == null || page.IsNull)
					{  throw new NullReferenceException();  }

				if (page.IsSourceFile)
					{
					var file = target.EngineInstance.Files.FromID(page.FileID);
					var fileSource = target.EngineInstance.Files.FileSourceOf(file);

					if (fileSource == null)
						{  throw new InvalidOperationException();  }

					Path relativePath = fileSource.MakeRelative(file.FileName);

					return Paths.SourceFile.OutputFile(target.OutputFolder, fileSource.Number, relativePath);
					}

				else if (page.IsClass)
					{
					var language = target.EngineInstance.Languages.FromID(page.ClassString.LanguageID);
					var hierarchy = target.EngineInstance.Hierarchies.FromID(page.ClassString.HierarchyID);
					return Paths.Class.OutputFile(target.OutputFolder, hierarchy, language, page.ClassString.Symbol);
					}

				else
					{  throw new NotImplementedException();  }
				}
			}


		/* Property: ToolTipsFile
		 * The path of the <Page's> tool tips data file.
		 */
		public Path ToolTipsFile
		   {
			get
				{
				string outputFileString = this.OutputFile.ToString();

				#if DEBUG
				if (!outputFileString.EndsWith(".html"))
					{  throw new Exception("Expected output file path \"" + outputFileString + "\" to end with \".html\".");  }
				#endif

				return outputFileString.Substring(0, outputFileString.Length - 5) + "-ToolTips.js";
				}
			}


		/* Property: SummaryFile
		 * The path of the <Page's> summary data file.
		 */
		public Path SummaryFile
		   {
			get
				{
				string outputFileString = this.OutputFile.ToString();

				#if DEBUG
				if (!outputFileString.EndsWith(".html"))
					{  throw new Exception("Expected output file path \"" + outputFileString + "\" to end with \".html\".");  }
				#endif

				return outputFileString.Substring(0, outputFileString.Length - 5) + "-Summary.js";
				}
			}


		/* Property: SummaryToolTipsFile
		 * The path of the <Page's> summary tool tips data file.
		 */
		public Path SummaryToolTipsFile
		   {
			get
				{
				string outputFileString = this.OutputFile.ToString();

				#if DEBUG
				if (!outputFileString.EndsWith(".html"))
					{  throw new Exception("Expected output file path \"" + outputFileString + "\" to end with \".html\".");  }
				#endif

				return outputFileString.Substring(0, outputFileString.Length - 5) + "-SummaryToolTips.js";
				}
			}


		/* Property: HashPath
		 * The hash path of the <Page>, and if set, the <Topic>.
		 */
		public string HashPath
			{
			get
				{
				// Get the file or class part of the hash path

				string fileHashPath;

				if (target == null || page.IsNull)
					{  throw new NullReferenceException();  }

				if (page.IsSourceFile)
					{
					var file = target.EngineInstance.Files.FromID(page.FileID);
					var fileSource = target.EngineInstance.Files.FileSourceOf(file);

					if (fileSource == null)
						{  throw new InvalidOperationException();  }

					Path relativePath = fileSource.MakeRelative(file.FileName);

					fileHashPath = Paths.SourceFile.HashPath(fileSource.Number, relativePath);
					}

				else if (page.IsClass)
					{
					var language = target.EngineInstance.Languages.FromID(page.ClassString.LanguageID);
					var hierarchy = target.EngineInstance.Hierarchies.FromID(page.ClassString.HierarchyID);
					fileHashPath = Paths.Class.HashPath(hierarchy, language, page.ClassString.Symbol);
					}

				else
					{  throw new NotImplementedException();  }


				// Get the topic hash path if the topic is set

				string topicHashPath = null;

				if (topic != null)
					{  topicHashPath = TopicOnlyHashPath;  }


				if (topicHashPath != null)
					{  return fileHashPath + ':' + topicHashPath;  }
				else
					{  return fileHashPath;  }
				}
			}


		/* Property: TopicOnlyHashPath
		 * Returns only the topic part of the hash path, or null if there is none.  The page must still be set since it affects
		 * how this is generated.
		 */
		public string TopicOnlyHashPath
			{
			get
				{
				if (topic == null)
					{  return null;  }

				else if (page.IsSourceFile)
					{
					// Include the class because it needs to be File:Class.Member
					return Paths.Topic.HashPath(topic, includeClass: true);
					}

				else if (page.IsClass)
					{
					// If we're in a class hierarchy and the topic defines a class, that means we're on the first topic on the
					// page.  We omit the topic part so it can just be Class instead of Class:Class.
					if (topic.DefinesClass)
						{  return null;  }
					else
						{
						// Omit the class so it can be Class:Member.  The class is already part of the file hash path.
						return Paths.Topic.HashPath(topic, includeClass: false);
						}
					}
				else
					{  throw new NotImplementedException();  }
				}
			}



		// Group: Variables
		// __________________________________________________________________________


		/* var: target
		 * The <HTML.Target> associated with this context.
		 */
		private readonly HTML.Target target;

		/* var: page
		 * The <PageLocation> associated with this context, or null if it's not relevant.
		 */
		private readonly PageLocation page;

		/* var: topic
		 * The <Topic> associated with this context, or null if it's not relevant.
		 */
		private Topic topic;

		}
	}
