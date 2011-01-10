/* 
 * Class: GregValure.NaturalDocs.Engine.Output.Manager
 * ____________________________________________________________________________
 * 
 * A class to manage all the output builders.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2011 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using GregValure.NaturalDocs.Engine.Errors;


namespace GregValure.NaturalDocs.Engine.Output
	{
	public class Manager
		{
		
		// Group: Functions
		// __________________________________________________________________________
		
		
		/* Constructor: Manager
		 */
		public Manager ()
			{
			builders = new List<Builder>();
			reparseStyleFiles = false;
			}
			
			
		/* Function: AddBuilder
		 * Adds an <Output.Builder> to the list.  This can only be called before the class is started.
		 */
		public void AddBuilder (Builder builder)
			{
			builders.Add(builder);
			}
			
			
		/* Function: Start
		 * Initializes the manager and returns whether all the settings are correct and that execution is ready to begin.  
		 * If there are problems they are added as <Errors> to the errorList parameter.  This class is *not* designed to allow 
		 * multiple attempts.  If this function fails scrap the entire <Engine.Instance> and start again.
		 */
		public bool Start (ErrorList errorList)
			{
			bool success = true;
			
			foreach (Builder builder in builders)
				{
				if (builder.Start(errorList) == false)
					{  success = false;  }
				}
				
			if (success == false)
				{  return false;  }

			Styles.FileSource styleFileSource = new Styles.FileSource();
				
			foreach (Builder builder in builders)
				{
				Engine.Instance.CodeDB.AddChangeWatcher(builder);

				if (builder is Files.IStyleChangeWatcher)
					{  Engine.Instance.Files.AddStyleChangeWatcher((Files.IStyleChangeWatcher)builder);  }

				if (builder.Styles != null)
					{
					foreach (Style style in builder.Styles)
						{  styleFileSource.AddStyle(style);  }
					}
				}

			if (reparseStyleFiles)
				{  styleFileSource.ForceReparse = true;  }

			Engine.Instance.Files.AddFileSource(styleFileSource);
				
			return success;
			}


		/* Function: WorkOnUpdatingOutput
		 * 
		 * Works on the task of updating the output files for any changes it has detected so far.  This is a parallelizable task, so
		 * multiple threads can call this function and the work will be divided up between them.  Note that the output may not be
		 * usable after this completes; you also need to call <WorkOnFinalizingOutput()>.
		 * 
		 * This function returns if it's cancelled or there is no more work to be done.  If there is only one thread working on this 
		 * then the task is complete, but if there are multiple threads the task isn't complete until they all have returned.  This one 
		 * may have returned because there was no more work for this thread to do, but other threads are still working.
		 */
		public void WorkOnUpdatingOutput (CancelDelegate cancelDelegate)
			{
			foreach (Builder builder in builders)
				{
				if (cancelDelegate())
					{  return;  }
					
				builder.WorkOnUpdatingOutput(cancelDelegate);
				}
			}


		/* Function: WorkOnFinalizingOutput
		 * 
		 * Works on the task of finalizing the output, which is any task that requires all files to be successfully processed by
		 * <WorkOnUpdatingOutput()> before it can run.  You must wait for all threads to return from <WorkOnUpdatingOutput()>
		 * before calling this function.  Examples of finalization include generating index and search data for HTML output and
		 * compiling the temporary files into the final one for PDF output.  This is a parallelizable task, so multiple threads can call 
		 * this function and the work will be divided up between them.
		 * 
		 * This function returns if it's cancelled or there is no more work to be done.  If there is only one thread working on this 
		 * then the task is complete, but if there are multiple threads the task isn't complete until they all have returned.  This one 
		 * may have returned because there was no more work for this thread to do, but other threads are still working.
		 */
		public void WorkOnFinalizingOutput (CancelDelegate cancelDelegate)
			{
			foreach (Builder builder in builders)
				{
				if (cancelDelegate())
					{  return;  }
					
				builder.WorkOnFinalizingOutput(cancelDelegate);
				}
			}


		/* Function: Cleanup
		 * Cleans up the module's internal data when everything is up to date.  This will do things like remove empty output
		 * folders.  You can pass a <CancelDelegate> to interrupt the process if necessary.
		 */
		public void Cleanup (CancelDelegate cancelDelegate)
			{
			foreach (Builder builder in builders)
				{  builder.Cleanup(cancelDelegate);  }
			}
			


		// Group: Properties
		// __________________________________________________________________________
		
		
		/* Property: Builders
		 * A read-only list of <Builders> managed by this module.  If there are none, the list will be empty instead of null.
		 */
		public IList<Builder> Builders
			{
			get
				{  return builders.AsReadOnly();  }
			}

		/* Property: ReparseStyleFiles
		 * If set to true, it will force reparsing of all files associated with styles.  Use this when adding a new style to a builder,
		 * since if another builder already used that style they will not be parsed again otherwise.  You can only set this to true,
		 * you cannot set it back to false.
		 */
		public bool ReparseStyleFiles
			{
			get
				{  return reparseStyleFiles;  }
			set
				{
				if (value == true)
					{  reparseStyleFiles = true;  }
				else
					{  throw new InvalidOperationException();  }
				}
			}


			
		// Group: Variables
		// __________________________________________________________________________
		
		
		protected List<Builder> builders;

		protected bool reparseStyleFiles;
		
		
		}
	}