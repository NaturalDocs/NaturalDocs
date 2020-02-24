/* 
 * Class: CodeClear.NaturalDocs.Engine.Output.Builder
 * ____________________________________________________________________________
 * 
 * The base class for an output builder.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2020 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.IO;
using CodeClear.NaturalDocs.Engine.Collections;
using CodeClear.NaturalDocs.Engine.Links;
using CodeClear.NaturalDocs.Engine.Topics;


namespace CodeClear.NaturalDocs.Engine.Output
	{
	abstract public class Builder
		{
		
		// Group: Functions
		// __________________________________________________________________________
		
		
		/* Function: Builder
		 */
		public Builder (Output.Manager manager)
			{
			this.manager = manager;
			}
			
		
		/* Function: Start
		 * Initializes the builder and returns whether all the settings are correct and that execution is ready to begin.  
		 * If there are problems they are added as <Errors> to the errorList parameter.
		 */
		virtual public bool Start (Errors.ErrorList errorList)
			{  
			return true;
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
		abstract public void WorkOnUpdatingOutput (CancelDelegate cancelDelegate);

			
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
		virtual public void WorkOnFinalizingOutput (CancelDelegate cancelDelegate)
			{
			}


		/* Function: Cleanup
		 * Cleans up the builder's internal data when everything is up to date.  The default implementation does nothing.  You
		 * can pass a <CancelDelegate> to interrupt the process if necessary.
		 */
		virtual public void Cleanup (CancelDelegate cancelDelegate)
			{
			}


		/* Function: UnitsOfWorkRemaining
		 * Returns a number representing how much work the builder has left to do.  Building the HTML output for a single source 
		 * file is counted as 10 units so everything else should be scored relative to that.
		 */
		abstract public long UnitsOfWorkRemaining ();
			
			
			
		// Group: Path Functions
		// __________________________________________________________________________
		
		
		/* Function: CreateTextFileAndPath
		 * Creates a UTF-8 text file at the specified path, creating any subfolders as required.
		 */
		public StreamWriter CreateTextFileAndPath (Path path)
			{
			try
				{  
				// This will create multiple subdirectories if needed, and will not throw an exception if it already exists.
				Directory.CreateDirectory(path.ParentFolder);  
				}
			catch
				{
				throw new Exceptions.UserFriendly( Locale.Get("NaturalDocs.Engine", "Error.CouldNotCreateOutputFolder(name)", path.ParentFolder) );
				}
				
			StreamWriter streamWriter = null;
			
			try
				{  streamWriter = File.CreateText(path);  }
			catch
				{
				throw new Exceptions.UserFriendly( Locale.Get("NaturalDocs.Engine", "Error.CouldNotCreateOutputFile(name)", path) );
				}
				
			return streamWriter;
			}



		// Group: Properties
		// __________________________________________________________________________


		/* Property: Manager
		 * The <Output.Manager> associated with this builder.
		 */
		public Output.Manager Manager
			{
			get
				{  return manager;  }
			}


		/* Property: EngineInstance
		 * The <Engine.Instance> associated with this builder.
		 */
		public Engine.Instance EngineInstance
			{
			get
				{  return Manager.EngineInstance;  }
			}


		/* Property: Styles
		 * A list of <Styles> that apply to this builder, or null if none.
		 */
		virtual public IList<Style> Styles
			{
			get
				{  return null;  }
			}



		// Group: Variables
		// __________________________________________________________________________

		protected Output.Manager manager;

		}
	}