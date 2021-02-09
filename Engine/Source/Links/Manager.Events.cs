/* 
 * Class: CodeClear.NaturalDocs.Engine.Links.Manager
 * ____________________________________________________________________________
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2021 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using CodeClear.NaturalDocs.Engine.CodeDB;
using CodeClear.NaturalDocs.Engine.Files;
using CodeClear.NaturalDocs.Engine.Topics;


namespace CodeClear.NaturalDocs.Engine.Links
	{
	public partial class Manager
		{

		// Group: CodeDB.IChangeWatcher Functions
		// __________________________________________________________________________


		public void OnAddTopic (Topic topic, EventAccessor eventAccessor)
			{
			unprocessedChanges.AddTopic(topic);
			}
		

		public void OnUpdateTopic (Topic oldTopic, Topic newTopic, Topic.ChangeFlags changeFlags, EventAccessor eventAccessor)
			{
			}
		

		public void OnDeleteTopic (Topic topic, IDObjects.NumberSet linksAffected, EventAccessor eventAccessor)
			{
			unprocessedChanges.DeleteTopic(topic, linksAffected);
			}

		
		public void OnAddLink (Link link, EventAccessor eventAccessor)
			{
			unprocessedChanges.AddLink(link);
			}

		
		public void OnChangeLinkTarget (Link link, int oldTargetTopicID, int oldTargetClassID, EventAccessor eventAccessor)
			{
			// We're going to be the one causing this event, not responding to it.  No other code should be changing link definitions.
			}

		
		public void OnDeleteLink (Link link, EventAccessor eventAccessor)
			{
			unprocessedChanges.DeleteLink(link);
			}


		public void OnAddImageLink (ImageLink imageLink, CodeDB.EventAccessor eventAccessor)
			{
			unprocessedChanges.AddImageLink(imageLink);
			}


		public void OnChangeImageLinkTarget (ImageLink imageLink, int oldTargetFileID, CodeDB.EventAccessor eventAccessor)
			{
			// We're going to be the one causing this event, not responding to it.  No other code should be changing link definitions.
			}


		public void OnDeleteImageLink (ImageLink imageLink, CodeDB.EventAccessor eventAccessor)
			{
			unprocessedChanges.DeleteImageLink(imageLink);
			}



		// Group: Files.IChangeWatcher Functions
		// __________________________________________________________________________


		public void OnAddFile (File file)
			{
			if (file.Type == FileType.Image)
				{  unprocessedChanges.AddImageFile((ImageFile)file);  }
			}


		public void OnFileChanged (File file)
			{
			// Not relevant to link resolution.
			}


		public void OnDeleteFile (File file)
			{
			if (file.Type == FileType.Image)
				{
				// We need to get the image links affected by this

				CodeDB.Accessor accessor = EngineInstance.CodeDB.GetAccessor();

				try
					{
					accessor.GetReadOnlyLock();
					IDObjects.NumberSet linksAffected = accessor.GetImageLinkIDsByTarget(file.ID, Delegates.NeverCancel);
					accessor.ReleaseLock();

					unprocessedChanges.DeleteImageFile((ImageFile)file, linksAffected);
					}
				finally
					{  
					if (accessor.HasLock)
						{  accessor.ReleaseLock();  }

					accessor.Dispose();
					}
				}
			}

		}
	}
