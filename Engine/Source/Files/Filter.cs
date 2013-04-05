/* 
 * Class: GregValure.NaturalDocs.Engine.Files.Filter
 * ____________________________________________________________________________
 * 
 * A base class for a filter.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2013 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace GregValure.NaturalDocs.Engine.Files
	{
	public class Filter
		{
		
		public Filter ()
			{
			}
			
		public virtual bool IgnoreSourceFolder (Path path)
			{
			return false;
			}
		
		}
	}