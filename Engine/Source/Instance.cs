/* 
 * Class: CodeClear.NaturalDocs.Engine.Instance
 * ____________________________________________________________________________
 * 
 * A class for managing the overall Natural Docs engine.
 * 
 * 
 * Usage:
 * 
 *		- Create an instance object.
 *		
 *		- Create a <Config.ProjectConfig> object for the command line parameters.  At minimum you must set the project
 *		  config folder.
 *		
 *		- Call <Start()>.  If it succeeds you can use the engine instance.  If it fails and you want to try again instead of
 *		  exiting the program, you must call <Dispose()> and create a new object.  You cannot reuse it.
 *		  
 *		- Use the engine.
 *		
 *		- Call <Dispose()> passing to it whether you're closing because of the normal event or because of an error.
 *		
 * 
 * Topic: Module Start Order
 * 
 *		It's critical for module code to understand its place in the initialization order so it doesn't call anything later than itself in
 *		its Start() function.  This also serves to document exactly why the order is the way it is.
 *		
 *		- <Config.Manager> is first because almost everything depends on it, such as for its config and working data folder
 *		  properties or for its flag to rebuild everything.
 *		  
 *		- <CommentTypes.Manager> is next.
 *		
 *		- <Languages.Manager> is next because it depends on <CommentTypes.Manager> for the "[Comment Type] Prototype Enders"
 *		  property.
 *		  
 *		- <Comments.Manager> is next though it only needs <Config.Manager> and <CommentTypes.Manager>.
 *		
 *		- <Links.Manager> is next because it needs to be added as a <CodeDB.Manager> and <Files.Manager> watcher.
 *		
 *		- <SearchIndex.Manager> is next because it also needs to be added as a <CodeDB.Manager> watcher.
 *		
 *		- <Output.Manager> is next because <Output.Builders> may need to be added as <Files.Manager>, <CodeDB.Manager>, and 
 *		  <SearchIndex.Manager> watchers.  They can also set the rebuild/reparse flags that CodeDB needs to interpret.
 *		   
 *		- <CodeDB.Manager> needs to be almost last so it can handle anything that can set <Config.Manager.ReparseEverything>
 *		   to true, though it only needs <Config.Manager>.
 *		   
 *		- <Files.Manager> is almost last because it must be after anything that can set <Config.Manager.ReparseEverything> to true.
 *		   It also depends on <Languages.Manager> to know whether a file's extension is for a supported language or not.
 *		  
 *		- <Files.Processor> and <Files.Searcher> are last because they depends on <Files.Manager>.
 *		
 * 
 * File: GracefulExit.nd
 * 
 *		This is a file with no particular content.  It is created when the engine instance starts and deleted if it exits gracefully.  
 *		Therefore the existence of this file on startup means the engine did not exit gracefully last time, possibly because of a 
 *		crash or exception.  This automatically causes <Config.Manager.RebuildEverything> to be set the next time it starts.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2020 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Text;


namespace CodeClear.NaturalDocs.Engine
	{
	public class Instance : IDisposable
		{
		
		// Group: Functions
		// ________________________________________________________________________
		
		
		/* Constructor: Instance
		 * 
		 * Creates the instance so you can access modules like <Config>.  The modules will not be started by this
		 * function.
		 * 
		 * You can optionally pass your own module objects in which allows you to populate the engine with derived classes.
		 * Any left as null will have the default classes created instead.
		 */
		public Instance (Config.Manager configManager = null, CommentTypes.Manager commentTypesManager = null, 
								Languages.Manager languagesManager = null, Comments.Manager commentsManager = null, 
								Links.Manager linksManager = null, CodeDB.Manager codeDBManager = null, 
								Output.Manager outputManager = null, SearchIndex.Manager searchIndexManager = null, 
								Files.Manager filesManager = null, Files.Processor fileProcessor = null,
								Files.Searcher fileSearcher = null)
			{
			startupWatchers = new List<IStartupWatcher>();

			this.config = configManager ?? new Config.Manager(this);
			this.commentTypes = commentTypesManager ?? new CommentTypes.Manager(this);
			this.languages = languagesManager ?? new Languages.Manager(this);
			this.comments = commentsManager ?? new Comments.Manager(this);
			this.links = linksManager ?? new Links.Manager(this);
			this.codeDB = codeDBManager ?? new CodeDB.Manager(this);
			this.output = outputManager ?? new Output.Manager(this);
			this.searchIndex = searchIndexManager ?? new SearchIndex.Manager(this);
			this.files = filesManager ?? new Files.Manager(this);
			this.fileProcessor = fileProcessor ?? new Files.Processor(this);
			this.fileSearcher = fileSearcher ?? new Files.Searcher(this);
			}
			

		~Instance ()
			{
			Dispose(false, true);
			}


		/* Function: Dispose
		 * Shuts down the engine instance.  Pass to it whether it was a graceful shutdown, as opposed to closing because
		 * of an error or exception.
		 */
		public void Dispose (bool graceful)
			{
			Dispose(graceful, false);
			}


		void IDisposable.Dispose ()
			{
			Dispose(false, false);
			}

			
		/* Function: Dispose
		 * Shuts down the engine instance.  Pass to it whether it was a graceful shutdown, as opposed to closing because
		 * of an error or exception.
		 */
		protected void Dispose (bool graceful, bool strictRulesApply)
			{
			if (graceful && !strictRulesApply)
				{
				Path gracefulExitFilePath = config.WorkingDataFolder + "/GracefulExit.nd";
				
				if (System.IO.File.Exists(gracefulExitFilePath))
					{  System.IO.File.Delete(gracefulExitFilePath);  }
				}

			if (output != null && !strictRulesApply)
				{
				output.Dispose();
				output = null;
				}

			if (fileProcessor != null && !strictRulesApply)
				{
				fileProcessor.Dispose();
				fileProcessor = null;
				}
				
			if (fileSearcher != null && !strictRulesApply)
				{
				fileSearcher.Dispose();
				fileSearcher = null;
				}
				
			if (codeDB != null && !strictRulesApply)
				{
				codeDB.Dispose();
				codeDB = null;
				}

			if (files != null && !strictRulesApply)
				{  
				files.Dispose();  
				files = null;
				}
			
			if (searchIndex != null && !strictRulesApply)
				{
				searchIndex.Dispose();					
				searchIndex = null;
				}

			if (links != null && !strictRulesApply)
				{
				links.Dispose();					
				links = null;
				}

			if (comments != null && !strictRulesApply)
				{
				comments.Dispose();					
				comments = null;
				}

			if (languages != null && !strictRulesApply)
				{
				languages.Dispose();					
				languages = null;
				}

			if (commentTypes != null && !strictRulesApply)
				{
				commentTypes.Dispose();					
				commentTypes = null;
				}

			if (config != null && !strictRulesApply)
				{
				config.Dispose();					
				config = null;
				}
			}
			

		/* Function: Start
		 * Attempts to start the engine instance.  Returns whether it was successful, and if it wasn't, puts any errors that 
		 * prevented it on the list.  If you wish to try to start it again, call <Dispose()> and <Create()> first.
		 */
		public bool Start (Errors.ErrorList errors, Config.ProjectConfig commandLineConfig)
			{
			if (config.Start(errors, commandLineConfig) == false)
				{  return false;  }
				
				
			Path gracefulExitFilePath = config.WorkingDataFolder + "/GracefulExit.nd";
			
			if (System.IO.File.Exists(gracefulExitFilePath))
				{  
				config.ReparseEverything = true;
				config.RebuildAllOutput = true;
				}
			else
				{			
				BinaryFile gracefulExitFile = new BinaryFile();
				gracefulExitFile.OpenForWriting(gracefulExitFilePath);
				gracefulExitFile.WriteByte(0);
				gracefulExitFile.Close();
				}
				
				
			return (
				commentTypes.Start(errors) &&
				languages.Start(errors) &&
				comments.Start(errors) &&
				links.Start(errors) &&
				searchIndex.Start(errors) &&
				output.Start(errors) &&
				codeDB.Start(errors) &&
				files.Start(errors) &&
				fileSearcher.Start(errors) &&
				fileProcessor.Start(errors)
				);
			}
			
			
		/* Function: Cleanup
		 * 
		 * Assuming everything is up to date, has the engine modules clean up their internal data.  A delegate can be passed
		 * to cancel the process early.
		 * 
		 * It is important to run this process, but it is also important that it only be run when all other operations are complete.
		 * For example, one of the things it does is remove entries and IDs of deleted files.  This can't be done immediately 
		 * because builders may not have handled the deletions yet and rely on that entry.  Once everything is up to date 
		 * however you can assume nothing else needs them.
		 */
		public void Cleanup (CancelDelegate cancelDelegate)
			{
			if (cancelDelegate())
				{  return;  }
				
			files.Cleanup(cancelDelegate);
			
			if (cancelDelegate())
				{  return;  }
				
			output.Cleanup(cancelDelegate);
			
			if (cancelDelegate())
				{  return;  }
				
			codeDB.Cleanup(cancelDelegate);
			}
			
			
		/* Function: GetCrashInformation
		 * Builds crash information for the passed exception.  It is safe to use even though the program is in an unstable state.
		 */
		public string GetCrashInformation (Exception exception)
			{
			StringBuilder output = new StringBuilder();


			// Gather platform information

			string dotNETVersion = null;
			string monoVersion = null;
			string osNameAndVersion = null;
			string sqliteVersion = null;

			if (Engine.SystemInfo.OnWindows)
				{
				try
					{  dotNETVersion = Engine.SystemInfo.dotNETVersion;  }
				catch
					{  }
				}
			else if (Engine.SystemInfo.OnUnix)
				{
				try
					{  monoVersion = Engine.SystemInfo.MonoVersion;  }
				catch
					{  }
				}
			
			try
				{  osNameAndVersion = Engine.SystemInfo.OSNameAndVersion;  }
			catch
				{  }

			try
				{  sqliteVersion = SQLite.API.LibVersion();  }
			catch
				{  }


			try
				{

				// Crash message

				output.AppendLine( Locale.SafeGet("NaturalDocs.Engine", "CrashReport.Message", "Crash Message") + ':' );
				output.AppendLine();
				output.AppendLine( "   " + exception.Message );


				// Exception type

				Exception inner = exception.InnerException;

				if (exception is Engine.Exceptions.Thread)
					{
					output.AppendLine( "   (" + inner.GetType() + ")" );
					inner = inner.InnerException;
					}
				else
					{
					output.AppendLine( "   (" + exception.GetType() + ")" );
					}


				// Outdated Mono version

				if (monoVersion != null)
					{
					if (monoVersion.StartsWith("0.") || 
						monoVersion.StartsWith("1.") ||
						monoVersion.StartsWith("2.") )
						{
						output.AppendLine();
						output.AppendLine( Locale.SafeGet("NaturalDocs.Engine", "CrashReport.OutdatedMono", 
													 "You appear to be using a very outdated version of Mono.  This has been known to cause Natural Docs to crash.  Please update it to a more recent version.") );
						output.AppendLine();
						output.AppendLine("   Mono " + monoVersion);
						}
					}


				// Natural Docs task

				if (exception.HasNaturalDocsTask())
					{
					output.AppendLine();
					output.AppendLine( Locale.SafeGet("NaturalDocs.Engine", "CrashReport.Task", "Task") + ':' );
					output.AppendLine();

					var tasks = exception.GetNaturalDocsTasks();

					foreach (var task in tasks)
						{  output.AppendLine( "   " + task );  }
					}
				

				// Natural Docs query

				if (exception.HasNaturalDocsQuery())
					{
					output.AppendLine();
					output.AppendLine( Locale.SafeGet("NaturalDocs.Engine", "CrashReport.Query", "Query") + ':' );
					output.AppendLine();

					string query;
					List<string> queryValues;

					exception.GetNaturalDocsQuery(out query, out queryValues);

					output.AppendLine( "   " + query );

					if (queryValues != null && queryValues.Count > 0)
						{
						output.AppendLine();
						output.AppendLine( "   " + Locale.SafeGet("NaturalDocs.Engine", "CrashReport.Values", "Values") + ' ' + 
													 string.Join(", ", queryValues.ToArray()) );
						}
					}


				// Nested exceptions

				while (inner != null)
					{
					output.AppendLine();
					output.AppendLine( Locale.SafeGet("NaturalDocs.Engine", "CrashReport.CausedBy", "Caused By") + ':' );
					output.AppendLine();
					output.AppendLine( "   " + inner.Message );
					output.AppendLine( "   (" + inner.GetType() + ")" );
					
					if (inner.HasNaturalDocsTask())
						{
						output.AppendLine();
						output.AppendLine( Locale.SafeGet("NaturalDocs.Engine", "CrashReport.Task", "Task") + ':' );
						output.AppendLine();

						var tasks = inner.GetNaturalDocsTasks();

						foreach (var task in tasks)
							{  output.AppendLine( "   " + task );  }
						}

					if (exception.HasNaturalDocsQuery())
						{
						output.AppendLine();
						output.AppendLine( Locale.SafeGet("NaturalDocs.Engine", "CrashReport.Query", "Query") + ':' );
						output.AppendLine();

						string query;
						List<string> queryValues;

						exception.GetNaturalDocsQuery(out query, out queryValues);

						output.AppendLine( "   " + query );

						if (queryValues != null && queryValues.Count > 0)
							{
							output.AppendLine();
							output.AppendLine( "   " + Locale.SafeGet("NaturalDocs.Engine", "CrashReport.Values", "Values") + ' ' + 
														 string.Join(", ", queryValues.ToArray()) );
							}
						}

					inner = inner.InnerException;
					}


				// Stack trace
					
				output.AppendLine();
				output.AppendLine( Locale.SafeGet("NaturalDocs.Engine", "CrashReport.StackTrace", "Stack Trace") + ':' );
				output.AppendLine();
				output.AppendLine( exception.StackTrace );


				// Command Line

				output.AppendLine ();
				output.AppendLine ( Locale.SafeGet("NaturalDocs.Engine", "CrashReport.CommandLine", "Command Line") + ':' );
				output.AppendLine ();
				output.AppendLine ("   " + Environment.CommandLine );


				// Versions

				output.AppendLine ();
				output.AppendLine ( Locale.SafeGet("NaturalDocs.Engine", "CrashReport.Versions", "Versions") + ':' );
				output.AppendLine ();
				output.AppendLine ( "   Natural Docs " + Instance.VersionString );
				output.AppendLine ();

				if (osNameAndVersion != null)
					{  output.AppendLine( "   " + osNameAndVersion);  }
				else
					{  output.AppendLine( "   Couldn't get OS name and version");  }

				if (Engine.SystemInfo.OnWindows) 
					{  
					if (dotNETVersion != null)
						{  output.AppendLine("   .NET " + dotNETVersion);  }
					else
						{  output.AppendLine("   Couldn't get .NET version");  }
					}
				else if (Engine.SystemInfo.OnUnix)
					{
					if (monoVersion != null)
						{  output.AppendLine( "   Mono " + monoVersion);  }
					else
						{  output.AppendLine( "   Couldn't get Mono version");  }
					}

				if (sqliteVersion != null)
					{  output.AppendLine( "   SQLite " + sqliteVersion);  }
				else
					{  output.AppendLine ( "   Couldn't get SQLite version" );  }
				}
				
			// If the information building crashes out at any time, that's fine.  We'll just return what we managed to build before that happened.
			catch
				{  }
				
			return output.ToString();
			}
			

		/* Function: BuildCrashReport
		 * 
		 * Attempts to build a crash report for the passed exception.  If it succeeds it will return the path to the file,
		 * otherwise it will return null.  It is safe to use even though the program is in an unstable state.  It will 
		 * simply eat any exceptions it generates trying to create the report and return null instead.  If it's not able
		 * to generate the report you should display <GetCrashInformation()> instead.
		 */
		public Path BuildCrashReport (Exception e)
			{
			System.IO.StreamWriter crashReport = null;
			Path filePath = null;
			
			try
				{
				if (config == null || String.IsNullOrEmpty(config.ProjectConfigFolder))
					{  return null;  }
					
				filePath = config.WorkingDataFolder + "/LastCrash.txt";
				crashReport = System.IO.File.CreateText(filePath);

				crashReport.WriteLine( Locale.SafeGet("NaturalDocs.Engine", "CrashReport.GeneratedOn(date)", "Generated on {0}",
																		 DateTime.Now ) );
				crashReport.WriteLine();				
				crashReport.Write( GetCrashInformation(e) );
				}
				
			catch
				{  return null;  }
				
			finally
				{
				if (crashReport != null)
					{  crashReport.Close();  }
				}
				
			return filePath;
			}
			


		// Group: Startup Event Functions
		// __________________________________________________________________________


		/* Function: AddStartupWatcher
		 * Adds an object that wants to be aware of events that occur during initialization.  Call after <Create()> but before <Start()>.
		 */
		public void AddStartupWatcher (IStartupWatcher watcher)
			{
			startupWatchers.Add(watcher);
			}

		/* Function: StartPossiblyLongOperation
		 * Called *by module code only* to signify that a possibly long operation is about to begin.  The operation name is arbitrary but 
		 * should be documented in <IStartupWatcher.OnStartPossiblyLongOperation>.  Every call should be matched with a
		 * <EndPossiblyLongOperation()> call, and it is up to the module code to make sure the calls are properly paired and 
		 * non-overlapping.
		 */
		public void StartPossiblyLongOperation (string operationName)
			{
			foreach (IStartupWatcher watcher in startupWatchers)
				{  watcher.OnStartPossiblyLongOperation(operationName);  }
			}

		/* Function: EndPossiblyLongOperation
		 * Called *by module code only* to signify that the possibly long operation previously recorded with <StartPossiblyLongOperation()>
		 * has concluded.
		 */
		public void EndPossiblyLongOperation ()
			{
			foreach (IStartupWatcher watcher in startupWatchers)
				{  watcher.OnEndPossiblyLongOperation();  }
			}



		// Group: Constants and Properties
		// __________________________________________________________________________
		
		
		/* Constant: VersionString
		 * The current version of the Natural Docs engine as a string.
		 */
		public const string VersionString = "2.1 (Development Release 1)";
		
		
		/* Property: Version
		 * The current version of the Natural Docs engine.
		 */
		public static Version Version
			{
			get
				{
				Version version = VersionString;
				return version;
				}
			}
			
	
		/* Property: Config
		 * Returns the <Config.Manager> associated with this instance.
		 */
		public Config.Manager Config
			{
			get
				{  return config;  }
			}
			
		/* Property: CommentTypes
		 * Returns the <CommentTypes.Manager> associated with this instance.
		 */
		public CommentTypes.Manager CommentTypes
			{
			get
				{  return commentTypes;  }
			}
			
		/* Property: Languages
		 * Returns the <Languages.Manager> associated with this instance.
		 */
		public Languages.Manager Languages
			{
			get
				{  return languages;  }
			}
			
		/* Property: Comments
		 * Returns the <Comments.Manager> associated with this instance.
		 */
		public Comments.Manager Comments
			{
			get
				{  return comments;  }
			}
			
		/* Property: Links
		 * Returns the <Links.Manager> associated with this instance.
		 */
		public Links.Manager Links
			{
			get
				{  return links;  }
			}
			
		/* Property: CodeDB
		 * Returns the <CodeDB.Manager> associated with this instance.
		 */
		public CodeDB.Manager CodeDB
			{
			get
				{  return codeDB;  }
			}
			
		/* Property: SearchIndex
		 * Returns the <SearchIndex.Manager> associated with this instance.
		 */
		public SearchIndex.Manager SearchIndex
			{
			get
				{  return searchIndex;  }
			}

		/* Property: Output
		 * Returns the <Output.Manager> associated with this instance.
		 */
		public Output.Manager Output
			{
			get
				{  return output;  }
			}

		/* Property: Files
		 * Returns the <Files.Manager> associated with this instance.
		 */
		public Files.Manager Files
			{
			get
				{  return files;  }
			}
			
		/* Property: FileProcessor
		 * Returns the <Files.Processor> associated with this instance.
		 */
		public Files.Processor FileProcessor
			{
			get
				{  return fileProcessor;  }
			}
			
		/* Property: FileSearcher
		 * Returns the <Files.Searcher> associated with this instance.
		 */
		public Files.Searcher FileSearcher
			{
			get
				{  return fileSearcher;  }
			}
			
			
			
		// Group: Variables
		// __________________________________________________________________________
	
		/* var: startupWatchers
		 * A list of all the objects that want to observe the engine's initialization.
		 */
		protected List<IStartupWatcher> startupWatchers;

		protected Config.Manager config;
		
		protected CommentTypes.Manager commentTypes;
		
		protected Languages.Manager languages;
		
		protected Comments.Manager comments;

		protected Links.Manager links;
		
		protected CodeDB.Manager codeDB;
		
		protected SearchIndex.Manager searchIndex;

		protected Output.Manager output;
			
		protected Files.Manager files;

		protected Files.Processor fileProcessor;

		protected Files.Searcher fileSearcher;

		}
	}