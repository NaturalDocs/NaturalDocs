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

			// Reset immediately before doing any work
			beforeFirstResolve = false;


			Accessor accessor = EngineInstance.CodeDB.GetAccessor();

			try
				{
				accessor.GetReadPossibleWriteLock();

				int linkID;
				EndingSymbol endingSymbol;
				NumberSet topicIDs;

				while (!cancelled())
					{
					if (PickLinkIDToResolve(out linkID))
						{
						ResolveLink(linkID, accessor);

						if (accessor.LockHeld == Accessor.LockType.ReadWrite)
							{  accessor.DowngradeToReadPossibleWriteLock();  }
						}

					else if (PickEndingSymbolToResolve(out endingSymbol, out topicIDs))
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


		/* Function: PickLinkIDToResolve
		 * If <linksToResolve> isn't empty it will remove one from the list and return true.  Otherwise returns false.
		 * Handles locking automatically.
		 */
		protected bool PickLinkIDToResolve (out int linkID)
			{
			lock (linksToResolve)
				{  
				linkID = linksToResolve.Pop();
				return (linkID != 0);
				}
			}


		/* Function: PickEndingSymbolToResolve
		 * If <newTopicIDsByEndingSymbol> isn't empty it will remove one from the list and return true.  Otherwise 
		 * returns false.  Handles locking automatically.
		 */
		protected bool PickEndingSymbolToResolve (out EndingSymbol endingSymbol, out NumberSet topicIDs)
			{
			lock (newTopicIDsByEndingSymbol)
				{
				if (newTopicIDsByEndingSymbol.Count == 0)
					{
					endingSymbol = default(EndingSymbol);
					topicIDs = null;
					return false;
					}
				else
					{
					var enumerator = newTopicIDsByEndingSymbol.GetEnumerator();
					enumerator.MoveNext();  // It's not positioned on the first element by default.

					endingSymbol = enumerator.Current.Key;
					topicIDs = enumerator.Current.Value;

					newTopicIDsByEndingSymbol.Remove(endingSymbol);

					return true;
					}
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
			long units = 0;
			
			lock (linksToResolve)
				{  units += linksToResolve.Count;  }

			lock (newTopicIDsByEndingSymbol)
				{
				foreach (var kvPair in newTopicIDsByEndingSymbol)
					{  units += kvPair.Value.Count;  }
				}

			return units;
			}



		// Group: CodeDB.IChangeWatcher Functions
		// __________________________________________________________________________


		public void OnAddTopic (Topic topic, EventAccessor eventAccessor)
			{
			// We don't need to track newTopicIDsByEndingSymbol when we're reparsing everything and nothing has been resolved
			// yet because the entire codebase will be reparsed and therefore every link will be re-added.  Every link will thus be on
			// linksToResolve and we don't need to worry about new topics causing existing links to need to be reresolved.

			if ( (beforeFirstResolve && EngineInstance.Config.ReparseEverything) == false)
				{
				lock (newTopicIDsByEndingSymbol)
					{
					IDObjects.NumberSet newTopicIDs = newTopicIDsByEndingSymbol[topic.Symbol.EndingSymbol];

					if (newTopicIDs == null)
						{
						newTopicIDs = new IDObjects.NumberSet();
						newTopicIDsByEndingSymbol.Add(topic.Symbol.EndingSymbol, newTopicIDs);
						}

					newTopicIDs.Add(topic.TopicID);
					}
				}
			}
		

		public void OnUpdateTopic (Topic oldTopic, Topic newTopic, Topic.ChangeFlags changeFlags, EventAccessor eventAccessor)
			{
			}
		

		public void OnDeleteTopic (Topic topic, IDObjects.NumberSet linksAffected, EventAccessor eventAccessor)
			{
			lock (linksToResolve)
				{  linksToResolve.Add(linksAffected);  }


			// Check newTopicIDsByEndingSymbol just in case.  We don't want to leave any references to a deleted topic.

			lock (newTopicIDsByEndingSymbol)
				{
				IDObjects.NumberSet newTopicIDs = newTopicIDsByEndingSymbol[topic.Symbol.EndingSymbol];

				if (newTopicIDs != null)
					{  newTopicIDs.Remove(topic.TopicID);  }
				}
			}

		
		public void OnAddLink (Link link, EventAccessor eventAccessor)
			{
			lock (linksToResolve)
				{  linksToResolve.Add(link.LinkID);  }
			}

		
		public void OnChangeLinkTarget (Link link, int oldTargetTopicID, int oldTargetClassID, EventAccessor eventAccessor)
			{
			// We're going to be the one causing this event, not responding to it.  No other code should be changing link definitions.
			}

		
		public void OnDeleteLink (Link link, EventAccessor eventAccessor)
			{
			// Just in case, so there's no hanging references
			
			lock (linksToResolve)
				{  linksToResolve.Remove(link.LinkID);  }
			}

		public void OnAddImageLink (ImageLink imageLink, CodeDB.EventAccessor eventAccessor)
			{
			// xxx placeholder
			}

		public void OnChangeImageLinkTarget (ImageLink imageLink, int oldTargetFileID, CodeDB.EventAccessor eventAccessor)
			{
			// xxx placeholder
			}

		public void OnDeleteImageLink (ImageLink imageLink, CodeDB.EventAccessor eventAccessor)
			{
			// xxx placeholder
			}

		}
	}
