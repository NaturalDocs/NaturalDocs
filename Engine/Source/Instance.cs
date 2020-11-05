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
 *		- <Styles.Manager> is next because it adds a FileSource to <Files.Manager> for dealing with style files.
 *		
 *		- <Output.Manager> is next because <Output.Targets> may need to be added as <Files.Manager> and <CodeDB.Manager>
 *		  watchers.  They also need to load their styles from <Styles.Manager>.  They can also set the rebuild/reparse flags that 
 *		  <CodeDB.Manager> and <Styles.Manager> need to interpret.
 *		   
 *		- <CodeDB.Manager> needs to be almost last so it can handle anything that can set <Config.Manager.ReparseEverything>
 *		   to true, though it only needs <Config.Manager>.
 *		   
 *		- <Files.Manager> is last because it must be after anything that can set <Config.Manager.ReparseEverything> to true.
 *		   It also depends on <Languages.Manager> to know whether a file's extension is for a supported language or not.
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
		public Instance (Config.Manager configManager = null, 
								CommentTypes.Manager commentTypesManager = null, 
								Languages.Manager languagesManager = null, 
								Comments.Manager commentsManager = null, 
								Links.Manager linksManager = null, 
								CodeDB.Manager codeDBManager = null,
								Styles.Manager stylesManager = null, 
								Output.Manager outputManager = null, 
								Files.Manager filesManager = null)
			{
			startupIssues = StartupIssues.None;
			startupWatchers = new List<IStartupWatcher>();

			this.config = configManager ?? new Config.Manager(this);
			this.commentTypes = commentTypesManager ?? new CommentTypes.Manager(this);
			this.languages = languagesManager ?? new Languages.Manager(this);
			this.comments = commentsManager ?? new Comments.Manager(this);
			this.links = linksManager ?? new Links.Manager(this);
			this.codeDB = codeDBManager ?? new CodeDB.Manager(this);
			this.styles = stylesManager ?? new Styles.Manager(this);
			this.output = outputManager ?? new Output.Manager(this);
			this.files = filesManager ?? new Files.Manager(this);
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

			if (styles != null && !strictRulesApply)
				{
				styles.Dispose();
				styles = null;
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
		 * Attempts to start the engine instance.  Returns whether it was successful, and if it wasn't, puts any errors that prevented
		 * it on the list.  If you wish to try to start it again, <Dispose()> of the instance object and create another one.
		 */
		public bool Start (Errors.ErrorList errors, Config.ProjectConfig commandLineConfig)
			{
			if (config.Start(errors, commandLineConfig) == false)
				{  return false;  }
				
				
			Path gracefulExitFilePath = config.WorkingDataFolder + "/GracefulExit.nd";
			
			if (System.IO.File.Exists(gracefulExitFilePath))
				{  
				startupIssues |= StartupIssues.NeedToStartFresh;
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
				styles.Start(errors) &&
				output.Start(errors) &&
				codeDB.Start(errors) &&
				files.Start(errors)
				);
			}
			
			
		/* Function: Cleanup
		 * 
		 * Assuming everything is up to date, has the engine modules clean up their internal data.  A delegate can be passed
		 * to cancel the process early.
		 * 
		 * It is important to run this process, but it is also important that it only be run when all other operations are complete.
		 * For example, one of the things it does is remove entries and IDs of deleted files.  This can't be done immediately 
		 * because output targets may not have handled the deletions yet and rely on that entry.  Once everything is up to date 
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



		// Group: Crash Handling
		// __________________________________________________________________________
			
			
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

			try { dotNETVersion = Engine.SystemInfo.dotNETVersion; } catch {  }
			try { monoVersion = Engine.SystemInfo.MonoVersion; } catch {  }
			try { osNameAndVersion = Engine.SystemInfo.OSNameAndVersion; } catch {  }
			try { sqliteVersion = Engine.SystemInfo.SQLiteVersion; } catch {  }


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

				if (SystemInfo.MonoVersionTooOld)
					{
					output.AppendLine();
					output.AppendLine( Locale.SafeGet("NaturalDocs.Engine", "CrashReport.OutdatedMono", 
													"You appear to be using a very outdated version of Mono.  This has been known to cause Natural Docs to crash.  Please update it to a more recent version.") );
					output.AppendLine();
					output.AppendLine("   Mono " + monoVersion);
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

				if (osNameAndVersion != null)
					{  output.AppendLine( "   " + osNameAndVersion);  }
				else
					{  output.AppendLine( "   Couldn't get OS name and version");  }

				// There's a possibility of Natural Docs being run through Mono on Windows
				if (Engine.SystemInfo.OnUnix || monoVersion != null)
					{
					if (monoVersion != null)
						{  output.AppendLine( "   Mono " + monoVersion);  }
					else
						{  output.AppendLine( "   Couldn't get Mono version");  }
					}
				else
					{  
					if (dotNETVersion != null)
						{  output.AppendLine("   .NET " + dotNETVersion);  }
					else
						{  output.AppendLine("   Couldn't get .NET version");  }
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
			


		// Group: Startup Tracking Functions
		// __________________________________________________________________________


		/* Function: AddStartupWatcher
		 * Adds an object that wants to be aware of events that occur during initialization.  Call before <Start()>.
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
			foreach (var watcher in startupWatchers)
				{  watcher.OnStartPossiblyLongOperation(operationName);  }
			}


		/* Function: EndPossiblyLongOperation
		 * Called *by module code only* to signify that the possibly long operation previously recorded with <StartPossiblyLongOperation()>
		 * has concluded.
		 */
		public void EndPossiblyLongOperation ()
			{
			foreach (var watcher in startupWatchers)
				{  watcher.OnEndPossiblyLongOperation();  }
			}


		/* Function: AddStartupIssues
		 * 
		 * Called *during engine startup only* to set one or more <StartupIssueFlags>.  These are combined with the existing
		 * <StartupIssues> rather than replacing them, which means flags can be set but they cannot be cleared.  More than one can
		 * be set in a single call so you can pass a combination of flags.  If any of them weren't previously set it will notify the 
		 * <IStartupWatchers> of the changes.
		 * 
		 * You can pass one startup watcher to not be notified, which can be used to prevent a module from receiving its own notification.
		 * If that notification leads to other startup issues being added it will still receive those later notifications.
		 */
		public void AddStartupIssues (StartupIssues newIssues, IStartupWatcher dontNotify = null)
			{
			StartupIssues oldIssues = this.startupIssues;
			StartupIssues combinedIssues = (oldIssues | newIssues);

			if (combinedIssues != oldIssues)
				{
				StartupIssues changedIssues = (combinedIssues & ~oldIssues);

				this.startupIssues = combinedIssues;

				foreach (var watcher in startupWatchers)
					{  
					if (watcher != dontNotify)
						{  watcher.OnStartupIssues(changedIssues, combinedIssues);  }
					}
				}
			}


		/* Function: HasIssues
		 * Returns whether *any* of the passed <StartupIssues> are set to true.  You can pass a combination of flags to this function to test
		 * several at once.  It will only return false if *all* of the passed flags are false.
		 */
		public bool HasIssues (StartupIssues toTest)
			{
			return ( (startupIssues & toTest) != 0 );
			}



		// Group: Constants
		// __________________________________________________________________________
		
		
		/* Constant: VersionString
		 * The current version of the Natural Docs engine as a string.
		 */
		public const string VersionString = "2.2 (Development Release 1)";

				

		// Group: Properties
		// __________________________________________________________________________
		
		
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


		/* Property: StartupIssues
		 * Returns a combination of all <StartupIssueFlags> that apply at the current point of the engine startup.  It may be easier to
		 * use <HasIssues()> than reading this directly.  If you need to be notified of any issues that occur after your module has been
		 * started, implement <IStartupWatcher> and pass it to <AddStartupWatcher()>.
		 */
		public StartupIssues StartupIssues
			{
			get
				{  return startupIssues;  }
			}



		// Group: Modules
		// __________________________________________________________________________
		

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
			
		/* Property: Styles
		 * Returns the <Styles.Manager> associated with this instance.
		 */
		public Styles.Manager Styles
			{
			get
				{  return styles;  }
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
			
			
			
		// Group: Variables
		// __________________________________________________________________________
	

		/* var: startupIssues
		 * 
		 * A set of flags that track issues that can occur during engine startup.
		 * 
		 * Thread Safety:
		 * 
		 *		This variable is not thread safe.  However, it should only be modified during engine initialization which is a single
		 *		threaded process.  Afterwards it becomes read only.
		 */
		protected StartupIssues startupIssues;


		/* var: startupWatchers
		 * 
		 * A list of all the objects that want to observe the engine's initialization.
		 * 
		 * Thread Safety:
		 * 
		 *		This variable is not thread safe.  However, it should only be modified during engine initialization which is a single
		 *		threaded process.  Afterwards it becomes read only.
		 */
		protected List<IStartupWatcher> startupWatchers;



		// Group: Module Variables
		// __________________________________________________________________________

		protected Config.Manager config;
		protected CommentTypes.Manager commentTypes;
		protected Languages.Manager languages;
		protected Comments.Manager comments;
		protected Links.Manager links;
		protected CodeDB.Manager codeDB;
		protected Styles.Manager styles;
		protected Output.Manager output;
		protected Files.Manager files;

		}
	}