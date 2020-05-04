/* 
 * Class: CodeClear.NaturalDocs.Engine.Links.Resolver
 * ____________________________________________________________________________
 * 
 * A process which handles resolving links due to topics changing or new links being added.
 * 
 * 
 * Multithreading: Thread Safety Notes
 * 
 *		Externally, this class is thread safe.
 *		
 *		Internally, all variable accesses must use a monitor on <accessLock>.
 *		
 */

// This file is part of Natural Docs, which is Copyright © 2003-2020 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using CodeClear.NaturalDocs.Engine.CodeDB;
using CodeClear.NaturalDocs.Engine.IDObjects;
using CodeClear.NaturalDocs.Engine.Symbols;
using CodeClear.NaturalDocs.Engine.Topics;


namespace CodeClear.NaturalDocs.Engine.Links
	{
	public class Resolver : Process
		{
		
		// Group: Initialization Functions
		// __________________________________________________________________________
		

		/* Function: Resolver
		 */
		public Resolver (Engine.Instance engineInstance) : base (engineInstance)
			{
			changesBeingProcessed = 0;
			accessLock = new object();
			}
			
		override protected void Dispose (bool strictRulesApply)
			{
			if (!strictRulesApply)
				{
				if (changesBeingProcessed != 0)
					{  throw new Exception("Links.Resolver shut down while links were still being processed.");  }
				}
			}



		// Group: Resolving Functions
		// __________________________________________________________________________


		/* Function: WorkOnResolvingLinks
		 * 
		 * Works on resolving links due to topics changing or new links being added.  This is a parallelizable task so multiple 
		 * threads can call this function and the work will be divided up between them.
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
					linkID = PickLinkID();

					if (linkID != 0)
						{
						ResolveLink(linkID, accessor);
						FinalizeLinkID(linkID);

						if (accessor.LockHeld == Accessor.LockType.ReadWrite)
							{  accessor.DowngradeToReadPossibleWriteLock();  }
						}

					else if (PickNewTopics(out topicIDs, out endingSymbol))
						{
						ResolveNewTopics(topicIDs, endingSymbol, accessor);
						FinalizeNewTopics(topicIDs, endingSymbol);

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
				long score = Manager.Score(link, topic, bestMatchScore, alternateInterpretations);

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
		 * Goes through the IDs of newly created <Topics> and sees if they serve as better targets for any existing links.
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
		protected void ResolveNewTopics (NumberSet topicIDs, EndingSymbol endingSymbol, Accessor accessor)
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
						long score = Manager.Score(link, topic, bestMatchScore, alternateInterpretations);

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


		/* Function: GetStatus
		 * Fills the passed object with the status of <WorkOnResolvingLinks()>.  This will be a snapshot of its progress rather than
		 * a live object, so the values won't change out from under you.
		 */
		public void GetStatus(ref ResolverStatus statusTarget)
			{
			lock (accessLock)
				{
				statusTarget.ChangesBeingProcessed = changesBeingProcessed;
				statusTarget.ChangesRemaining = Manager.UnprocessedChanges.Count;
				}
			}



		// Group: Pick Functions
		// __________________________________________________________________________


		/* Function: PickLinkID
		 * Returns a link ID that needs to be processed, or zero if there aren't any.  All link IDs should be passed to 
		 * <FinalizeLinkID()> after being resolved.
		 */
		protected int PickLinkID ()
			{
			lock (accessLock)
				{
				int linkID = Manager.UnprocessedChanges.PickLinkID();

				if (linkID != 0)
					{
					// DEPENDENCY: Make sure all changes to changesBeingProcessed match the system used in UnprocessedChanges.Count
					changesBeingProcessed++;
					}

				return linkID;
				}
			}


		/* Function: PickNewTopics
		 * Returns the IDs for a batch of new topics and their shared ending symbol, or false if there aren't any.  This allows 
		 * you to process new topics that could potentially serve as better definitions to existing links.  You must pass the values
		 * to <FinalizeNewTopics()> after resolving them.
		 */
		protected bool PickNewTopics (out IDObjects.NumberSet topicIDs, out EndingSymbol endingSymbol)
			{
			lock (accessLock)
				{
				if (Manager.UnprocessedChanges.PickNewTopics(out topicIDs, out endingSymbol))
					{
					// DEPENDENCY: Make sure all changes to changesBeingProcessed match the system used in UnprocessedChanges.Count
					changesBeingProcessed += topicIDs.Count;
					return true;
					}
				else
					{  return false;  }
				}
			}
			

		/* Function: FinalizeLinkID
		 * Finalizes processing of a link ID that was resolved.
		 */
		protected void FinalizeLinkID (int linkID)
			{
			if (linkID == 0)
				{  return;  }

			lock (accessLock)
				{
				// DEPENDENCY: Make sure all changes to changesBeingProcessed match the system used in UnprocessedChanges.Count
				changesBeingProcessed--;
				}
			}


		/* Function: FinalizeNewTopics
		 * Finalizes processing of a set of new topics and their shared ending symbol.
		 */
		protected void FinalizeNewTopics (IDObjects.NumberSet topicIDs, EndingSymbol endingSymbol)
			{
			lock (accessLock)
				{
				// DEPENDENCY: Make sure all changes to changesBeingProcessed match the system used in UnprocessedChanges.Count
				changesBeingProcessed -= topicIDs.Count;
				}
			}
			


		// Group: Properties
		// __________________________________________________________________________


		public Links.Manager Manager
			{
			get
				{  return EngineInstance.Links;  }
			}

		

		// Group: Variables
		// __________________________________________________________________________
		

		/* var: changesBeingProcessed
		 * 
		 * The number of changes currently being worked on across all threads.  Make sure all changes to this value match the 
		 * system used in <UnprocessedChanges.Count>.
		 * 
		 * Thread Safety:
		 * 
		 *		You must hold <accessLock> to use this variable.
		 */
		protected int changesBeingProcessed;


		/* var: accessLock
		 * 
		 * An object used for a monitor that prevents more than one thread from accessing any of the variables at a time.
		 * 
		 * Rationale:
		 * 
		 *		Since the only variable is <changesBeingProcessed>, why not just use atomic operations in 
		 *		System.Threading.Interlocked instead?  Well, it could cause a race condition where <GetStatus()> catches a moment
		 *		between when a thread has picked something out of <Links.UnprocessedChanges> and before it increments
		 *		<changesBeingProcessed>.  That means the value could not reflect some work being done.  This could cause the total
		 *		to decrement in one moment and increment in another instead of continuously going down.
		 */
		protected object accessLock;

		}
	}