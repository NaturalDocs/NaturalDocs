/* 
 * Class: CodeClear.NaturalDocs.Engine.Output.HTML.Target
 * ____________________________________________________________________________
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2021 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using CodeClear.NaturalDocs.Engine.Files;
using CodeClear.NaturalDocs.Engine.Links;
using CodeClear.NaturalDocs.Engine.Topics;


namespace CodeClear.NaturalDocs.Engine.Output.HTML
	{
	public partial class Target
		{

		// Group: IStartupWatcher Functions
		// __________________________________________________________________________


		/* Function: OnStartupIssues
		 * Called whenever new startup issues occur.  Includes both what's new for this call and the total for the engine initialization
		 * thus far.  Multiple new issues can be combined into a single notification, but you will only be notified of each new issue once.
		 */
		public void OnStartupIssues (StartupIssues newIssues, StartupIssues allIssues)
			{
			StartupIssues issuesToAdd = StartupIssues.None;

			if ( (newIssues & ( StartupIssues.NeedToStartFresh |
										StartupIssues.CodeIDsInvalidated |
										StartupIssues.CommentIDsInvalidated |
										StartupIssues.FileIDsInvalidated )) != 0)
				{
				buildState.sourceFilesWithContent.Clear();
				unprocessedChanges.sourceFiles.Clear();

				buildState.classesWithContent.Clear();
				unprocessedChanges.classes.Clear();

				buildState.usedImageFiles.Clear();
				unprocessedChanges.imageFiles.Clear();

				bool inPurgingOperation = false;
				PurgeAllSourceAndImageFolders(ref inPurgingOperation);
				PurgeAllClassFolders(ref inPurgingOperation);
				PurgeAllDatabaseFolders(ref inPurgingOperation);
				PurgeAllMenuFolders(ref inPurgingOperation);
				FinishedPurging(ref inPurgingOperation);

				unprocessedChanges.AddMenu();

				issuesToAdd |= StartupIssues.NeedToReparseAllFiles |
									   StartupIssues.NeedToRebuildAllOutput;
				}

			if ( (newIssues & StartupIssues.NeedToRebuildAllOutput) != 0 ||
				 (issuesToAdd & StartupIssues.NeedToRebuildAllOutput) != 0 )
				{
				unprocessedChanges.AddSourceFiles(buildState.sourceFilesWithContent);
				unprocessedChanges.AddClasses(buildState.classesWithContent);
				unprocessedChanges.AddImageFiles(buildState.usedImageFiles);

				var usedPrefixes = searchIndex.UsedPrefixes();

				foreach (var searchPrefix in usedPrefixes)
					{  unprocessedChanges.AddSearchPrefix(searchPrefix);  }

				unprocessedChanges.AddMainStyleFiles();
				unprocessedChanges.AddMainSearchFiles();
				unprocessedChanges.AddFramePage();
				unprocessedChanges.AddHomePage();
				unprocessedChanges.AddMenu();
				}

			if (issuesToAdd != StartupIssues.None)
				{  EngineInstance.AddStartupIssues(issuesToAdd, dontNotify: this);  }
			}


		public void OnStartPossiblyLongOperation (string operationName)
			{  }
		
		public void OnEndPossiblyLongOperation ()
			{  }



		// Group: CodeDB.IChangeWatcher Functions
		// __________________________________________________________________________
		
		
		public void OnAddTopic (Topic topic, CodeDB.EventAccessor eventAccessor)
			{
			// If this topic defines a class, it's possible it's now the best definition and thus we have to update the class prototypes
			// of all its parents.  We don't have to worry about updating children because that will be taken care of by OnLinkChange
			// since there would be a class parent link pointing to it.

			IDObjects.NumberSet parentClassIDs = null;
			IDObjects.NumberSet parentClassFileIDs = null;

			if (topic.DefinesClass)
				{
				eventAccessor.GetInfoOnClassParents(topic.ClassID, out parentClassIDs, out parentClassFileIDs);
				}

			unprocessedChanges.Lock();
			try
				{
				unprocessedChanges.AddSourceFile(topic.FileID);

				if (topic.ClassID != 0)
					{  unprocessedChanges.AddClass(topic.ClassID);  }

				if (parentClassIDs != null)
					{  unprocessedChanges.AddClasses(parentClassIDs);  }
				if (parentClassFileIDs != null)
					{  unprocessedChanges.AddSourceFiles(parentClassFileIDs);  }
				}
			finally
				{  unprocessedChanges.Unlock();  }
			}

		public void OnUpdateTopic (Topic oldTopic, Topic newTopic, Topic.ChangeFlags changeFlags, CodeDB.EventAccessor eventAccessor)
			{
			// We don't care about line number changes.  They don't affect the output.  We also don't care about context
			// changes.  They might affect links but if they do it will be handled in OnChangeLinkTarget().
			changeFlags &= ~(Topic.ChangeFlags.CommentLineNumber | Topic.ChangeFlags.CodeLineNumber |
									   Topic.ChangeFlags.PrototypeContext | Topic.ChangeFlags.BodyContext);

			if (changeFlags == 0)
				{  return;  }

			unprocessedChanges.Lock();
			try
				{
				unprocessedChanges.AddSourceFile(oldTopic.FileID);

				#if DEBUG
				if (newTopic.FileID != oldTopic.FileID)
					{  throw new Exception("Called OnUpdateTopic() with topics that had different file IDs");  }
				#endif

				if (oldTopic.ClassID != 0)
					{  unprocessedChanges.AddClass(oldTopic.ClassID);  }
				if (newTopic.ClassID != 0)
					{  unprocessedChanges.AddClass(newTopic.ClassID);  }
				}
			finally
				{  unprocessedChanges.Unlock();  }

			// If the summary or prototype changed this means its tooltip changed.  Rebuild any file that contains links 
			// to this topic.
			if ((changeFlags & (Topic.ChangeFlags.Prototype | Topic.ChangeFlags.Summary | 
												Topic.ChangeFlags.LanguageID | Topic.ChangeFlags.CommentTypeID)) != 0)
				{
				IDObjects.NumberSet linkFileIDs, linkClassIDs;
				eventAccessor.GetInfoOnLinksThatResolveToTopicID(oldTopic.TopicID, out linkFileIDs, out linkClassIDs);

				IDObjects.NumberSet oldParentClassIDs = null;
				IDObjects.NumberSet oldParentClassFileIDs = null;
				IDObjects.NumberSet newParentClassIDs = null;
				IDObjects.NumberSet newParentClassFileIDs = null;

				if (oldTopic.DefinesClass)
					{  eventAccessor.GetInfoOnClassParents(oldTopic.ClassID, out oldParentClassIDs, out oldParentClassFileIDs);  }
				if (newTopic.DefinesClass && (newTopic.ClassID != oldTopic.ClassID || oldTopic.DefinesClass == false))
					{  eventAccessor.GetInfoOnClassParents(newTopic.ClassID, out newParentClassIDs, out newParentClassFileIDs);  }

				if (linkFileIDs != null || linkClassIDs != null || 
					oldParentClassIDs != null || oldParentClassFileIDs != null ||
					newParentClassIDs != null || newParentClassFileIDs != null)
					{  
					unprocessedChanges.Lock();
					try
						{  
						if (linkFileIDs != null)
							{  unprocessedChanges.AddSourceFiles(linkFileIDs);  }
						if (linkClassIDs != null)
							{  unprocessedChanges.AddClasses(linkClassIDs);  }
						if (oldParentClassIDs !=  null)
							{  unprocessedChanges.AddClasses(oldParentClassIDs);  }
						if (oldParentClassFileIDs != null)
							{  unprocessedChanges.AddSourceFiles(oldParentClassFileIDs);  }
						if (newParentClassIDs !=  null)
							{  unprocessedChanges.AddClasses(newParentClassIDs);  }
						if (newParentClassFileIDs != null)
							{  unprocessedChanges.AddSourceFiles(newParentClassFileIDs);  }
						}
					finally
						{  unprocessedChanges.Unlock();  }
					}
				}
			}

		public void OnDeleteTopic (Topic topic, IDObjects.NumberSet linksAffected, CodeDB.EventAccessor eventAccessor)
			{
			// We'll wait for OnChangeLinkTarget to handle linksAffected

			IDObjects.NumberSet parentClassIDs = null;
			IDObjects.NumberSet parentClassFileIDs = null;

			if (topic.DefinesClass)
				{
				eventAccessor.GetInfoOnClassParents(topic.ClassID, out parentClassIDs, out parentClassFileIDs);
				}

			unprocessedChanges.Lock();
			try
				{
				unprocessedChanges.AddSourceFile(topic.FileID);

				if (topic.ClassID != 0)
					{  unprocessedChanges.AddClass(topic.ClassID);  }

				if (parentClassIDs != null)
					{  unprocessedChanges.AddClasses(parentClassIDs);  }
				if (parentClassFileIDs != null)
					{  unprocessedChanges.AddSourceFiles(parentClassFileIDs);  }
				}
			finally
				{  unprocessedChanges.Unlock();  }
			}

		public void OnAddLink (Link link, CodeDB.EventAccessor eventAccessor)
			{
			// If a class parent link was added we have to rebuild all source files that define that class as the class prototype
			// may have changed.
			IDObjects.NumberSet filesThatDefineClass = null;

			if (link.Type == LinkType.ClassParent)
				{  
				filesThatDefineClass = eventAccessor.GetFileIDsThatDefineClassID(link.ClassID);  

				// If it's resolved we have to rebuild all source files that define the target as well so its children get updated.
				if (link.TargetClassID != 0)
					{
					IDObjects.NumberSet filesThatDefineTarget = eventAccessor.GetFileIDsThatDefineClassID(link.TargetClassID);

					if (filesThatDefineClass == null)
						{  filesThatDefineClass = filesThatDefineTarget;  }
					else if (filesThatDefineTarget != null)
						{  filesThatDefineClass.Add(filesThatDefineTarget);  }
					}
				}

			unprocessedChanges.Lock();
			try
				{
				// Even if it's not a class parent link we still need to rebuild the source and class files that contain it.  We can't
				// rely on the topic events picking it up because it's possible to change links without changing topics.  How?  By
				// changing a using statement, which causes all the links to change.
				unprocessedChanges.AddSourceFile(link.FileID);

				if (link.ClassID != 0)
					{  unprocessedChanges.AddClass(link.ClassID);  }

				if (link.Type == LinkType.ClassParent && link.TargetClassID != 0)
					{  unprocessedChanges.AddClass(link.TargetClassID);  }

				if (filesThatDefineClass != null)
					{  unprocessedChanges.AddSourceFiles(filesThatDefineClass);  }
				}
			finally
				{  unprocessedChanges.Unlock();  }
			}
		
		public void OnChangeLinkTarget (Link link, int oldTargetTopicID, int oldTargetClassID, CodeDB.EventAccessor eventAccessor)
			{
			IDObjects.NumberSet filesThatDefineClass = null;

			if (link.Type == LinkType.ClassParent)
				{
				filesThatDefineClass = eventAccessor.GetFileIDsThatDefineClassID(link.ClassID);  

				if (link.TargetClassID != 0)
					{
					IDObjects.NumberSet filesThatDefineTarget = eventAccessor.GetFileIDsThatDefineClassID(link.TargetClassID);

					if (filesThatDefineClass == null)
						{  filesThatDefineClass = filesThatDefineTarget;  }
					else if (filesThatDefineTarget != null)
						{  filesThatDefineClass.Add(filesThatDefineTarget);  }
					}

				if (oldTargetClassID != 0)
					{
					IDObjects.NumberSet filesThatDefineOldTarget = eventAccessor.GetFileIDsThatDefineClassID(oldTargetClassID);

					if (filesThatDefineClass == null)
						{  filesThatDefineClass = filesThatDefineOldTarget;  }
					else if (filesThatDefineOldTarget != null)
						{  filesThatDefineClass.Add(filesThatDefineOldTarget);  }
					}
				}

			unprocessedChanges.Lock();
			try
				{
				unprocessedChanges.AddSourceFile(link.FileID);

				if (link.ClassID != 0)
					{  unprocessedChanges.AddClass(link.ClassID);  }

				if (link.Type == LinkType.ClassParent)
					{
					if (link.TargetClassID != 0)
						{  unprocessedChanges.AddClass(link.TargetClassID);  }
					if (oldTargetClassID != 0)
						{  unprocessedChanges.AddClass(oldTargetClassID);  }
					}

				if (filesThatDefineClass != null)
					{  unprocessedChanges.AddSourceFiles(filesThatDefineClass);  }
				}
			finally
				{  unprocessedChanges.Unlock();  }


			// If this is a Natural Docs link, see if it appears in the summary for any topics.  This would mean that it appears in
			// these topics' tooltips, so we have to find any links to these topics and rebuild the files those links appear in.

			// Why do we have to do this if links aren't added to tooltips?  Because how it's resolved can affect it's appearance.
			// It will show up as "link" versus "<link>" if it's resolved or not, and "a at b" versus "a" depending on if it resolves to
			// the topic "a at b" or the topic "b".

			// Why don't we do this for type links?  Because unlike Natural Docs links, type links don't change in appearance
			// based on whether they're resolved or not.  Therefore the logic that we don't have to worry about it because links
			// don't appear in tooltips holds true.

			if (link.Type == LinkType.NaturalDocs)
				{
				IDObjects.NumberSet fileIDs, classIDs;
				eventAccessor.GetInfoOnLinksToTopicsWithNDLinkInSummary(link, out fileIDs, out classIDs);

				if (fileIDs != null || classIDs != null)
					{
					unprocessedChanges.Lock();
					try
						{
						if (fileIDs != null)
							{  unprocessedChanges.AddSourceFiles(fileIDs);  }
						if (classIDs != null)
							{  unprocessedChanges.AddClasses(classIDs);  }
						}
					finally
						{  unprocessedChanges.Unlock();  }
					}
				}
			}
		
		public void OnDeleteLink (Link link, CodeDB.EventAccessor eventAccessor)
			{
			IDObjects.NumberSet filesThatDefineClass = null;

			if (link.Type == LinkType.ClassParent)
				{
				filesThatDefineClass = eventAccessor.GetFileIDsThatDefineClassID(link.ClassID);  

				if (link.TargetClassID != 0)
					{
					IDObjects.NumberSet filesThatDefineTarget = eventAccessor.GetFileIDsThatDefineClassID(link.TargetClassID);

					if (filesThatDefineClass == null)
						{  filesThatDefineClass = filesThatDefineTarget;  }
					else if (filesThatDefineTarget != null)
						{  filesThatDefineClass.Add(filesThatDefineTarget);  }
					}
				}

			unprocessedChanges.Lock();
			try
				{
				unprocessedChanges.AddSourceFile(link.FileID);

				if (link.ClassID != 0)
					{  unprocessedChanges.AddClass(link.ClassID);  }

				if (link.Type == LinkType.ClassParent && link.TargetClassID != 0)
					{  unprocessedChanges.AddClass(link.TargetClassID);  }

				if (filesThatDefineClass != null)
					{  unprocessedChanges.AddSourceFiles(filesThatDefineClass);  }
				}
			finally
				{  unprocessedChanges.Unlock();  }
			}

		public void OnAddImageLink (ImageLink imageLink, CodeDB.EventAccessor eventAccessor)
			{
			// We don't have to force any HTML to be rebuilt here.  This can only happen if the containing topic was 
			// changed so we can rely on the topic code to handle that.

			// However, this could change whether the image file is used or unused, so we have to add it to the list.
			if (imageLink.TargetFileID != 0)
				{  unprocessedChanges.AddImageFileUseCheck(imageLink.TargetFileID);  }
			}

		public void OnChangeImageLinkTarget (ImageLink imageLink, int oldTargetFileID, CodeDB.EventAccessor eventAccessor)
			{
			// Here we have to rebuild the HTML containing the link because this could happen without the containing
			// topic changing, such as if an image file was deleted or a new one served as a better target.

			unprocessedChanges.AddSourceFile(imageLink.FileID);

			if (imageLink.ClassID != 0)
				{  unprocessedChanges.AddClass(imageLink.ClassID);  }


			// We also have to check both image files because they could have changed between used and unused.

			if (imageLink.TargetFileID != 0)
				{  unprocessedChanges.AddImageFileUseCheck(imageLink.TargetFileID);  }
			if (oldTargetFileID != 0)
				{  unprocessedChanges.AddImageFileUseCheck(oldTargetFileID);  }


			// We also have to see if it appears in the summary for any topics.  This would mean that it appears in these topics'
			// tooltips, so we have to find any links to these topics and rebuild the files those links appear in.

			// Why do we have to do this if links aren't added to tooltips?  Because how it's resolved can affect it's appearance.
			// It will show up as "(see diagram)" versus "(see images/diagram.jpg)" if it's resolved or not.

			IDObjects.NumberSet fileIDs, classIDs;
			eventAccessor.GetInfoOnLinksToTopicsWithImageLinkInSummary(imageLink, out fileIDs, out classIDs);

			if (fileIDs != null)
				{  unprocessedChanges.AddSourceFiles(fileIDs);  }
			if (classIDs != null)
				{  unprocessedChanges.AddClasses(classIDs);  }
			}

		public void OnDeleteImageLink (ImageLink imageLink, CodeDB.EventAccessor eventAccessor)
			{
			// We don't have to force any HTML to be rebuilt here.  This can only happen if the containing topic was 
			// changed so we can rely on the topic code to handle that.

			// However, this could chang e whether the image file is used or unused, so we have to add it to the list.
			if (imageLink.TargetFileID != 0)
				{  unprocessedChanges.AddImageFileUseCheck(imageLink.TargetFileID);  }
			}



		// Group: Files.IChangeWatcher Functions
		// __________________________________________________________________________


		public void OnAddFile (File file)
			{
			if (file.Type == FileType.Style)
				{  
				// Add it to the build list.  The build function will check if it's part of a style we're using.
				unprocessedChanges.AddStyleFile(file.ID);  
				}

			else if (file.Type == FileType.Image)
				{
				// Add it to the build list.  The build function will check if it's used or unused.
				unprocessedChanges.AddImageFile(file.ID);
				}

			// We don't care about source files here.  They'll be handled by functions like OnAddTopic().
			}

		public void OnFileChanged (File file)
			{
			if (file.Type == FileType.Style)
				{  
				// Add it to the build list.  The build function will check if it's part of a style we're using.
				unprocessedChanges.AddStyleFile(file.ID);  
				}

			else if (file.Type == FileType.Image)
				{
				// Add it to the build list.  The build function will check if it's used or unused.
				unprocessedChanges.AddImageFile(file.ID);

				// Also rebuild the HTML files containing links to it in case the dimensions changed.

				// Creating and destroying an accessor on every event might be a heavy operation.  This event shouldn't
				// happen too often though so it's probably not worth caching one, plus it would be difficult to know when
				// to release it.
				CodeDB.Accessor accessor = EngineInstance.CodeDB.GetAccessor();

				try
					{
					accessor.GetReadOnlyLock();

					List<ImageLink> imageLinks = accessor.GetImageLinksByTarget(file.ID, Delegates.NeverCancel, 
																														  CodeDB.Accessor.GetImageLinkFlags.DontLookupClasses);

					foreach (var imageLink in imageLinks)
						{
						unprocessedChanges.AddSourceFile(imageLink.FileID);
						unprocessedChanges.AddClass(imageLink.ClassID);
						}

					accessor.ReleaseLock();
					}
				finally
					{  accessor.Dispose();  }
				}

			// We don't care about source files here.  They'll be handled by functions like OnUpdateTopic().
			}

		public void OnDeleteFile (File file)
			{
			if (file.Type == FileType.Style)
				{  
				// Add it to the build list.  The build function will check if it's part of a style we're using.
				unprocessedChanges.AddStyleFile(file.ID);  
				}

			else if (file.Type == FileType.Image)
				{
				// Add it to the build list.  The build function will check if it's used or unused.
				unprocessedChanges.AddImageFile(file.ID);
				}

			// We don't care about source files here.  They'll be handled by functions like OnDeleteTopic().
			}



		// Group: SearchIndex.IChangeWatcher Functions
		// __________________________________________________________________________


		public void OnAddPrefix (string prefix, CodeDB.EventAccessor accessor)
			{
			unprocessedChanges.AddMainSearchFiles();
			unprocessedChanges.AddSearchPrefix(prefix);  
			}

		public void OnUpdatePrefix (string prefix, CodeDB.EventAccessor accessor)
			{
			unprocessedChanges.AddSearchPrefix(prefix);
			}

		public void OnDeletePrefix (string prefix, CodeDB.EventAccessor accessor)
			{
			unprocessedChanges.AddMainSearchFiles();
			unprocessedChanges.AddSearchPrefix(prefix);  
			}

		}
	}

