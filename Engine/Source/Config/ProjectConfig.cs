/* 
 * Class: CodeClear.NaturalDocs.Engine.Config.ProjectConfig
 * ____________________________________________________________________________
 * 
 * A class representing the entire project configuration from <Project.txt> or equivalent sources.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2020 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using CodeClear.NaturalDocs.Engine.Config;


namespace CodeClear.NaturalDocs.Engine.Config
	{
	public class ProjectConfig
		{
		
		// Group: Functions
		// __________________________________________________________________________
		

		public ProjectConfig (Config.Source source)
			{
			this.source = source;

			projectConfigFolder = new Path();
			workingDataFolder = new Path();

			projectInfo = new ProjectInfo();

			tabWidth = 0;
			documentedOnly = false;
			autoGroup = true;
			shrinkFiles = true;

			projectConfigFolderPropertyLocation = Source.NotDefined;
			workingDataFolderPropertyLocation = Source.NotDefined;
			tabWidthPropertyLocation = Source.NotDefined;
			documentedOnlyPropertyLocation = Source.NotDefined;
			autoGroupPropertyLocation = Source.NotDefined;
			shrinkFilesPropertyLocation = Source.NotDefined;

			inputTargets = new List<Targets.InputBase>();
			filterTargets = new List<Targets.FilterBase>();
			outputTargets = new List<Targets.OutputBase>();
			}


			
		// Group: Properties
		// __________________________________________________________________________
		

		/* Property: Source
		 * The <Config.Source> of the entire configuration.
		 */
		public Config.Source Source
			{
			get
				{  return source;  }
			}

		
		/* Property: ProjectConfigFolder
		 * The <Path> where the project configuration is stored, or null if it hasn't been set.
		 */
		public Path ProjectConfigFolder
			{
			get
				{  return projectConfigFolder;  }
			set
				{  projectConfigFolder = value;  }
			}

		/* Property: WorkingDataFolder
		 * The <Path> where temporary working data is stored, or null if it hasn't been set.
		 */
		public Path WorkingDataFolder
			{
			get
				{  return workingDataFolder;  }
			set
				{  workingDataFolder = value;  }
			}

		/* Property: ProjectInfo
		 * The <ProjectInfo> that applies to the entire project.  The object will always be defined, though the properties inside it may
		 * not be.
		 */
		public ProjectInfo ProjectInfo
			{
			get
				{  return projectInfo;  }
			}

		/* Property: InputTargets
		 * All the input targets defined in this file.  If there are none, the list will exist but be empty.
		 */
		public List<Targets.InputBase> InputTargets
			{
			get
				{  return inputTargets;  }
			}
			
		/* Property: FilterTargets
		 * All the input filters defined in this file.  If there are none, the list will exist but be empty.
		 */
		public List<Targets.FilterBase> FilterTargets
			{
			get
				{  return filterTargets;  }
			}
			
		/* Property: OutputTargets
		 * All the output targets defined in this file.  If there are none, the list will exist but be empty.
		 */
		public List<Targets.OutputBase> OutputTargets
			{
			get
				{  return outputTargets;  }
			}
			
		/* Property: TabWidth
		 *	The number of spaces in a tab, or zero if it's undefined.
		 */
		public int TabWidth
			{
			get
				{  return tabWidth;  }
			set
				{  tabWidth = value;  }
			}
			
		/* Property: DocumentedOnly
		 * Whether the documentation should be limited to code elements that are explicitly documented.  Check <DocumentedOnlyPropertyLocation>
		 * to determine whether it's defined.
		 */
		public bool DocumentedOnly
			{
			get
				{  return documentedOnly;  }
			set
				{  documentedOnly = value;  }
			}
			
		/* Property: AutoGroup
		 * Whether automatic grouping is on.  Check <AutoGroupPropertyLocation> to determine whether it's defined.
		 */
		public bool AutoGroup
			{
			get
				{  return autoGroup;  }
			set
				{  autoGroup = value;  }
			}
			
		/* Property: ShrinkFiles
		 * Whether to remove whitespace and comments from resource files like CSS and JavaScript in the output.
		 */
		public bool ShrinkFiles
			{
			get
				{  return shrinkFiles;  }
			set
				{  shrinkFiles = value;  }
			}
			
	
		
		// Group: Property Locations
		// __________________________________________________________________________
	

		/* Property: ProjectConfigFolderPropertyLocation
		 * Where the <ProjectConfigFolder> property is defined, or <Source.NotDefined> if it isn't.
		 */
		public PropertyLocation ProjectConfigFolderPropertyLocation
			{
			get
				{  return projectConfigFolderPropertyLocation;  }
			set
				{  projectConfigFolderPropertyLocation = value;  }
			}

		/* Property: WorkingDataFolderPropertyLocation
		 * Where the <WorkingDataFolder> property is defined, or <Source.NotDefined> if it isn't.
		 */
		public PropertyLocation WorkingDataFolderPropertyLocation
			{
			get
				{  return workingDataFolderPropertyLocation;  }
			set
				{  workingDataFolderPropertyLocation = value;  }
			}

		/* Property: TabWidthPropertyLocation
		 * Where the <TabWidth> property is defined, or <Source.NotDefined> if it isn't.
		 */
		public PropertyLocation TabWidthPropertyLocation
			{
			get
				{  return tabWidthPropertyLocation;  }
			set
				{  tabWidthPropertyLocation = value;  }
			}
			
	
		/* Property: DocumentedOnlyPropertyLocation
		 * Where the <DocumentedOnly> property is defined, or <Source.NotDefined> if it isn't.
		 */
		public PropertyLocation DocumentedOnlyPropertyLocation
			{
			get
				{  return documentedOnlyPropertyLocation;  }
			set
				{  documentedOnlyPropertyLocation = value;  }
			}
			
	
		/* Property: AutoGroupPropertyLocation
		 * Where the <AutoGroup> property is defined, or <Source.NotDefined> if it isn't.
		 */
		public PropertyLocation AutoGroupPropertyLocation
			{
			get
				{  return autoGroupPropertyLocation;  }
			set
				{  autoGroupPropertyLocation = value;  }
			}
			
	
		/* Property: ShrinkFilesPropertyLocation
		 * Where the <ShrinkFiles> property is defined, or <Source.NotDefined> if it isn't.
		 */
		public PropertyLocation ShrinkFilesPropertyLocation
			{
			get
				{  return shrinkFilesPropertyLocation;  }
			set
				{  shrinkFilesPropertyLocation = value;  }
			}
			
	
		
		// Group: Variables
		// __________________________________________________________________________
		
		protected Config.Source source;

		protected Path projectConfigFolder;
		protected Path workingDataFolder;

		protected ProjectInfo projectInfo;

		protected int tabWidth;
		protected bool documentedOnly;
		protected bool autoGroup;
		protected bool shrinkFiles;

		protected PropertyLocation projectConfigFolderPropertyLocation;
		protected PropertyLocation workingDataFolderPropertyLocation;
		protected PropertyLocation globalStyleNamePropertyLocation;
		protected PropertyLocation tabWidthPropertyLocation;
		protected PropertyLocation documentedOnlyPropertyLocation;
		protected PropertyLocation autoGroupPropertyLocation;
		protected PropertyLocation shrinkFilesPropertyLocation;

		protected List<Targets.InputBase> inputTargets;
		protected List<Targets.FilterBase> filterTargets;
		protected List<Targets.OutputBase> outputTargets;
		
		}
	}