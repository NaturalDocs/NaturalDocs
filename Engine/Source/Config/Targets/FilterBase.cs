/* 
 * Class: GregValure.NaturalDocs.Engine.Config.Targets.FilterBase
 * ____________________________________________________________________________
 * 
 * A base class for the configuration of all filter targets.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2013 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using GregValure.NaturalDocs.Engine.Config;


namespace GregValure.NaturalDocs.Engine.Config.Targets
	{
	abstract public class FilterBase : Base
		{
		
		// Group: Functions
		// __________________________________________________________________________
		

		public FilterBase (PropertyLocation propertyLocation) : base (propertyLocation)
			{
			}

		public FilterBase (FilterBase toCopy) : base (toCopy)
			{
			}

		abstract public FilterBase Duplicate ();
					
		}
	}