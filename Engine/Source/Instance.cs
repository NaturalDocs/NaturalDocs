/* 
 * Class: GregValure.NaturalDocs.Engine.Instance
 * ____________________________________________________________________________
 * 
 * A class for managing the overall Natural Docs engine.
 * 
 * 
 * Usage:
 * 
 *		- Call <Create()>.
 *		
 *		- Set any properties you need prior to start, such as <Config.Manager.ProjectConfigFolder>.
 *		
 *		- Call <Start()>.  If it succeeds you can use the engine instance.  If it fails and you want to try again instead of
 *		  exiting the program, you must call <Dispose()> and <Create()> first.
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
 *		- <TopicTypes.Manager> is next.
 *		
 *		- <Languages.Manager> is next because it depends on <TopicTypes.Manager> for the "[Topic Type] Prototype Enders"
 *		  property.
 *		  
 *		- <Comments.Manager> is next though it only needs <Config.Manager> and <TopicTypes.Manager>.
 *		
 *		- <Output.Manager> is next because all <Output.Builders> need to be added as <CodeDB.Manager> watchers.  It can
 *		   also set the rebuild/reparse flags that CodeDB needs to interpret.
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

// This file is part of Natural Docs, which is Copyright © 2003-2011 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;


namespace GregValure.NaturalDocs.Engine
	{
	static public class Instance
		{
		
		// Group: Functions
		// ________________________________________________________________________
		
		
		/* Function: Create
		 * 
		 * Creates the instance so you can access modules like <Config>.  The modules will not be started by this
		 * function.
		 * 
		 * You can optionally pass your own module objects in which allows you to populate the engine with derived classes.
		 * Any left as null will have the default classes created instead.
		 */
		static public void Create (Config.Manager configManager = null, TopicTypes.Manager topicTypesManager = null, 
														 Languages.Manager languagesManager = null, Comments.Manager commentsManager = null, 
														 CodeDB.Manager codeDBManager = null, Output.Manager outputManager = null,
														 Files.Manager filesManager = null)
			{
			startupWatchers = new List<IStartupWatcher>();

			config = configManager ?? new Config.Manager();
			topicTypes = topicTypesManager ?? new TopicTypes.Manager();
			languages = languagesManager ?? new Languages.Manager();
			comments = commentsManager ?? new Comments.Manager();
			codeDB = codeDBManager ?? new CodeDB.Manager();
			output = outputManager ?? new Output.Manager();
			files = filesManager ?? new Files.Manager();
			}
			
			
		/* Function: Dispose
		 * Shuts down the engine instance.  Pass to it whether it was a graceful shutdown, as opposed to closing because
		 * of an error or exception.
		 */
		static public void Dispose (bool graceful)
			{
			if (graceful)
				{
				Path gracefulExitFilePath = config.WorkingDataFolder + "/GracefulExit.nd";
				
				if (System.IO.File.Exists(gracefulExitFilePath))
					{  System.IO.File.Delete(gracefulExitFilePath);  }
				}

			if (output != null)
				{
				output.Dispose();
				output = null;
				}
				
			if (codeDB != null)
				{
				codeDB.Dispose();
				codeDB = null;
				}

			if (files != null)
				{  
				files.Dispose();  
				files = null;
				}
								
			comments = null;
			languages = null;
			topicTypes = null;
			config = null;
			}
			

		/* Function: Start
		 * Attempts to start the engine instance.  Returns whether it was successful, and if it wasn't, puts any errors that 
		 * prevented it on the list.  If you wish to try to start it again, call <Dispose()> and <Create()> first.
		 */
		static public bool Start (Errors.ErrorList errors)
			{
			if (config.Start(errors) == false)
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
				topicTypes.Start(errors) &&
				languages.Start(errors) &&
				comments.Start(errors) &&
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
		 * because builders may not have handled the deletions yet and rely on that entry.  Once everything is up to date 
		 * however you can assume nothing else needs them.
		 */
		static public void Cleanup (CancelDelegate cancelDelegate)
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
			
			
		/* Function: BuildCrashReport
		 * 
		 * Attempts to build a crash report for the passed exception.  If it succeeds it will return the path to the file,
		 * otherwise it will return null.  It is safe to use even though the program is in an unstable state.  It will 
		 * simply eat any exceptions it generates trying to create the report and return null instead.  Since it may not
		 * be able to generate the report, you should have a backup method of displaying the information.
		 */
		static public Path BuildCrashReport (Exception e)
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
				crashReport.WriteLine( Locale.SafeGet("NaturalDocs.Engine", "CrashReport.Message", "Crash Message:") );
				crashReport.WriteLine();
				crashReport.WriteLine( "   " + e.Message );

				Exception inner = e.InnerException;

				if (e is Engine.Exceptions.Thread)
					{
					crashReport.WriteLine( "   (" + inner.GetType() + ")" );
					inner = inner.InnerException;
					}
				else
					{
					crashReport.WriteLine( "   (" + e.GetType() + ")" );
					}
				
				while (inner != null)
					{
					crashReport.WriteLine();
					crashReport.WriteLine( Locale.SafeGet("NaturalDocs.Engine", "CrashReport.CausedBy", "Caused By:") );
					crashReport.WriteLine();
					crashReport.WriteLine( "   " + inner.Message );
					crashReport.WriteLine( "   (" + inner.GetType() + ")" );
					
					inner = inner.InnerException;
					}
					
				crashReport.WriteLine();
				crashReport.WriteLine( Locale.SafeGet("NaturalDocs.Engine", "CrashReport.StackTrace", "Stack Trace:") );
				crashReport.WriteLine();
				crashReport.WriteLine( e.StackTrace );

				// This part has a separate try block because it's okay if any of this information doesn't make it into the
				// crash report.  We'd like to have it, but it's still better to have the rest of it if we can't.
				try
					{				
					crashReport.WriteLine ();
					crashReport.WriteLine ( Locale.SafeGet("NaturalDocs.Engine", "CrashReport.CommandLine", "Command Line") + ":" );
					crashReport.WriteLine ();
					crashReport.WriteLine ("   " + Environment.CommandLine );
					crashReport.WriteLine ();
					crashReport.WriteLine ( Locale.SafeGet("NaturalDocs.Engine", "CrashReport.Version", "Version") +
														  ": " + Instance.VersionString );
					crashReport.WriteLine ( Locale.SafeGet("NaturalDocs.Engine", "CrashReport.Platform", "Platform") +
														  ": " + Environment.OSVersion.VersionString +
														  " (" + Environment.OSVersion.Platform + ")" );
					crashReport.WriteLine ( "SQLite: " + SQLite.API.LibVersion() );
					}
				catch
					{  }
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
		static public void AddStartupWatcher (IStartupWatcher watcher)
			{
			startupWatchers.Add(watcher);
			}

		/* Function: StartPossiblyLongOperation
		 * Called *by module code only* to signify that a possibly long operation is about to begin.  The operation name is arbitrary but 
		 * should be documented in <IStartupWatcher.OnStartPossiblyLongOperation>.  Every call should be matched with a
		 * <EndPossiblyLongOperation()> call, and it is up to the module code to make sure the calls are properly paired and 
		 * non-overlapping.
		 */
		static public void StartPossiblyLongOperation (string operationName)
			{
			foreach (IStartupWatcher watcher in startupWatchers)
				{  watcher.OnStartPossiblyLongOperation(operationName);  }
			}

		/* Function: EndPossiblyLongOperation
		 * Called *by module code only* to signify that the possibly long operation previously recorded with <StartPossiblyLongOperation()>
		 * has concluded.
		 */
		static public void EndPossiblyLongOperation ()
			{
			foreach (IStartupWatcher watcher in startupWatchers)
				{  watcher.OnEndPossiblyLongOperation();  }
			}



		// Group: Constants and Properties
		// __________________________________________________________________________
		
		
		/* Constant: VersionString
		 * The current version of the Natural Docs engine as a string.
		 */
		public const string VersionString = "2.0 (Development Release 10-01-2011)";
		
		
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
		static public Config.Manager Config
			{
			get
				{  return config;  }
			}
			
		/* Property: TopicTypes
		 * Returns the <TopicTypes.Manager> associated with this instance.
		 */
		static public TopicTypes.Manager TopicTypes
			{
			get
				{  return topicTypes;  }
			}
			
		/* Property: Languages
		 * Returns the <Languages.Manager> associated with this instance.
		 */
		static public Languages.Manager Languages
			{
			get
				{  return languages;  }
			}
			
		/* Property: Comments
		 * Returns the <Comments.Manager> associated with this instance.
		 */
		static public Comments.Manager Comments
			{
			get
				{  return comments;  }
			}
			
		/* Property: CodeDB
		 * Returns the <CodeDB.Manager> associated with this instance.
		 */
		static public CodeDB.Manager CodeDB
			{
			get
				{  return codeDB;  }
			}
			
		/* Property: Output
		 * Returns the <Output.Manager> associated with this instance.
		 */
		static public Output.Manager Output
			{
			get
				{  return output;  }
			}

		/* Property: Files
		 * Returns the <Files.Manager> associated with this instance.
		 */
		static public Files.Manager Files
			{
			get
				{  return files;  }
			}
			
			
			
			
		// Group: Variables
		// __________________________________________________________________________
		
		
		/* var: startupWatchers
		 * A list of all the objects that want to observe the engine's initialization.
		 */
		static private List<IStartupWatcher> startupWatchers;

		static private Config.Manager config;
		
		static private TopicTypes.Manager topicTypes;
		
		static private Languages.Manager languages;
		
		static private Comments.Manager comments;
		
		static private CodeDB.Manager codeDB;
		
		static private Output.Manager output;
			
		static private Files.Manager files;

		}
	}