/* 
 * Struct: CodeClear.NaturalDocs.Engine.Output.HTML.Context
 * ____________________________________________________________________________
 * 
 * A struct that contains the context in which a HTML component is being built, such as which <Topic> it's for and which
 * <HTMLTopicPage> it appears in.
 * 
 * 
 * Multithreading: Not Thread Safe
 * 
 *		This class is only designed to be used by one thread at a time.
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2020 Code Clear LLC.
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
		public Context (Builders.HTML builder, TopicPage topicPage = default, Topic topic = null)
			{
			this.builder = builder;
			this.topicPage = topicPage;
			this.topic = topic;
			}

		/* Constructor: Context
		 * Creates a context that includes a source file topic page.
		 */
		public Context (Builders.HTML builder, int fileID, Topic topic = null)
			{
			this.builder = builder;
			this.topicPage = new TopicPage(fileID);
			this.topic = topic;
			}

		/* Constructor: Context
		 * Creates a context that includes a topic page in a class hierarchy.
		 */
		public Context (Builders.HTML builder, int classID, Symbols.ClassString classString, Topic topic = null)
			{
			#if DEBUG
			if (classString != null && classID == 0)
				{  throw new Exception("Can't create a Context from a class string when its ID isn't known.");  }
			if (classID != 0 && classString == null)
				{  throw new Exception("Can't create a Context from a class ID when its string isn't known.");  }
			#endif

			this.builder = builder;
			this.topicPage = new TopicPage(classID, classString);
			this.topic = topic;
			}



		// Group: Properties
		// __________________________________________________________________________


		/* Property: Builder
		 * The <Builders.HTML> associated with this context.
		 */
		public Builders.HTML Builder
			{
			get
				{  return builder;  }
			}


		/* Property: TopicPage
		 * The <TopicPage> associated with this context, or null if it's not relevant.
		 */
		public TopicPage TopicPage
			{
			get
				{  return topicPage;  }
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
		 * The output file of the <TopicPage>.
		 */
		public Path OutputFile
			{
			get
				{
				if (builder == null || topicPage.IsNull)
					{  throw new NullReferenceException();  }

				if (topicPage.IsSourceFile)
					{
					var file = builder.EngineInstance.Files.FromID(topicPage.FileID);
					var fileSource = builder.EngineInstance.Files.FileSourceOf(file);

					if (fileSource == null)
						{  throw new InvalidOperationException();  }

					Path relativePath = fileSource.MakeRelative(file.FileName);

					return Paths.SourceFile.OutputFile(builder.OutputFolder, fileSource.Number, relativePath);
					}

				else if (topicPage.IsClass)
					{
					var language = builder.EngineInstance.Languages.FromID(topicPage.ClassString.LanguageID);
					return Paths.Class.OutputFile(builder.OutputFolder, language.SimpleIdentifier, topicPage.ClassString.Symbol);
					}

				else if (topicPage.IsDatabase)
					{
					return Paths.Database.OutputFile(builder.OutputFolder, topicPage.ClassString.Symbol);
					}

				else
					{  throw new NotImplementedException();  }
				}
			}


		/* Property: ToolTipsFile
		 * The path of the <TopicPage's> tool tips data file.
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
		 * The path of the <TopicPage's> summary data file.
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
		 * The path of the <TopicPage's> summary tool tips data file.
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
		 * The hash path of the <TopicPage>, and if set, the <Topic>.
		 */
		public string HashPath
			{
			get
				{
				// Get the file or class part of the hash path

				string fileHashPath;

				if (builder == null || topicPage.IsNull)
					{  throw new NullReferenceException();  }

				if (topicPage.IsSourceFile)
					{
					var file = builder.EngineInstance.Files.FromID(topicPage.FileID);
					var fileSource = builder.EngineInstance.Files.FileSourceOf(file);

					if (fileSource == null)
						{  throw new InvalidOperationException();  }

					Path relativePath = fileSource.MakeRelative(file.FileName);

					fileHashPath = Paths.SourceFile.HashPath(fileSource.Number, relativePath);
					}

				else if (topicPage.IsClass)
					{
					var language = builder.EngineInstance.Languages.FromID(topicPage.ClassString.LanguageID);
					fileHashPath = Paths.Class.HashPath(language.SimpleIdentifier, topicPage.ClassString.Symbol);
					}

				else if (topicPage.IsDatabase)
					{
					fileHashPath = Paths.Database.HashPath(topicPage.ClassString.Symbol);
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
		 * Returns only the topic part of the hash path, or null if there is none.  The topic page must still be set since it affects
		 * how this is generated.
		 */
		public string TopicOnlyHashPath
			{
			get
				{
				if (topic == null)
					{  return null;  }

				else if (topicPage.IsSourceFile)
					{
					// Include the class because it needs to be File:Class.Member
					return Paths.Topic.HashPath(topic, includeClass: true);
					}

				else if (topicPage.InHierarchy)
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


		/* var: builder
		 * The <Builders.HTML> being built for.
		 */
		private readonly Builders.HTML builder;

		/* var: topicPage
		 * The <TopicPage> being built for, or null if it's not relevant.
		 */
		private readonly TopicPage topicPage;

		/* var: topic
		 * The <Topic> being built for, or null if it's not relevant.
		 */
		private Topic topic;

		}
	}
