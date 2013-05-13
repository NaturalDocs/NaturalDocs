/* 
 * Class: GregValure.NaturalDocs.Engine.Config.ConfigData
 * ____________________________________________________________________________
 * 
 * A class representing the information that can be parsed out of <Project.txt> or <Project.nd>.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2013 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;


namespace GregValure.NaturalDocs.Engine.Config
	{
	public class ConfigData
		{
		
		// Group: Functions
		// __________________________________________________________________________
		
		public ConfigData ()
			{
			projectInfo = new ProjectInfo();
			tabWidth = 0;
			documentedOnly = null;
			autoGroup = null;
			entries = new List<Entry>();
			}
			
			
		// Group: Properties
		// __________________________________________________________________________
		

		/* Property: ProjectInfo
		 * A reference to the default <ProjectInfo> of the project.
		 */
		public ProjectInfo ProjectInfo
			{
			get
				{  return projectInfo;  }
			}
		
		/* Property: Title
		 * The default title of the project, or null if it's not set.
		 */
		public string Title
			{
			get
				{  return projectInfo.Title;  }
			set
				{  projectInfo.Title = value;  }
			}
			
		/* Property: Subtitle
		 * The default subtitle of the project, or null if it's not set.
		 */
		public string Subtitle
			{
			get
				{  return projectInfo.Subtitle;  }
			set
				{  projectInfo.Subtitle = value;  }
			}
			
		/* Property: Copyright
		 *	The default copyright line of the project, or null if it's not set.
		 */
		public string Copyright
			{
			get
				{  return projectInfo.Copyright;  }
			set
				{  projectInfo.Copyright = value;  }
			}
			
		/* Property: TimestampCode
		 * The default timestamp code of the project, or null if it's not set.
		 */
		public string TimestampCode
			{
			get
				{  return projectInfo.TimeStampCode;  }
			set
				{  projectInfo.TimeStampCode = value;  }
			}
			
		/* Property: StyleName
		 * The default style identifier for all output targets, or null if it's not set.
		 */
		public string StyleName
			{
			get
				{  return projectInfo.StyleName;  }
			set
				{  projectInfo.StyleName = value;  }
			}
			
		/* Property: TabWidth
		 * The number of spaces a tab should be expanded to, or zero if it's not set.
		 */
		public int TabWidth
			{
			get
				{  return tabWidth;  }
			set
				{  tabWidth = value;  }
			}

		/* Property: DocumentedOnly
		 * Whether only documented code elements should appear in the output.
		 */
		public bool? DocumentedOnly
			{
			get
				{  return documentedOnly;  }
			set
				{  documentedOnly = value;  }
			}
			
		/* Property: AutoGroup
		 * Whether automatic grouping should be applied or not, or null if it's not set.
		 */
		public bool? AutoGroup
			{
			get
				{  return autoGroup;  }
			set
				{  autoGroup = value;  }
			}
			
		/* Property: Entries
		 * A list of the <Entries> appearing in the file.  They appear in the file's order.  The list will never be null; if
		 * there are no entries, the list will be empty.
		 */
		public List<Entry> Entries
			{
			get
				{  return entries;  }
			set
				{
				if (value == null)
					{  entries.Clear();  }
				else
					{  entries = value;  }
				}
			}
		
		
		// Group: Variables
		// __________________________________________________________________________
		
		protected ProjectInfo projectInfo;
		protected int tabWidth;
		protected bool? documentedOnly;
		protected bool? autoGroup;
		protected List<Entry> entries;
		
		}
	}