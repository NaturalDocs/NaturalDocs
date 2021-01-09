/* 
 * Class: CodeClear.NaturalDocs.Engine.Links.UnprocessedChanges
 * ____________________________________________________________________________
 * 
 * An object which stores all the unprocessed link changes that have been detected and allows them to be retrieved
 * for processing.
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
using CodeClear.NaturalDocs.Engine.Collections;
using CodeClear.NaturalDocs.Engine.Files;
using CodeClear.NaturalDocs.Engine.Symbols;
using CodeClear.NaturalDocs.Engine.Topics;


namespace CodeClear.NaturalDocs.Engine.Links
	{
	public class UnprocessedChanges
		{
		
		// Group: Functions
		// __________________________________________________________________________
		

		/* Function: UnprocessedChanges
		 */
		public UnprocessedChanges (bool reparsingEverything = false)
			{
			linksToResolve = new IDObjects.NumberSet();
			newTopicIDsByEndingSymbol = new SafeDictionary<Symbols.EndingSymbol, IDObjects.NumberSet>();

			imageLinksToResolve = new IDObjects.NumberSet();
			newImageFileIDsByLCFileName = new SafeDictionary<string, IDObjects.NumberSet>();

			// If we're reparsing everything we can ignore new topics and image files.  See the variable's documentation for the explanation.
			allLinksAreNew = reparsingEverything;

			accessLock = new object();
			}


		/* Function: AddLink
		 */
		public void AddLink (Link link)
			{
			lock (accessLock)
				{  linksToResolve.Add(link.LinkID);  }
			}


		/* Function: DeleteLink
		 */
		public void DeleteLink (Link link)
			{
			lock (accessLock)
				{  linksToResolve.Remove(link.LinkID);  }
			}


		/* Function: AddTopic
		 */
		public void AddTopic (Topic topic)
			{
			lock (accessLock)
				{
				if (allLinksAreNew)
					{  return;  }

				var endingSymbol = topic.Symbol.EndingSymbol;
				var newTopicIDs = newTopicIDsByEndingSymbol[endingSymbol];

				if (newTopicIDs == null)
					{
					newTopicIDs = new IDObjects.NumberSet();
					newTopicIDsByEndingSymbol[endingSymbol] = newTopicIDs;
					}

				newTopicIDs.Add(topic.TopicID);
				}
			}


		/* Function: DeleteTopic
		 */
		public void DeleteTopic (Topic topic, IDObjects.NumberSet linksAffected)
			{
			lock (accessLock)
				{
				// Reresolve affected links

				linksToResolve.Add(linksAffected);


				// Remove topic from newTopicIDsByEndingSymbol

				var endingSymbol = topic.Symbol.EndingSymbol;
				var newTopicIDs = newTopicIDsByEndingSymbol[endingSymbol];

				if (newTopicIDs != null)
					{  
					newTopicIDs.Remove(topic.TopicID);

					if (newTopicIDs.IsEmpty)
						{  newTopicIDsByEndingSymbol.Remove(endingSymbol);  }
					}
				}
			}
			

		/* Function: AddImageLink
		 */
		public void AddImageLink (ImageLink imageLink)
			{
			lock (accessLock)
				{  imageLinksToResolve.Add(imageLink.ImageLinkID);  }
			}


		/* Function: DeleteImageLink
		 */
		public void DeleteImageLink (ImageLink imageLink)
			{
			lock (accessLock)
				{  imageLinksToResolve.Remove(imageLink.ImageLinkID);  }
			}


		/* Function: AddImageFile
		 */
		public void AddImageFile (ImageFile imageFile)
			{
			lock (accessLock)
				{
				if (allLinksAreNew)
					{  return;  }

				string lcFileName = imageFile.FileName.NameWithoutPath.ToLower();
				var newImageFileIDs = newImageFileIDsByLCFileName[lcFileName];

				if (newImageFileIDs == null)
					{
					newImageFileIDs = new IDObjects.NumberSet();
					newImageFileIDsByLCFileName[lcFileName] = newImageFileIDs;
					}

				newImageFileIDs.Add(imageFile.ID);
				}
			}


		/* Function: DeleteImageFile
		 */
		public void DeleteImageFile (ImageFile imageFile, IDObjects.NumberSet linksAffected)
			{
			lock (accessLock)
				{
				// Reresolve affected links

				imageLinksToResolve.Add(linksAffected);


				// Remove topic from newImageFileIDsByLCFileName

				string lcFileName = imageFile.FileName.NameWithoutPath.ToLower();
				var newImageFileIDs = newImageFileIDsByLCFileName[lcFileName];

				if (newImageFileIDs != null)
					{  
					newImageFileIDs.Remove(imageFile.ID);

					if (newImageFileIDs.IsEmpty)
						{  newImageFileIDsByLCFileName.Remove(lcFileName);  }
					}
				}
			}
			


		// Group: Pick Functions
		// __________________________________________________________________________


		/* Function: PickLinkID
		 * Returns a link ID that needs to be processed, or zero if there aren't any.
		 */
		public int PickLinkID ()
			{
			lock (accessLock)
				{
				// Once we pick a link to resolve we can't allow this optimization anymore.  See the variable's documentation for
				// the explanation.
				allLinksAreNew = false;
				
				return linksToResolve.Pop();  
				}
			}


		/* Function: PickNewTopics
		 * Returns the IDs for a batch of new topics and their shared ending symbol, or false if there aren't any.  This allows you to process
		 * new topics that could potentially serve as better definitions to existing links.
		 */
		public bool PickNewTopics (out IDObjects.NumberSet topicIDs, out EndingSymbol endingSymbol)
			{
			lock (accessLock)
				{
				if (newTopicIDsByEndingSymbol.Count == 0)
					{
					topicIDs = null;
					endingSymbol = default(EndingSymbol);
					return false;
					}
				else
					{
					// Once links might be resolved we can't allow this optimization anymore.  See the variable's documentation for
					// the explanation.
					allLinksAreNew = false;

					var enumerator = newTopicIDsByEndingSymbol.GetEnumerator();
					enumerator.MoveNext();  // It's not positioned on the first element by default.

					endingSymbol = enumerator.Current.Key;
					topicIDs = enumerator.Current.Value;

					newTopicIDsByEndingSymbol.Remove(endingSymbol);

					return true;
					}
				}
			}


		/* Function: PickImageLinkID
		 * Returns an image link ID that needs to be processed, or zero if there aren't any.
		 */
		public int PickImageLinkID ()
			{
			lock (accessLock)
				{
				// Once we pick a link to resolve we can't allow this optimization anymore.  See the variable's documentation for
				// the explanation.
				allLinksAreNew = false;
				
				return imageLinksToResolve.Pop();  
				}
			}


		/* Function: PickNewImageFiles
		 * Returns the IDs for a batch of new image files and their shared lowercase file name, or false if there aren't any.  This allows you to 
		 * process new image files that could potentially serve as better definitions to existing links.
		 */
		public bool PickNewImageFiles (out IDObjects.NumberSet imageFileIDs, out string lcFileName)
			{
			lock (accessLock)
				{
				if (newImageFileIDsByLCFileName.Count == 0)
					{
					imageFileIDs = null;
					lcFileName = null;
					return false;
					}
				else
					{
					// Once links might be resolved we can't allow this optimization anymore.  See the variable's documentation for
					// the explanation.
					allLinksAreNew = false;

					var enumerator = newImageFileIDsByLCFileName.GetEnumerator();
					enumerator.MoveNext();  // It's not positioned on the first element by default.

					lcFileName = enumerator.Current.Key;
					imageFileIDs = enumerator.Current.Value;

					newImageFileIDsByLCFileName.Remove(lcFileName);

					return true;
					}
				}
			}



		// Group: Properties
		// __________________________________________________________________________


		/* Function: Count
		 * Returns the total number of changes, which is the number of links to resolve plus the number of new topics.
		 */
		public int Count
			{
			get
				{
				// DEPENDENCY: Make sure Resolver.changesBeingProcessed follows the same method of counting changes

				lock (accessLock)
					{
					int changes = linksToResolve.Count;

					foreach (var pair in newTopicIDsByEndingSymbol)
						{  changes += pair.Value.Count;  }

					changes += imageLinksToResolve.Count;

					foreach (var pair in newImageFileIDsByLCFileName)
						{  changes += pair.Value.Count;  }

					return changes;
					}
				}
			}



		// Group: Variables
		// __________________________________________________________________________


		/* var: linksToResolve
		 * 
		 * The IDs of all the links that need to be resolved, either because they're new or their previous target was deleted.
		 * 
		 * Thread Safety:
		 * 
		 *		You must hold <accessLock> in order to use this variable.
		 */
		protected IDObjects.NumberSet linksToResolve;


		/* var: newTopicIDsByEndingSymbol
		 * 
		 * Keeps track of all newly created <Topics>.  The keys are the <EndingSymbols> the topics use, and the values are
		 * <IDObjects.NumberSets> of all the topic IDs associated with that ending symbol.
		 * 
		 * Rationale:
		 * 
		 *		When a new <Topic> is created, it might serve as a better definition for existing links.  We don't want to reresolve
		 *		the links as soon as the topic is created because there may be multiple topics that affect the same links and we'd 
		 *		be wasting effort.  Instead we store which topics are new and resolve the links after parsing is complete.
		 *		
		 *		We can't store the <Topic> objects themselves because we could potentially end up storing a large portion of the
		 *		documentation in memory.  Instead we store the topic IDs and look up the <Topics> again when it's time to resolve links.
		 *		
		 *		We group them by ending symbol instead of having a single NumberSet so that we can reresolve links in batches.  Topics
		 *		that have the same ending symbol will be candidates for the same group of links, so we can query those topics and links
		 *		into memory, reresolve them all at once, and then move on to the next ending symbol.  If we stored a single NumberSet
		 *		of topic IDs we'd have to handle the topics one by one and query for each topic's links separately.
		 *		
		 * Thread Safety:
		 * 
		 *		You must hold <accessLock> in order to use this variable.
		 */
		protected SafeDictionary<Symbols.EndingSymbol, IDObjects.NumberSet> newTopicIDsByEndingSymbol;


		/* var: imageLinksToResolve
		 * 
		 * The IDs of all the image links that need to be resolved, either because they're new or their previous target was deleted.
		 * 
		 * Thread Safety:
		 * 
		 *		You must hold <accessLock> in order to use this variable.
		 */
		protected IDObjects.NumberSet imageLinksToResolve;


		/* var: newImageFileIDsByLCFileName
		 * 
		 * Keeps track of all newly created image file IDs.  The keys are their all-lowercase file names, and the values are
		 * <IDObjects.NumberSets> of all the file IDs associated with that file name.
		 * 
		 * Rationale:
		 * 
		 *		When a new image file is detected, it might serve as a better definition for existing image links.  We don't want to 
		 *		reresolve the links as soon as the file is detected because there may be multiple files that affect the same links and we'd 
		 *		be wasting effort.  Instead we store which files are new and resolve the links after parsing is complete.
		 *		
		 *		We group them by file name instead of having a single NumberSet so that we can reresolve links in batches.  Image files
		 *		that have the same file name will be candidates for the same group of links, so we can query those links into memory, 
		 *		reresolve them all at once, and then move on to the next file name.  If we stored a single NumberSet of image file IDs 
		 *		we'd have to handle the files one by one and query for each file's links separately.
		 *		
		 * Thread Safety:
		 * 
		 *		You must hold <accessLock> in order to use this variable.
		 */
		protected SafeDictionary<string, IDObjects.NumberSet> newImageFileIDsByLCFileName;


		/* var: allLinksAreNew
		 * 
		 * Wheher we can optimize the change list by not tracking new topics in <newTopicIDsByEndingSymbol> or new files to
		 * <newImageFileIDsByLCFileName>.
		 * 
		 * This occurs when we're reparsing everything.  All links will be treated as new and added to <linksToResolve>, therefore we
		 * don't need to track new <Topics> to see if they serve as better definitions for any unchanged links, because there aren't any.
		 * However, once one link is resolved this is no longer the case, so it gets set to false as soon as one is picked.
		 * 
		 * Thread Safety:
		 * 
		 *		You must hold <accessLock> in order to use this variable.
		 */
		protected bool allLinksAreNew;


		/* var: accessLock
		 * An object used for a monitor that prevents more than one thread from accessing any of the variables at a time.
		 */
		protected object accessLock;

		}
	}