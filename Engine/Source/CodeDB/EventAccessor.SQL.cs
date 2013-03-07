/* 
 * Class: GregValure.NaturalDocs.Engine.CodeDB.EventAccessor
 */

// This file is part of Natural Docs, which is Copyright © 2003-2012 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Text;
using GregValure.NaturalDocs.Engine.IDObjects;
using GregValure.NaturalDocs.Engine.Links;


namespace GregValure.NaturalDocs.Engine.CodeDB
	{
	public partial class EventAccessor
		{
		
		/* Function: GetInfoOnLinksThatResolveToTopicID
		 * 
		 * Returns aggregate information on all links that resolve to the passed topic ID.
		 * 
		 * Parameters:
		 * 
		 *		topicID - The topic ID to look up links for.
		 *		fileIDs - The file IDs of all the links.  Will be null if none.
		 *		classIDs - The class IDs of all the links.  Will be null if none.
		 * 
		 */
		public void GetInfoOnLinksThatResolveToTopicID (int topicID, out IDObjects.NumberSet fileIDs, out IDObjects.NumberSet classIDs)
			{
			accessor.RequireAtLeast(Accessor.LockType.ReadOnly);

			fileIDs = null;
			classIDs = null;
			
			using (SQLite.Query query = accessor.Connection.Query("SELECT FileID, ClassID FROM Links WHERE TargetTopicID=?", topicID))
				{
				while (query.Step())
					{
					if (fileIDs == null)
						{  fileIDs = new NumberSet();  }

					fileIDs.Add(query.IntColumn(0));

					int classID = query.IntColumn(1);

					if (classID != 0)
						{
						if (classIDs == null)
							{  classIDs = new NumberSet();  }

						classIDs.Add(classID);
						}
					}
				}
			}


		/* Function: GetInfoOnLinksToTopicsWithNDLinkInSummary
		 * 
		 * What the hell?  Okay, check this out: First it finds the topics which have the passed Natural Docs link in their
		 * summaries.  Then it returns aggregate information on all links that resolve to any of those topics.  This is needed
		 * for keeping tooltips accurate with differential building.  It makes sense, trust me.
		 * 
		 * Parameters:
		 * 
		 *		link - The link to look up.  It must be a Natural Docs link.
		 *		fileIDs - The file IDs of all the links that resolve to the topics that have the link in the summary.  Will be null if none.
		 *		classIDs - The class IDs of all the links that resolve to the topics that have the link in the summary.  Will be null if none.
		 * 
		 */
		public void GetInfoOnLinksToTopicsWithNDLinkInSummary (Link link, out IDObjects.NumberSet fileIDs, out IDObjects.NumberSet classIDs)
			{
			#if DEBUG
				if (link.Type != LinkType.NaturalDocs)
					{  throw new InvalidOperationException("GetInfoOnLinksToTopicsWithNDLinkInSummary() must be used with Natural Docs links.  It's right there in the title, derp.");  }
			#endif

			accessor.RequireAtLeast(Accessor.LockType.ReadOnly);

			fileIDs = null;
			classIDs = null;

			IDObjects.NumberSet topicIDs = null;
			string likeText = "%<link type=\"naturaldocs\" originaltext=\"" + link.Text.EntityEncode() + "\"%";
			
			using (SQLite.Query query = accessor.Connection.Query("SELECT TopicID FROM Topics WHERE FileID=? AND Summary LIKE ?", 
																												 link.FileID, likeText))
				{
				while (query.Step())
					{
					if (topicIDs == null)
						{  topicIDs = new NumberSet();  }

					topicIDs.Add(query.IntColumn(0));
					}
				}

			if (topicIDs == null)
				{  return;  }

			StringBuilder queryText = new StringBuilder("SELECT FileID, ClassID FROM Links WHERE ");
			List<object> queryParams = new List<object>();

			CodeDB.Accessor.AppendWhereClause_ColumnIsInNumberSet("TargetTopicID", topicIDs, queryText, queryParams);

			using (SQLite.Query query = accessor.Connection.Query(queryText.ToString(), queryParams.ToArray()))
				{
				while (query.Step())
					{
					if (fileIDs == null)
						{  fileIDs = new NumberSet();  }

					fileIDs.Add(query.IntColumn(0));

					int classID = query.IntColumn(1);

					if (classID != 0)
						{
						if (classIDs == null)
							{  classIDs = new NumberSet();  }

						classIDs.Add(classID);
						}
					}
				}
			}

		}
	}
