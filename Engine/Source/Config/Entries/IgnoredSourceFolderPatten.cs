/* 
 * Class: GregValure.NaturalDocs.Engine.Config.Entries.IgnoredSourceFolderPattern
 * ____________________________________________________________________________
 * 
 * An <Entry> for ignored folder patterns.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2013 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)


using System;


namespace GregValure.NaturalDocs.Engine.Config.Entries
	{
	public class IgnoredSourceFolderPattern : FilterEntry
		{
		
		// Group: Functions
		// __________________________________________________________________________
		
		public IgnoredSourceFolderPattern (string pattern, Path configFile = default(Path), int lineNumber = -1) : base (configFile, lineNumber)
			{
			this.pattern = pattern;
			}
			

		// Group: Properties
		// __________________________________________________________________________
		
		public string Pattern
			{
			get
				{  return pattern;  }
			}
			

		// Group: Variables
		// __________________________________________________________________________
		
		protected string pattern;

		}
	}