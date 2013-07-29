/* 
 * Class: GregValure.NaturalDocs.Engine.SearchIndex.KeywordEntry
 * ____________________________________________________________________________
 * 
 * A single keyword entry in the search index with it's associated <TopicEntries>.
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2013 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;


namespace GregValure.NaturalDocs.Engine.SearchIndex
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
		 * The keyword associated with this entry.
		 */
		public string Keyword
			{
			get
				{  return keyword;  }
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