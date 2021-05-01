/* 
 * Class: CodeClear.NaturalDocs.Engine.Hierarchies.Hierarchy
 * ____________________________________________________________________________
 * 
 * Information about a single hierarchy within Natural Docs.
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2021 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace CodeClear.NaturalDocs.Engine.Hierarchies
	{
	public class Hierarchy
		{
		
		// Group: Functions
		// __________________________________________________________________________
		
		
		/* Constructor: Hierarchy
		 */
		public Hierarchy (string name, string pluralName, HierarchyType type)
			{
			this.name = name;
			this.pluralName = pluralName;
			this.type = type;
			}



		// Group: Properties
		// __________________________________________________________________________


		/* Property: ID
		 * The numeric ID of the hierarchy.
		 */
		public int ID
			{
			get
				{  return (int)type;  }
			}

		/* Property: Name
		 * The name of the hierarchy.
		 */
		public string Name
			{
			get
				{  return name;  }
			}

		/* Property: PluralName
		 * The plural name of the hierarchy, or null if none.
		 */
		public string PluralName
			{
			get
				{  return pluralName;  }
			}

		/* Property: Type
		 * The <HierarchyType> of the hierarchy.
		 */
		public HierarchyType Type
			{
			get
				{  return type;  }
			}



		// Group: Variables
		// __________________________________________________________________________


		/* var: name
		 * The name of the hierarchy.
		 */
		protected string name;

		/* var: pluralName
		 * The plural name of the hierarchy, or null if none.
		 */
		protected string pluralName;

		/* var: type
		 * The <HierarchyType> associated with this hierarchy.
		 */
		protected HierarchyType type;

		}
	}