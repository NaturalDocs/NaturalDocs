/* 
 * Class: CodeClear.NaturalDocs.Engine.Config.Targets.Filter
 * ____________________________________________________________________________
 * 
 * A base class for the configuration of all filter targets.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2020 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace CodeClear.NaturalDocs.Engine.Config.Targets
	{
	abstract public class Filter : Target
		{
		
		// Group: Functions
		// __________________________________________________________________________
		

		public Filter (PropertyLocation propertyLocation) : base (propertyLocation)
			{
			}

		public Filter (Filter toCopy) : base (toCopy)
			{
			}

		abstract public Filter Duplicate ();
					
		}
	}