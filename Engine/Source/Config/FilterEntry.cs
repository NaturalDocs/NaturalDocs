/* 
 * Class: GregValure.NaturalDocs.Engine.Config.FilterEntry
 * ____________________________________________________________________________
 * 
 * A base class for <Enties> handling filtering.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2011 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace GregValure.NaturalDocs.Engine.Config
	{
	abstract public class FilterEntry : Entry
		{
		
		protected FilterEntry (Path configFile = default(Path), int lineNumber = -1) : base (configFile, lineNumber)
			{
			}
		
		}
	}