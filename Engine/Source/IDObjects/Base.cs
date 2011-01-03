/* 
 * Class: GregValure.NaturalDocs.Engine.IDObjects.Base
 * ____________________________________________________________________________
 * 
 * The base class for all objects to be managed with <IDObjects.Manager>.
 * 
 * 
 * Topic: Usage
 * 
 *		- The deriving class needs to define <Name> to give each object a unique textual name.   *<Name> must never 
 *		  change.*
 */

// This file is part of Natural Docs, which is Copyright © 2003-2011 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace GregValure.NaturalDocs.Engine.IDObjects
	{
	abstract public class Base
		{
		
		/* Function: Base
		 */
		public Base ()
			{
			id = 0;
			}
			
		
		/* Property: Name
		 * The textual name of the object.
		 */
		abstract public string Name
			{
			get;
			}
			
			
		/* Property: ID
		 * The numeric ID of the object.  This cannot be changed once it is set the first time.  It will be zero if it's read 
		 * before that happens.
		 */
		public int ID
			{
			get
				{  return id;  }
			set
				{
				if (id != 0)
					{  throw new InvalidOperationException("Tried to change an object ID after it has been assigned.");  }
				if (value < 1)
					{  throw new ArgumentException("Tried to set an object ID to zero or a negative number.");  }
					
				id = value;
				}
			}

		
		/* Var: id
		 * The numberic ID of the object.
		 */
		protected int id;
		}
	}