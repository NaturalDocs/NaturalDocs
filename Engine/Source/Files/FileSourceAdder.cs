/*
 * Class: CodeClear.NaturalDocs.Engine.Files.FileSourceAdder
 * ____________________________________________________________________________
 *
 * A base class for adding all the files in a single <FileSource> to <Files.Manager>.  Each <FileSource> class should have
 * its own corresponding Adder class.
 *
 *
 * Topic: Usage
 *
 *		- Call <AddAllFiles()>.  This is not a WorkOn function so only a single thread can call it.
 *
 *		- Other threads may check the status with <GetStatus()>.
 *
 *
 * Multithreading: Thread Safety Notes
 *
 *		Externally, this class is thread safe.
 *
 *		Internally, all variables are either read-only or themselves thread safe.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2022 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using CodeClear.NaturalDocs.Engine.Collections;


namespace CodeClear.NaturalDocs.Engine.Files
	{
	abstract public class FileSourceAdder : Process
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Function: FileSourceAdder
		 */
		public FileSourceAdder (Files.FileSource fileSource, Engine.Instance engineInstance) : base (engineInstance)
			{
			this.fileSource = fileSource;
			this.status = new AdderStatus();
			}


		/* Function: Dispose
		 */
		override protected void Dispose (bool strictRulesApply)
			{
			}


		/* Function: AddAllFiles
		 * Goes through all the files in the <FileSource> and calls <Files.Manager.AddOrUpdateFile()> on each one.
		 */
		abstract public void AddAllFiles (CancelDelegate cancelDelegate);


		/* Function: GetStatus
		 * Fills the passed object with the status of <AddAllFiles()>.
		 */
		public void GetStatus (ref AdderStatus statusTarget)
			{
			statusTarget.Copy(status);
			}


		/* Function: AddStatusTo
		 * Adds the status of <AddAllFiles()> to the passed one.
		 */
		public void AddStatusTo (ref AdderStatus statusTarget)
			{
			statusTarget.Add(status);
			}



		// Group: Properties
		// __________________________________________________________________________


		public Files.Manager Manager
			{
			get
				{  return EngineInstance.Files;  }
			}

		public Files.FileSource FileSource
			{
			get
				{  return fileSource;  }
			}



		// Group: Variables
		// __________________________________________________________________________


		/* var: fileSource
		 *
		 * The <FileSource> associated with this adder.
		 *
		 * Thread Safety:
		 *
		 *		This variable is only set on object creation and is read-only after that, so it is inherently thread safe and can
		 *		be read without a lock.
		 */
		protected FileSource fileSource;


		/* var: status
		 *
		 * The current status of <AddAllFiles()>.
		 *
		 * Thread Safety:
		 *
		 *		This object is externally thread safe and can be used without a lock.
		 */
		protected AdderStatus status;

		}
	}
