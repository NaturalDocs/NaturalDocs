/* 
 * Class: GregValure.NaturalDocs.Engine.Config.OutputEntry
 * ____________________________________________________________________________
 * 
 * A base class for <Entries> that handle output.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2013 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace GregValure.NaturalDocs.Engine.Config
	{
	abstract public class OutputEntry : Entry
		{

		// Group: Functions
		// __________________________________________________________________________
		
		
		protected OutputEntry (Path folder, Path configFile = default(Path), int lineNumber = -1) : base (configFile, lineNumber)
			{
			this.folder = folder;
			this.number = 0;
			this.projectInfo = new ProjectInfo();

			if (folder.IsRelative)
				{  throw new Exception("OutputEntry must use absolute paths.");  }
			}

		

		// Group: Properties
		// __________________________________________________________________________

		
		/* Property: Folder
		 * The absolute <Path> to the output folder.
		 */
		public Path Folder
			{
			get
				{  return folder;  }
			}

		/* Property: Number
		 * The number of this output entry, or zero if it hasn't been set yet.
		 */
		public int Number
			{
			get
				{  return number;  }
			set
				{  number = value;  }
			}

		/* Property: ProjectInfo
		 * The project info as it applies to this entry.  During <Config.Manager.Start()> this will only contain
		 * the overridden values, but afterwards it will be filled in to be complete.
		 */
		public ProjectInfo ProjectInfo
			{
			get
				{  return projectInfo;  }
			}
			
		/* Property: OutputWorkingDataFile
		 * The working data file path for this entry, if you decide you need one.
		 */
		public Path OutputWorkingDataFile
			{
			get
				{  return Engine.Instance.Config.OutputWorkingDataFileOf(number);  }
			}
			
		/* Function: OutputWorkingDataFolder
		 * The working data folder path for this entry, if you decide you need one.
		 */
		public Path OutputWorkingDataFolder
			{
			get
				{  return Engine.Instance.Config.OutputWorkingDataFolderOf(number);  }
			}


		// Group: Variables
		// __________________________________________________________________________		
		
		protected Path folder;
		protected int number;
		protected ProjectInfo projectInfo;
		
		}
	}