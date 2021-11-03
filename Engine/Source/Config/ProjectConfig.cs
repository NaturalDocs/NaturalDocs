/* 
 * Class: CodeClear.NaturalDocs.Engine.Config.ProjectConfig
 * ____________________________________________________________________________
 * 
 * A class representing the entire project configuration from <Project.txt> or equivalent sources.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2021 Code Clear LLC.
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
		

		public ProjectConfig (Config.PropertySource source)
			{
			this.source = source;

			projectConfigFolder = null;
			workingDataFolder = null;

			inputSettings = new OverridableInputSettings();
			outputSettings = new OverridableOutputSettings();

			inputTargets = new List<Targets.Input>();
			filterTargets = new List<Targets.Filter>();
			outputTargets = new List<Targets.Output>();

			tabWidth = 0;
			documentedOnly = false;
			autoGroup = true;
			shrinkFiles = true;

			projectConfigFolderPropertyLocation = PropertySource.NotDefined;
			workingDataFolderPropertyLocation = PropertySource.NotDefined;
			tabWidthPropertyLocation = PropertySource.NotDefined;
			documentedOnlyPropertyLocation = PropertySource.NotDefined;
			autoGroupPropertyLocation = PropertySource.NotDefined;
			shrinkFilesPropertyLocation = PropertySource.NotDefined;
			}


			
		// Group: Properties
		// __________________________________________________________________________
		

		/* Property: Source
		 * The <Config.Source> of the entire configuration.
		 */
		public Config.PropertySource Source
			{
			get
				{  return source;  }
			}

		
		/* Property: ProjectConfigFolder
		 * The <AbsolutePath> where the project configuration is stored, or null if it hasn't been set.
		 */
		public AbsolutePath ProjectConfigFolder
			{
			get
				{  return projectConfigFolder;  }
			set
				{  projectConfigFolder = value;  }
			}

		/* Property: WorkingDataFolder
		 * The <AbsolutePath> where temporary working data is stored, or null if it hasn't been set.
		 */
		public AbsolutePath WorkingDataFolder
			{
			get
				{  return workingDataFolder;  }
			set
				{  workingDataFolder = value;  }
			}

		/* Property: InputSettings
		 * The <OverridableInputSettings> that apply to the entire project.  Individual input targets may override its properties.
		 * This object will always be defined, though the properties inside it may not be.
		 */
		public OverridableInputSettings InputSettings
			{
			get
				{  return inputSettings;  }
			}

		/* Property: OutputSettings
		 * The <OverridableOutputSettings> that apply to the entire project.  Individual output targets may override its properties.
		 * This object will always be defined, though the properties inside it may not be.
		 */
		public OverridableOutputSettings OutputSettings
			{
			get
				{  return outputSettings;  }
			}

		/* Property: InputTargets
		 * All the input targets defined in this file.  If there are none, the list will exist but be empty.
		 */
		public List<Targets.Input> InputTargets
			{
			get
				{  return inputTargets;  }
			}
			
		/* Property: FilterTargets
		 * All the input filters defined in this file.  If there are none, the list will exist but be empty.
		 */
		public List<Targets.Filter> FilterTargets
			{
			get
				{  return filterTargets;  }
			}
			
		/* Property: OutputTargets
		 * All the output targets defined in this file.  If there are none, the list will exist but be empty.
		 */
		public List<Targets.Output> OutputTargets
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
		 * Where the <ProjectConfigFolder> property is defined, or <PropertySource.NotDefined> if it isn't.
		 */
		public PropertyLocation ProjectConfigFolderPropertyLocation
			{
			get
				{  return projectConfigFolderPropertyLocation;  }
			set
				{  projectConfigFolderPropertyLocation = value;  }
			}

		/* Property: WorkingDataFolderPropertyLocation
		 * Where the <WorkingDataFolder> property is defined, or <PropertySource.NotDefined> if it isn't.
		 */
		public PropertyLocation WorkingDataFolderPropertyLocation
			{
			get
				{  return workingDataFolderPropertyLocation;  }
			set
				{  workingDataFolderPropertyLocation = value;  }
			}

		/* Property: TabWidthPropertyLocation
		 * Where the <TabWidth> property is defined, or <PropertySource.NotDefined> if it isn't.
		 */
		public PropertyLocation TabWidthPropertyLocation
			{
			get
				{  return tabWidthPropertyLocation;  }
			set
				{  tabWidthPropertyLocation = value;  }
			}
			
	
		/* Property: DocumentedOnlyPropertyLocation
		 * Where the <DocumentedOnly> property is defined, or <PropertySource.NotDefined> if it isn't.
		 */
		public PropertyLocation DocumentedOnlyPropertyLocation
			{
			get
				{  return documentedOnlyPropertyLocation;  }
			set
				{  documentedOnlyPropertyLocation = value;  }
			}
			
	
		/* Property: AutoGroupPropertyLocation
		 * Where the <AutoGroup> property is defined, or <PropertySource.NotDefined> if it isn't.
		 */
		public PropertyLocation AutoGroupPropertyLocation
			{
			get
				{  return autoGroupPropertyLocation;  }
			set
				{  autoGroupPropertyLocation = value;  }
			}
			
	
		/* Property: ShrinkFilesPropertyLocation
		 * Where the <ShrinkFiles> property is defined, or <PropertySource.NotDefined> if it isn't.
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
		
		protected Config.PropertySource source;

		protected AbsolutePath projectConfigFolder;
		protected AbsolutePath workingDataFolder;

		protected OverridableInputSettings inputSettings;
		protected OverridableOutputSettings outputSettings;

		protected List<Targets.Input> inputTargets;
		protected List<Targets.Filter> filterTargets;
		protected List<Targets.Output> outputTargets;

		protected int tabWidth;
		protected bool documentedOnly;
		protected bool autoGroup;
		protected bool shrinkFiles;

		protected PropertyLocation projectConfigFolderPropertyLocation;
		protected PropertyLocation workingDataFolderPropertyLocation;
		protected PropertyLocation tabWidthPropertyLocation;
		protected PropertyLocation documentedOnlyPropertyLocation;
		protected PropertyLocation autoGroupPropertyLocation;
		protected PropertyLocation shrinkFilesPropertyLocation;

		}
	}