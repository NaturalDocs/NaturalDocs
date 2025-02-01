/*
 * Class: CodeClear.NaturalDocs.Engine.Output.HTML.SearchIndex.Entries.Keyword
 * ____________________________________________________________________________
 *
 * A single keyword entry in the search index with it's associated <Topics>.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2025 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;


namespace CodeClear.NaturalDocs.Engine.Output.HTML.SearchIndex.Entries
	{
	public class Keyword : Entry
		{

		// Group: Functions
		// __________________________________________________________________________


		public Keyword (string displayName) : base ()
			{
			this.displayName = displayName;
			this.searchText = Normalize(displayName);

			this.topicEntries = new List<Topic>();
			}



		// Group: Properties
		// __________________________________________________________________________


		/* Property: DisplayName
		 * The keyword associated with this entry, as it should appear in the output.  After the object is created it can only be changed to
		 * the  same keyword with different capitalization.
		 */
		public string DisplayName
			{
			get
				{  return displayName;  }
			set
				{
				#if DEBUG
				if (string.Compare(value, displayName, true) != 0)
					{  throw new Exception("Can only change a keyword's display name to the same string with different capitalization.");  }
				#endif

				displayName = value;
				searchText = Normalize(displayName);
				}
			}

		/* Property: SearchText
		 * The search text associated with this entry, which is <DisplayName> normalized for search.  See <Entry.Normalize()> for the
		 * details.
		 */
		public string SearchText
			{
			get
				{  return searchText;  }
			}

		/* Property: TopicEntries
		 * The <TopicEntries> that are associated with this keyword.
		 */
		public List<Topic> TopicEntries
			{
			get
				{  return topicEntries;  }
			}



		// Group: Variables
		// __________________________________________________________________________

		protected string displayName;
		protected string searchText;
		protected List<Topic> topicEntries;

		}
	}
