/*
 * Class: CodeClear.NaturalDocs.Engine.CodeDB.EventAccessor
 */

// This file is part of Natural Docs, which is Copyright © 2003-2024 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Text;
using CodeClear.NaturalDocs.Engine.IDObjects;
using CodeClear.NaturalDocs.Engine.Links;


namespace CodeClear.NaturalDocs.Engine.CodeDB
	{
	public partial class EventAccessor
		{

		/* Function: GetFileIDsThatDefineClassID
		 * Returns the file IDs that contain topics which define the class ID.
		 */
		public NumberSet GetFileIDsThatDefineClassID (int classID)
			{
			accessor.RequireAtLeast(Accessor.LockType.ReadOnly);

			NumberSet fileIDs = new NumberSet();

			using (SQLite.Query query = accessor.Connection.Query("SELECT FileID FROM Topics WHERE ClassID=? AND DefinesClass=1", classID))
				{
				while (query.Step())
					{
					fileIDs.Add(query.IntColumn(0));
					}
				}

			return fileIDs;
			}


		/* Function: GetInfoOnClassParents
		 * Looks up the parents of the passed class ID and returns their class IDs and all the file IDs that define them.
		 */
		public void GetInfoOnClassParents (int classID, out NumberSet parentClassIDs, out NumberSet parentClassFileIDs)
			{
			accessor.RequireAtLeast(Accessor.LockType.ReadOnly);

			parentClassIDs = new NumberSet();
			parentClassFileIDs = new NumberSet();

			using (SQLite.Query query = accessor.Connection.Query("SELECT TargetClassID FROM Links " +
																							   "WHERE ClassID=? AND Type=? AND TargetClassID != 0",
																							   classID, (int)Links.LinkType.ClassParent))
				{
				while (query.Step())
					{  parentClassIDs.Add(query.IntColumn(0));  }
				}

			if (parentClassIDs.IsEmpty)
				{  return;  }

			IDObjects.NumberSet remainingParentClassIDs = parentClassIDs;

			do
				{
				IDObjects.NumberSet temp;
				string queryText = "SELECT FileID FROM Topics WHERE " + Accessor.ColumnIsInNumberSetExpression("ClassID", remainingParentClassIDs, out temp) + " AND DefinesClass=1";
				remainingParentClassIDs = temp;

				using (SQLite.Query query = accessor.Connection.Query(queryText))
					{
					while (query.Step())
						{  parentClassFileIDs.Add(query.IntColumn(0));  }
					}
				}
			while (remainingParentClassIDs != null);
			}


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


			// First find all the topics that have the link in the summary.  We can restrict the search to the same file ID as the link because
			// links appearing in a different file would be seen as a different link.

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


			// Now find all the links that resolve to these topics.  We only need their file and class IDs though.

			IDObjects.NumberSet remainingTopicIDs = topicIDs;

			do
				{
				IDObjects.NumberSet temp;
				string queryText = "SELECT FileID, ClassID FROM Links WHERE " + Accessor.ColumnIsInNumberSetExpression("TargetTopicID", remainingTopicIDs, out temp);
				remainingTopicIDs = temp;

				using (SQLite.Query query = accessor.Connection.Query(queryText))
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
			while (remainingTopicIDs != null);
			}


		/* Function: GetInfoOnLinksToTopicsWithImageLinkInSummary
		 *
		 * What the hell?  Okay, check this out: First it finds the topics which have the passed image link in their summaries.
		 * Then it returns aggregate information on all links that resolve to any of those topics.  This is needed for keeping
		 * tooltips accurate with differential building.  It makes sense, trust me.
		 *
		 * Parameters:
		 *
		 *		imageLink - The image link to look up.
		 *		fileIDs - The file IDs of all the links that resolve to the topics that have the link in the summary.  Will be null if none.
		 *		classIDs - The class IDs of all the links that resolve to the topics that have the link in the summary.  Will be null if none.
		 *
		 */
		public void GetInfoOnLinksToTopicsWithImageLinkInSummary (ImageLink imageLink, out IDObjects.NumberSet fileIDs, out IDObjects.NumberSet classIDs)
			{
			accessor.RequireAtLeast(Accessor.LockType.ReadOnly);

			fileIDs = null;
			classIDs = null;


			// First find all the topics that have the image link in the summary.  We can restrict the search to the same file ID as the link
			// because links appearing in a different file would be seen as a different link.  Only inline image links can appear in the
			// summary so we don't need to orry about standalone ones.

			IDObjects.NumberSet topicIDs = null;
			string likeText = "%<image type=\"inline\" originaltext=\"" + imageLink.OriginalText.EntityEncode() + "\"%";

			using (SQLite.Query query = accessor.Connection.Query("SELECT TopicID FROM Topics WHERE FileID=? AND Summary LIKE ?",
																							   imageLink.FileID, likeText))
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


			// Now find all the links that resolve to these topics.  We only need their file and class IDs though.

			IDObjects.NumberSet remainingTopicIDs = topicIDs;

			do
				{
				IDObjects.NumberSet temp;
				string queryText = "SELECT FileID, ClassID FROM Links WHERE " + Accessor.ColumnIsInNumberSetExpression("TargetTopicID", remainingTopicIDs, out temp);
				remainingTopicIDs = temp;

				using (SQLite.Query query = accessor.Connection.Query(queryText))
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
			while (remainingTopicIDs != null);
			}

		}
	}
