/* 
 * Class: CodeClear.NaturalDocs.Engine.Hierarchies.Manager
 * ____________________________________________________________________________
 * 
 * A module to handle the different hierarchies within Natural Docs.
 * 
 * 
 * Topic: Usage
 * 
 *		- Call <Engine.Instance.Start()> which will start this module.
 *			
 */

// This file is part of Natural Docs, which is Copyright © 2003-2021 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;


namespace CodeClear.NaturalDocs.Engine.Hierarchies
	{
	public class Manager : Module
		{
		
		// Group: Functions
		// __________________________________________________________________________
		
		
		/* Constructor: Manager
		 */
		public Manager (Engine.Instance engineInstance) : base (engineInstance)
			{
			hierarchies = null;
			classHierarchyID = 0;
			}

		protected override void Dispose (bool strictRulesApply)
			{
			}
		
		/* Function: Start
		 * 
		 * Starts the module, returning whether it was successful.  If there were any errors they will be added to errorList.
		 * 
		 * Dependencies:
		 * 
		 *		- Call <Languages.Manager.Start_Stage1()> before calling this function.
		 */
		public bool Start (Errors.ErrorList errorList)
			{
			var classHierarchy = new Hierarchy(
				id: 1,
				name: "Class", 
				pluralName: "Classes", 
				simpleIdentifier: "Class",
				pluralSimpleIdentifier: "Classes",
				isLanguageSpecific: true);

			var sqlLanguage = EngineInstance.Languages.FromName("SQL");

			var databaseHierarchy = new Hierarchy(
				id: 2,
				name: "Database", 
				pluralName: "Database", // we don't want "Databases"
				simpleIdentifier: "Database",
				pluralSimpleIdentifier: "Database", // we don't want "Databases"
				isLanguageSpecific: false,
				isCaseSensitive: (sqlLanguage != null ? sqlLanguage.CaseSensitive : false) );

			hierarchies = new Hierarchy[] { classHierarchy, databaseHierarchy };

			classHierarchyID = classHierarchy.ID;

			return true;
			}



		// Group: Information Functions
		// __________________________________________________________________________


		/* Function: FromID
		 * Returns the <Hierarchy> associated with the passed ID, or null if none.
		 */
		public Hierarchy FromID (int hierarchyID)
			{
			foreach (var hierarchy in hierarchies)
				{
				if (hierarchy.ID == hierarchyID)
					{  return hierarchy;  }
				}

			return null;
			}

		/* Function: FromName
		 * Returns the <Hierarchy> associated with the passed name, or null if none.  This will search both the singular
		 * and plural names.
		 */
		public Hierarchy FromName (string name)
			{
			if (String.IsNullOrEmpty(name))
				{  return null;  }

			Collections.KeySettings normalizationSettings = Collections.KeySettings.IgnoreCase;

			string normalizedName = name.NormalizeKey(normalizationSettings);

			foreach (var hierarchy in hierarchies)
				{
				if (hierarchy.Name.NormalizeKey(normalizationSettings) == normalizedName)
					{  return hierarchy;  }
				if (hierarchy.PluralName != null &&
					hierarchy.PluralName.NormalizeKey(normalizationSettings) == normalizedName)
					{  return hierarchy;  }
				}

			return null;
			}



		// Group: Properties
		// __________________________________________________________________________


		/* Property: AllHierarchies
		 * Returns all the defined hierarchies.
		 */
		public IEnumerable<Hierarchy> AllHierarchies
			{
			get
				{  return hierarchies;  }
			}

		/* Property: ClassHierarchyID
		 * Returns the ID of the class hierarchy.
		 */
		public int ClassHierarchyID
			{
			get
				{  return classHierarchyID;  }
			}



		// Group: Variables
		// __________________________________________________________________________


		/* var: hierarchies
		 * An array of the <Hierarchies> defined.  Since it should be a short, hard-coded list for now it's more efficient to
		 * just put them in an array.
		 */
		protected Hierarchy[] hierarchies;

		/* var: classHierarchyID
		 * A copy of the class hierarchy ID since it is used so often.  Prevents some lookups in <hierarchies>.
		 */
		protected int classHierarchyID;

		}
	}