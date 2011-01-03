/* 
 * Class: GregValure.NaturalDocs.Engine.Comments.Parser
 * ____________________________________________________________________________
 * 
 * A base class for Natural Docs comment parsers.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2011 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace GregValure.NaturalDocs.Engine.Comments
	{
	public class Parser
		{
		
		// Group: Functions
		// __________________________________________________________________________
		
		
		/* Function: Parser
		 */
		public Parser ()
			{
			}
			
		/* Function: Start
		 */
		virtual public bool Start (Errors.ErrorList errors)
			{
			return true;
			}

		}
	}