/*
 * Class: CodeClear.NaturalDocs.Engine.Output.HTML.Components.JSONMenuEntries.Container
 * ____________________________________________________________________________
 *
 * A base class for container entries in <JSONMenu>.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2025 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;


namespace CodeClear.NaturalDocs.Engine.Output.HTML.Components.JSONMenuEntries
	{
	public class Container : Entry
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Function: Container
		 */
		public Container (MenuEntries.Container menuContainer) : base (menuContainer)
			{
			members = new List<Entry>(menuContainer.Members.Count);

			jsonBeforeMembers = null;
			jsonAfterMembers = null;
			dataFileName = null;
			hashPath = null;
			}



		// Group: Properties
		// __________________________________________________________________________


		/* Property: Members
		 * The list of members this entry contains.  The entire list may be replaced with another one, which is
		 * useful when merging containers.
		 */
		public List<Entry> Members
			{
			get
				{  return members;  }
			}

		/* Property: JSONBeforeMembers
		 * The generated JSON of this entry up to the point where its members would appear.
		 */
		public string JSONBeforeMembers
			{
			get
				{  return jsonBeforeMembers;  }
			set
				{  jsonBeforeMembers = value;  }
			}

		/* Property: JSONAfterMembers
		 * The generated JSON of this entry after the point where its members would appear.
		 */
		public string JSONAfterMembers
			{
			get
				{  return jsonAfterMembers;  }
			set
				{  jsonAfterMembers = value;  }
			}

		/* Property: JSONLengthOfMembers
		 * The calculated total JSON length of all members stored directly in this container.  It does NOT recurse into deeper
		 * containers.
		 */
		public int JSONLengthOfMembers
			{
			get
				{
				int jsonLengthOfMembers = 0;

				foreach (var member in members)
					{
					if (member is Container)
						{
						Container memberContainer = member as Container;
						jsonLengthOfMembers += memberContainer.JSONBeforeMembers.Length + memberContainer.JSONAfterMembers.Length;
						}
					else
						{
						Target memberTarget = member as Target;
						jsonLengthOfMembers += memberTarget.JSON.Length;
						}
					}

				return jsonLengthOfMembers;
				}
			}

		/* Property: StartsNewDataFile
		 * Whether this container starts a new data file.  This property is read-only.  If you need to change
		 * it, set <DataFileName> instead.
		 */
		public bool StartsNewDataFile
			{
			get
				{  return (dataFileName != null);  }
			}

		/* Property: DataFileName
		 * If this container starts a new data file this will be its file name, such as "files2.js" or "classes.js".  It will
		 * not include a path.  If this container doesn't start a new data file, this will be null.
		 */
		public string DataFileName
			{
			get
				{  return dataFileName;  }
			set
				{  dataFileName = value;  }
			}

		/* Property: HashPath
		 * The hash path of the container, or null if none.
		 */
		public string HashPath
			{
			get
				{  return hashPath;  }
			set
				{  hashPath = value;  }
			}



		// Group: Variables
		// __________________________________________________________________________


		/* var: members
		 */
		protected List<Entry> members;

		/* var: jsonBeforeMembers
		 * The generated JSON for this entry, up to the point where its members would be inserted.
		 */
		protected string jsonBeforeMembers;

		/* var: jsonAfterMembers
		 * The generated JSON for this entry, after the point where its members would be inserted.
		 */
		protected string jsonAfterMembers;

		/* var: dataFileName
		 * If this container starts a new data file this will be its file name, such as "files2.js" or "classes.js".  It will
		 * not include a path.  If this container doesn't start a new data file, this will be null.
		 */
		protected string dataFileName;

		/* var: hashPath
		 * The hash path of the container, or null if none.
		 */
		protected string hashPath;
		}
	}
