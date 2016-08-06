/* 
 * Class: CodeClear.NaturalDocs.Engine.SearchIndex.KeywordEntry
 * ____________________________________________________________________________
 * 
 * A single keyword entry in the search index with it's associated <TopicEntries>.
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2016 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;


namespace CodeClear.NaturalDocs.Engine.SearchIndex
	{
	public class KeywordEntry : Entry
		{

		// Group: Functions
		// __________________________________________________________________________


		public KeywordEntry (string keyword) : base ()
			{
			this.keyword = keyword;
			this.topicEntries = new List<TopicEntry>();
			}



		// Group: Properties
		// __________________________________________________________________________


		/* Property: Keyword
		 * The keyword associated with this entry.  After the object is created it can only be changed to the same keyword with
		 * different capitalization.
		 */
		public string Keyword
			{
			get
				{  return keyword;  }
			set
				{
				#if DEBUG
				if (string.Compare(value, keyword, true) != 0)
					{  throw new Exception("Can only change the keyword in a KeywordEntry to the same string with different capitalization.");  }
				#endif

				keyword = value;
				}
			}

		/* Property: SearchText
		 * The search text associated with this entry, which is <Keyword> normalized for search.
		 */
		public string SearchText
			{
			get
				{  return Normalize(keyword);  }
			}

		/* Property: TopicEntries
		 * The <TopicEntries> that are associated with this keyword.
		 */
		public List<TopicEntry> TopicEntries
			{
			get
				{  return topicEntries;  }
			}



		// Group: Variables
		// __________________________________________________________________________

		protected string keyword;
		protected List<TopicEntry> topicEntries;

		}
	}