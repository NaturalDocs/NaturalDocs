/* 
 * Class: CodeClear.NaturalDocs.Engine.Links.Manager
 */

// This file is part of Natural Docs, which is Copyright © 2003-2020 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Threading;
using CodeClear.NaturalDocs.Engine.CodeDB;
using CodeClear.NaturalDocs.Engine.IDObjects;
using CodeClear.NaturalDocs.Engine.Symbols;
using CodeClear.NaturalDocs.Engine.Topics;


namespace CodeClear.NaturalDocs.Engine.Links
	{
	public partial class Manager
		{

		// Group: Resolving Functions
		// __________________________________________________________________________


		/* Function: WorkOnResolvingLinks
		 * 
		 * Works on the task of resolving links due to topics changing or new links being added.  This is a parallelizable 
		 * task so multiple threads can call this function and the work will be divided up between them.
		 * 
		 * This function returns if it's cancelled or there is no more work to be done.  If there is only one thread working 
		 * on this then the task is complete, but if there are multiple threads the task isn't complete until they all have 
		 * returned.  This one may have returned because there was no more work for this thread to do, but other threads 
		 * are still working.
		 */
		public void WorkOnResolvingLinks (CancelDelegate cancelled)
			{
			Accessor accessor = EngineInstance.CodeDB.GetAccessor();

			try
				{
				accessor.GetReadPossibleWriteLock();

				int linkID;
				EndingSymbol endingSymbol;
				NumberSet topicIDs;

				while (!cancelled())
					{
					linkID = unprocessedChanges.PickLinkID();

					if (linkID != 0)
						{
						ResolveLink(linkID, accessor);

						if (accessor.LockHeld == Accessor.LockType.ReadWrite)
							{  accessor.DowngradeToReadPossibleWriteLock();  }
						}

					else if (unprocessedChanges.PickNewTopics(out topicIDs, out endingSymbol))
						{
						ResolveNewTopics(topicIDs, endingSymbol, accessor);

						if (accessor.LockHeld == Accessor.LockType.ReadWrite)
							{  accessor.DowngradeToReadPossibleWriteLock();  }
						}

					else
						{  break;  }
					}
				}
			finally
				{  
				if (accessor.LockHeld != Accessor.LockType.None)
					{  accessor.ReleaseLock();  }

				accessor.Dispose();
				}
			}


		/* Function: ResolveLink
		 * 
		 * Calculates a new target for the passed link ID.
		 * 
		 * Requirements:
		 * 
		 *		- Requires the accessor to have at least a read/possible write lock.  If the link target changes it will be upgraded to
		 *		  read/write automatically.
		 *		
		 */
		protected void ResolveLink (int linkID, Accessor accessor)
			{
			Link link = accessor.GetLinkByID(linkID, Accessor.GetLinkFlags.DontLookupClasses);
			List<EndingSymbol> endingSymbols = accessor.GetAlternateLinkEndingSymbols(linkID);

			if (endingSymbols == null)
				{  endingSymbols = new List<EndingSymbol>();  }

			endingSymbols.Add(link.EndingSymbol);

			// We only need the body's length, not its contents.
			List<Topic> topics = accessor.GetTopicsByEndingSymbol(endingSymbols, Delegates.NeverCancel, 
																							 Accessor.GetTopicFlags.BodyLengthOnly |
																							 Accessor.GetTopicFlags.DontLookupClasses |
																							 Accessor.GetTopicFlags.DontLookupContexts);

			List<LinkInterpretation> alternateInterpretations = null;

			if (link.Type == LinkType.NaturalDocs)
				{
				string ignore;
				alternateInterpretations = EngineInstance.Comments.NaturalDocsParser.LinkInterpretations(link.Text, 
																							Comments.Parsers.NaturalDocs.LinkInterpretationFlags.FromOriginalText |
																							Comments.Parsers.NaturalDocs.LinkInterpretationFlags.AllowNamedLinks |
																							Comments.Parsers.NaturalDocs.LinkInterpretationFlags.AllowPluralsAndPossessives,
																							out ignore);

				}
	
			int bestMatchTopicID = 0;
			int bestMatchClassID = 0;
			long bestMatchScore = 0;

			foreach (Topic topic in topics)
				{
				long score = Score(link, topic, bestMatchScore, alternateInterpretations);

				if (score > bestMatchScore)
					{
					bestMatchTopicID = topic.TopicID;
					bestMatchClassID = topic.ClassID;
					bestMatchScore = score;
					}
				}

			if (bestMatchTopicID != link.TargetTopicID || 
				bestMatchClassID != link.TargetClassID ||
				bestMatchScore != link.TargetScore)
				{
				int oldTargetTopicID = link.TargetTopicID;
				int oldTargetClassID = link.TargetClassID;

				link.TargetTopicID = bestMatchTopicID;
				link.TargetClassID = bestMatchClassID;
				link.TargetScore = bestMatchScore;

				accessor.UpdateLinkTarget(link, oldTargetTopicID, oldTargetClassID);
				}
			}


		/* Function: ResolveNewTopics
		 * 
		 * Goes through the IDs of newly created <Topics> and sees if they serve as better targets for any existing
		 * links.
		 * 
		 * Parameters:
		 * 
		 *		topicIDs - The set of IDs to check.  Every <Topic> represented here must have the same <EndingSymbol>.
		 *		endingSymbol - The <EndingSymbol> shared by all of the topic IDs.
		 *		accessor - The <Accessor> used for the database.
		 * 
		 * Requirements:
		 * 
		 *		- Requires the accessor to have at least a read/possible write lock.  If the link changes it will be upgraded to
		 *		  read/write automatically.
		 *		
		 */
		protected void ResolveNewTopics (IDObjects.NumberSet topicIDs, EndingSymbol endingSymbol, Accessor accessor)
			{
			// We only need the body's length, not its contents.
			List<Topic> topics = accessor.GetTopicsByID(topicIDs, Delegates.NeverCancel, 
																			Accessor.GetTopicFlags.BodyLengthOnly |
																			Accessor.GetTopicFlags.DontLookupClasses |
																			Accessor.GetTopicFlags.DontLookupContexts);
			List<Link> links = accessor.GetLinksByEndingSymbol(endingSymbol, Delegates.NeverCancel,
																						Accessor.GetLinkFlags.DontLookupClasses);

			// Go through each link and see if any of the topics serve as a better target.  It's better for the links to be the outer loop 
			// because we can generate alternate interpretations only once per link.

			foreach (Link link in links)
				{
				List<LinkInterpretation> alternateInterpretations = null;

				if (link.Type == LinkType.NaturalDocs)
					{
					string ignore;
					alternateInterpretations = EngineInstance.Comments.NaturalDocsParser.LinkInterpretations(link.Text, 
																						Comments.Parsers.NaturalDocs.LinkInterpretationFlags.FromOriginalText |
																						Comments.Parsers.NaturalDocs.LinkInterpretationFlags.AllowNamedLinks |
																						Comments.Parsers.NaturalDocs.LinkInterpretationFlags.AllowPluralsAndPossessives,
																						out ignore);

					}
	
				int bestMatchTopicID = link.TargetTopicID;
				int bestMatchClassID = link.TargetClassID;
				long bestMatchScore = link.TargetScore;

				foreach (Topic topic in topics)
					{
					// No use rescoring the existing target.
					if (topic.TopicID != link.TargetTopicID)
						{
						long score = EngineInstance.Links.Score(link, topic, bestMatchScore, alternateInterpretations);

						if (score > bestMatchScore)
							{
							bestMatchTopicID = topic.TopicID;
							bestMatchClassID = topic.ClassID;
							bestMatchScore = score;
							}
						}
					}

				if (bestMatchTopicID != link.TargetTopicID || 
					bestMatchClassID != link.TargetClassID ||
					bestMatchScore != link.TargetScore)
					{
					int oldTargetTopicID = link.TargetTopicID;
					int oldTargetClassID = link.TargetClassID;

					link.TargetTopicID = bestMatchTopicID;
					link.TargetClassID = bestMatchClassID;
					link.TargetScore = bestMatchScore;

					accessor.UpdateLinkTarget(link, oldTargetTopicID, oldTargetClassID);
					}
				}
			}


		/* Function: UnitsOfWorkRemaining
		 * Returns a number representing how much work is left to be done by <WorkOnResolvingLinks()>.  What tasks the 
		 * units represent can vary, so this is intended simply to allow a percentage to be calculated.
		 */
		public long UnitsOfWorkRemaining ()
			{
			return unprocessedChanges.Count;
			}

		}
	}
