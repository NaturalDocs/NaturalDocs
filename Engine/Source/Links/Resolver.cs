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

// This file is part of Natural Docs, which is Copyright © 2003-2021 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using CodeClear.NaturalDocs.Engine.CodeDB;
using CodeClear.NaturalDocs.Engine.Files;
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

				while (!cancelled())
					{

					// Links

					int linkID = PickLinkID();

					if (linkID != 0)
						{
						ResolveLink(linkID, accessor);
						FinalizeLinkID(linkID);

						if (accessor.LockHeld == Accessor.LockType.ReadWrite)
							{  accessor.DowngradeToReadPossibleWriteLock();  }

						continue;
						}


					// New topics

					NumberSet topicIDs;
					EndingSymbol endingSymbol;

					if (PickNewTopics(out topicIDs, out endingSymbol))
						{
						ResolveNewTopics(topicIDs, endingSymbol, accessor);
						FinalizeNewTopics(topicIDs, endingSymbol);

						if (accessor.LockHeld == Accessor.LockType.ReadWrite)
							{  accessor.DowngradeToReadPossibleWriteLock();  }

						continue;
						}


					// Image links

					int imageLinkID = PickImageLinkID();

					if (imageLinkID != 0)
						{
						ResolveImageLink(imageLinkID, accessor);
						FinalizeImageLinkID(imageLinkID);

						if (accessor.LockHeld == Accessor.LockType.ReadWrite)
							{  accessor.DowngradeToReadPossibleWriteLock();  }

						continue;
						}


					// New image files

					NumberSet imageFileIDs;
					string lcFileName;

					if (PickNewImageFiles(out imageFileIDs, out lcFileName))
						{
						ResolveNewImageFiles(imageFileIDs, lcFileName, accessor);
						FinalizeNewImageFiles(imageFileIDs, lcFileName);

						if (accessor.LockHeld == Accessor.LockType.ReadWrite)
							{  accessor.DowngradeToReadPossibleWriteLock();  }

						continue;
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


		/* Function: ResolveImageLink
		 * 
		 * Calculates a new target for the passed image link ID.
		 * 
		 * Requirements:
		 * 
		 *		- Requires the accessor to have at least a read/possible write lock.  If the link target changes it will be upgraded to
		 *		  read/write automatically.
		 *		
		 */
		protected void ResolveImageLink (int imageLinkID, Accessor accessor)
			{
			ImageLink imageLink = accessor.GetImageLinkByID(imageLinkID, Accessor.GetImageLinkFlags.DontLookupClasses);

			int bestMatchFileID = 0;
			int bestMatchScore = 0;


			// First try the path relative to the source file.

			Path pathRelativeToSourceFile = EngineInstance.Files.FromID(imageLink.FileID).FileName.ParentFolder + '/' + imageLink.Path;
			File fileRelativeToSourceFile = EngineInstance.Files.FromPath(pathRelativeToSourceFile);

			if (fileRelativeToSourceFile != null && fileRelativeToSourceFile.Deleted == false)
				{
				int score = Manager.Score(imageLink, fileRelativeToSourceFile, bestMatchScore);

				if (score > bestMatchScore)
					{
					bestMatchScore = score;
					bestMatchFileID = fileRelativeToSourceFile.ID;
					}
				}


			// Next try the path relative to any image folders.

			foreach (var fileSource in EngineInstance.Files.FileSources)
				{
				if (fileSource.Type == InputType.Image)
					{
					Path pathRelativeToFileSource = fileSource.MakeAbsolute(imageLink.Path);
					File fileRelativeToFileSource = EngineInstance.Files.FromPath(pathRelativeToFileSource);

					if (fileRelativeToFileSource != null && fileRelativeToFileSource.Deleted == false)
						{
						int score = Manager.Score(imageLink, fileRelativeToFileSource, bestMatchScore);

						if (score > bestMatchScore)
							{
							bestMatchScore = score;
							bestMatchFileID = fileRelativeToFileSource.ID;
							}
						}
					}
				}


			// Update the database

			if (bestMatchFileID != imageLink.TargetFileID ||
				bestMatchScore != imageLink.TargetScore)
				{
				int oldTargetFileID = imageLink.TargetFileID;

				imageLink.TargetFileID = bestMatchFileID;
				imageLink.TargetScore = bestMatchScore;

				accessor.UpdateImageLinkTarget(imageLink, oldTargetFileID);
				}
			}


		/* Function: ResolveNewImageFiles
		 * 
		 * Goes through the IDs of newly created image files and sees if they serve as better targets for any existing links.
		 * 
		 * Parameters:
		 * 
		 *		imageFileIDs - The set of IDs to check.  Every file represented here must have the same lowercase file name.
		 *		lcFileName - The all lowercase file name shared by all of the file IDs.  It does not include any part of the path.
		 *		accessor - The <Accessor> used for the database.
		 * 
		 * Requirements:
		 * 
		 *		- Requires the accessor to have at least a read/possible write lock.  If the link changes it will be upgraded to
		 *		  read/write automatically.
		 *		
		 */
		protected void ResolveNewImageFiles (NumberSet imageFileIDs, string lcFileName, Accessor accessor)
			{
			List<ImageLink> imageLinks = accessor.GetImageLinksByFileName(lcFileName, Delegates.NeverCancel,
																											  Accessor.GetImageLinkFlags.DontLookupClasses);


			// Go through each image link and see if any of the new image files serve as a better target.

			foreach (var imageLink in imageLinks)
				{
				int bestMatchFileID = imageLink.TargetFileID;
				int bestMatchScore = imageLink.TargetScore;

				foreach (int imageFileID in imageFileIDs)
					{
					int newScore = Manager.Score(imageLink, EngineInstance.Files.FromID(imageFileID), bestMatchScore);

					if (newScore > bestMatchScore)
						{
						bestMatchFileID = imageFileID;
						bestMatchScore = newScore;
						}
					}

				if (imageLink.TargetFileID != bestMatchFileID)
					{
					int oldTargetFileID = imageLink.TargetFileID;

					imageLink.TargetFileID = bestMatchFileID;
					imageLink.TargetScore = bestMatchScore;

					accessor.UpdateImageLinkTarget(imageLink, oldTargetFileID);
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
			

		/* Function: PickImageLinkID
		 * Returns an image link ID that needs to be processed, or zero if there aren't any.  All image link IDs should be passed to 
		 * <FinalizeImageLinkID()> after being resolved.
		 */
		protected int PickImageLinkID ()
			{
			lock (accessLock)
				{
				int imageLinkID = Manager.UnprocessedChanges.PickImageLinkID();

				if (imageLinkID != 0)
					{
					// DEPENDENCY: Make sure all changes to changesBeingProcessed match the system used in UnprocessedChanges.Count
					changesBeingProcessed++;
					}

				return imageLinkID;
				}
			}


		/* Function: PickNewImageFiles
		 * Returns the IDs for a batch of new image files and their shared lowercase file name, or false if there aren't any.  This allows 
		 * you to process new images that could potentially serve as better definitions to existing links.  You must pass the values
		 * to <FinalizeNewImageFiles()> after resolving them.
		 */
		protected bool PickNewImageFiles (out IDObjects.NumberSet imageFileIDs, out string lcFileName)
			{
			lock (accessLock)
				{
				if (Manager.UnprocessedChanges.PickNewImageFiles(out imageFileIDs, out lcFileName))
					{
					// DEPENDENCY: Make sure all changes to changesBeingProcessed match the system used in UnprocessedChanges.Count
					changesBeingProcessed += imageFileIDs.Count;
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
			

		/* Function: FinalizeImageLinkID
		 * Finalizes processing of an image link ID that was resolved.
		 */
		protected void FinalizeImageLinkID (int imageLinkID)
			{
			if (imageLinkID == 0)
				{  return;  }

			lock (accessLock)
				{
				// DEPENDENCY: Make sure all changes to changesBeingProcessed match the system used in UnprocessedChanges.Count
				changesBeingProcessed--;
				}
			}


		/* Function: FinalizeNewImageFiles
		 * Finalizes processing of a set of new image files and their shared lowercase file name.
		 */
		protected void FinalizeNewImageFiles (IDObjects.NumberSet imageFileIDs, string lcFileName)
			{
			lock (accessLock)
				{
				// DEPENDENCY: Make sure all changes to changesBeingProcessed match the system used in UnprocessedChanges.Count
				changesBeingProcessed -= imageFileIDs.Count;
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