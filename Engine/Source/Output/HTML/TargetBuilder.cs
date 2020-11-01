/* 
 * Class: CodeClear.NaturalDocs.Engine.Output.HTML.TargetBuilder
 * ____________________________________________________________________________
 * 
 * A process which handles building output files for a single <HTML.Target>.
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
using System.Text;
using CodeClear.NaturalDocs.Engine.Collections;
using CodeClear.NaturalDocs.Engine.Files;
using CodeClear.NaturalDocs.Engine.IDObjects;
using CodeClear.NaturalDocs.Engine.Symbols;


namespace CodeClear.NaturalDocs.Engine.Output.HTML
	{
	public class TargetBuilder : Output.TargetBuilder
		{
		
		// Group: Functions
		// __________________________________________________________________________
		

		/* Function: TargetBuilder
		 */
		public TargetBuilder (HTML.Target target) : base (target)
			{
			this.workInProgress = 0;
			this.accessLock = new object();
			}
			
		override protected void Dispose (bool strictRulesApply)
			{
			if (!strictRulesApply)
				{
				if (workInProgress != 0)
					{  throw new Exception("HTML.Builder shut down while work was still in progress.");  }
				}
			}


		/* Function: WorkOnUpdatingOutput
		 * 
		 * Works on the task of going through all the detected changes and updating the generated HTML and supporting files.
		 * This is a parallelizable task, so multiple threads can call this function and they will divide up the work until it's done.
		 * Pass a <CancelDelegate> if you'd like to be able to interrupt this task, or <Delegates.NeverCancel> if not.
		 * 
		 * Note that building the output is a two-stage process, so after this task is fully complete you must also call 
		 * <WorkOnFinalizingOutput()> to finish it.
		 * 
		 * This function returns if it's cancelled or there is no more work to be done.  If there is only one thread working on this 
		 * then the task is complete, but if there are multiple threads the task isn't complete until they all have returned.  This
		 * one may have returned because there was no more work for it to do but other threads could still be working.
		 */
		override public void WorkOnUpdatingOutput (CancelDelegate cancelDelegate)
			{
			if (cancelDelegate())
				{  return;  }

			CodeDB.Accessor accessor = null;
			
			try
				{
				for (;;)
					{

					// Build frame page

					if (Target.UnprocessedChanges.PickFramePage())
						{
						lock (accessLock)
							{  workInProgress += FramePageCost;  }

						BuildFramePage(cancelDelegate);

						lock (accessLock)
							{  workInProgress -= FramePageCost;  }

						if (cancelDelegate())
							{
							Target.UnprocessedChanges.AddFramePage();
							break;
							}
						else
							{  continue;  }
						}


					// Build main style files
						
					if (Target.UnprocessedChanges.PickMainStyleFiles())
						{
						lock (accessLock)
							{  workInProgress += MainStyleFilesCost;  }

						BuildMainStyleFiles(cancelDelegate);

						lock (accessLock)
							{  workInProgress -= MainStyleFilesCost;  }

						if (cancelDelegate())
							{
							Target.UnprocessedChanges.AddMainStyleFiles();
							break;
							}
						else
							{  continue;  }
						}


					// Build style files
						
					int styleFileToRebuild = Target.UnprocessedChanges.PickStyleFile();

					if (styleFileToRebuild != 0)
						{
						lock (accessLock)
							{  workInProgress += StyleFileCost;  }
						
						BuildStyleFile(styleFileToRebuild, cancelDelegate);
						
						lock (accessLock)
							{  workInProgress -= StyleFileCost;  }

						if (cancelDelegate())
							{
							Target.UnprocessedChanges.AddStyleFile(styleFileToRebuild);
							break;
							}		
						else
							{  continue;  }
						}
						

					// Build source files
					
					int sourceFileToRebuild = Target.UnprocessedChanges.PickSourceFile();

					if (sourceFileToRebuild != 0)
						{
						lock (accessLock)
							{  workInProgress += SourceFileCost;  }
						
						if (accessor == null)
							{  accessor = EngineInstance.CodeDB.GetAccessor();  }
							
						BuildSourceFile(sourceFileToRebuild, accessor, cancelDelegate);
						
						lock (accessLock)
							{  workInProgress -= SourceFileCost;  }

						if (cancelDelegate())
							{
							Target.UnprocessedChanges.AddSourceFile(sourceFileToRebuild);
							break;
							}
						else
							{  continue;  }
						}
						

					// Build class files
						
					int classToRebuild = Target.UnprocessedChanges.PickClass();

					if (classToRebuild != 0)
						{
						lock (accessLock)
							{  workInProgress += ClassCost;  }
						
						if (accessor == null)
							{  accessor = EngineInstance.CodeDB.GetAccessor();  }
							
						BuildClassFile(classToRebuild, accessor, cancelDelegate);
						
						lock (accessLock)
							{  workInProgress -= ClassCost;  }

						if (cancelDelegate())
							{
							Target.UnprocessedChanges.AddClass(classToRebuild);
							break;
							}		
						else
							{  continue;  }
						}

						
					// Build image files
						
					int imageToRebuild = Target.UnprocessedChanges.PickImageFile();

					if (imageToRebuild != 0)
						{
						lock (accessLock)
							{  workInProgress += ImageFileCost;  }
						
						if (accessor == null)
							{  accessor = EngineInstance.CodeDB.GetAccessor();  }
							
						BuildImageFile(imageToRebuild, accessor, cancelDelegate);
						
						lock (accessLock)
							{  workInProgress -= ImageFileCost;  }

						if (cancelDelegate())
							{
							Target.UnprocessedChanges.AddImageFile(imageToRebuild);
							break;
							}		
						else
							{  continue;  }
						}
						

					// Build image files that haven't changed but may or may not be used anymore
						
					int imageToCheck = Target.UnprocessedChanges.PickUnchangedImageFileUseCheck();

					if (imageToCheck != 0)
						{
						lock (accessLock)
							{  workInProgress += UnchangedImageFileUseCheckCost;  }
						
						if (accessor == null)
							{  accessor = EngineInstance.CodeDB.GetAccessor();  }
							
						// Same as building a regular image, only we don't have to do anything if the output file already exists.  It didn't change
						// so the existing output file should be fine.
						BuildImageFile(imageToCheck, accessor, cancelDelegate, overwrite: false);
						
						lock (accessLock)
							{  workInProgress -= UnchangedImageFileUseCheckCost;  }

						if (cancelDelegate())
							{
							Target.UnprocessedChanges.AddImageFileUseCheck(imageToCheck);
							break;
							}		
						else
							{  continue;  }
						}
						
					else
						{  break;  }
					}
				}
			finally
				{
				if (accessor != null)
					{  accessor.Dispose();  }
				}
			}
			

		/* Function: WorkOnFinalizingOutput
		 * 
		 * Works on the task of finalizing the output, which is any task that requires all files to be successfully processed by
		 * <WorkOnUpdatingOutput()> before it can run.  You must wait for all threads to return from <WorkOnUpdatingOutput()>
		 * before calling this function.  This is a parallelizable task, so multiple threads can call this function and the work will be
		 * divided up between them.  Pass a <CancelDelegate> if you'd like to be able to interrupt this task, or 
		 * <Delegates.NeverCancel> if not.
		 * 
		 * This function returns if it's cancelled or there is no more work to be done.  If there is only one thread working on this 
		 * then the task is complete, but if there are multiple threads the task isn't complete until they all have returned.  This
		 * one may have returned because there was no more work for it to do but other threads could still be working.
		 */
		override public void WorkOnFinalizingOutput (CancelDelegate cancelDelegate)
			{
			if (cancelDelegate())
				{  return;  }

			CodeDB.Accessor accessor = null;
			
			try
				{
				for (;;)
					{
					
					// Delete empty folders

					List<Path> possiblyEmptyFolders = Target.UnprocessedChanges.PickPossiblyEmptyFolders();

					if (possiblyEmptyFolders != null)
						{
						// This task is not parallelizable so the entire list gets claimed by one thread.  Theoretically it should be, but
						// in practice when two or more threads try to delete the same folder at the same time they both fail.  This
						// could happen if both the folder and it's parent folder are on the deletion list, so one thread gets it from the 
						// list while the other thread gets it by walking up the child's tree.

						lock (accessLock)
							{  workInProgress += PossiblyEmptyFolderCost * possiblyEmptyFolders.Count;  }

						int folderIndex = 0;

						while (folderIndex < possiblyEmptyFolders.Count)
							{
							DeleteEmptyFolders(possiblyEmptyFolders[folderIndex]);
							folderIndex++;

							lock (accessLock)
								{  workInProgress -= PossiblyEmptyFolderCost;  }

							if (cancelDelegate())
								{  break;  }
							}

						// If folderIndex isn't at the end that means the cancel delegate was triggered and we have to add the remaining
						// folders back to the unprocessed changes.
						if (folderIndex < possiblyEmptyFolders.Count)
							{
							lock (accessLock)
								{  workInProgress -= PossiblyEmptyFolderCost * (possiblyEmptyFolders.Count - folderIndex);  }

							do
								{
								Target.UnprocessedChanges.AddPossiblyEmptyFolder(possiblyEmptyFolders[folderIndex]);
								folderIndex++;
								}
							while (folderIndex < possiblyEmptyFolders.Count);

							break;
							}
						else
							{  continue;  }
						}


					// Build the menu

					if (Target.UnprocessedChanges.PickMenu())
						{
						lock (accessLock)
							{  workInProgress += MenuCost;  }

						if (accessor == null)
							{  accessor = EngineInstance.CodeDB.GetAccessor();  }

						BuildMenuFiles(accessor, cancelDelegate);

						lock (accessLock)
							{  workInProgress -= MenuCost;  }

						if (cancelDelegate())
							{
							Target.UnprocessedChanges.AddMenu();
							break;
							}
						else
							{  continue;  }
						}


					// Build the main search files

					if (Target.UnprocessedChanges.PickMainSearchFiles())
						{
						lock (accessLock)
							{  workInProgress += MainSearchFilesCost;  }

						if (accessor == null)
							{  accessor = EngineInstance.CodeDB.GetAccessor();  }

						BuildMainSearchFiles(accessor, cancelDelegate);

						lock (accessLock)
							{  workInProgress -= MainSearchFilesCost;  }

						if (cancelDelegate())
							{
							Target.UnprocessedChanges.AddMainSearchFiles();
							break;
							}
						else
							{  continue;  }
						}


					// Build the search prefix files

					string prefixToRebuild = Target.UnprocessedChanges.PickSearchPrefix();

					if (prefixToRebuild != null)
						{
						lock (accessLock)
							{  workInProgress += SearchPrefixCost;  }

						if (accessor == null)
							{  accessor = EngineInstance.CodeDB.GetAccessor();  }

						BuildSearchPrefixFile(prefixToRebuild, accessor, cancelDelegate);

						lock (accessLock)
							{  workInProgress -= SearchPrefixCost;  }

						if (cancelDelegate())
							{
							Target.UnprocessedChanges.AddSearchPrefix(prefixToRebuild);
							break;
							}
						else
							{  continue;  }
						}

					else
						{  break;  }
					}
				}
			finally
				{
				if (accessor != null)
					{  accessor.Dispose();  }
				}
			}
		 
		
		/* Function: GetStatus
		 * Returns numeric values representing the total changes being processed and those yet to be processed in 
		 * <Target.UnprocessedChanges>.  It is the sum of the respective tasks weighted by the <Cost Constants> which 
		 * estimate how hard each one is to perform.  It also encompasses the tasks performed by both 
		 * <WorkOnUpdatingOutput()> and <WorkOnFinalizingOutput()> so it will not reach zero until both stages are 
		 * completed.  The numbers are meaningless other than to track progress as they work their way towards zero.
		 */
		override public void GetStatus (out long workInProgress, out long workRemaining)
			{
			lock (accessLock)
				{  
				workInProgress = this.workInProgress;
				Target.UnprocessedChanges.GetStatus(out workRemaining);
				}
			}
			
		

		// Group: Support Functions
		// __________________________________________________________________________


		/* Function: BuildFramePage
		 * Builds index.html, which provides the documentation frame.
		 */
		protected void BuildFramePage (CancelDelegate cancelDelegate)
			{

			// Page and header titles

			string rawPageTitle;
			string headerTitleHTML;
			string headerSubtitleHTML;

			if (Target.ProjectInfo.Title == null)
				{
				rawPageTitle = Locale.Get("NaturalDocs.Engine", "HTML.DefaultPageTitle");
				headerTitleHTML = Locale.Get("NaturalDocs.Engine", "HTML.DefaultHeaderTitle").ToHTML();
				headerSubtitleHTML = null;
				}
			else
				{
				rawPageTitle = Locale.Get("NaturalDocs.Engine", "HTML.PageTitle(projectTitle)", Target.ProjectInfo.Title);
				headerTitleHTML = Locale.Get("NaturalDocs.Engine", "HTML.HeaderTitle(projectTitle)", Target.ProjectInfo.Title).ToHTML();

				if (Target.ProjectInfo.Subtitle == null)
					{  headerSubtitleHTML = null;  }
				else
					{
					headerSubtitleHTML = Locale.Get("NaturalDocs.Engine", "HTML.HeaderSubtitle(projectSubtitle)", Target.ProjectInfo.Subtitle).ToHTML();
					}
				}


			// Footer

			string timestampHTML = Target.ProjectInfo.MakeTimestamp();
			string copyrightHTML = Target.ProjectInfo.Copyright;

			if (timestampHTML != null)
				{  timestampHTML = timestampHTML.ToHTML();  }
			if (copyrightHTML != null)
				{  copyrightHTML = copyrightHTML.ToHTML();  }


			// index.html, the main frame page

			StringBuilder content = new StringBuilder();

			content.Append(

				"<div id=\"NDHeader\">" +

					"<div id=\"HTitle\">" +
						"<a href=\"#\">" +
							headerTitleHTML +
						"</a>" +
					"</div>");

					if (headerSubtitleHTML != null)
						{  
						content.Append(
							"<div id=\"HSubtitle\">" +
								"<a href=\"#\">" +
									headerSubtitleHTML +
								"</a>" +
							"</div>");  
						}

					content.Append(
					"<input id=\"NDSearchField\" type=\"text\" autocomplete=\"off\" />"+

				"</div>" +

				"<script type=\"text/javascript\">" +
					// The backslash on /div is necessary to validate even though it's logically not necessary.  See here:
					// http://www.htmlhelp.com/tools/validator/problems.html#script
					"document.write(\"<div id=\\\"NDLoadingNotice\\\"><\\/div>\");" +
				"</script>" +
				"<noscript>" +
					"<div id=\"NDJavaScriptRequiredNotice\">" +
						Locale.Get("NaturalDocs.Engine", "HTML.JavaScriptRequiredNotice").ToHTML() +
					"</div>" +
				"</noscript>" +

				"<div id=\"NDMenu\"></div>" +
				"<div id=\"NDMenuSizer\"></div>" +

				"<div id=\"NDSummary\"></div>" +
				"<div id=\"NDSummarySizer\"></div>" +

				"<div id=\"NDContent\">" +
					"<iframe id=\"CFrame\" frameborder=\"0\"></iframe>" +
				"</div>" +

				"<div id=\"NDFooter\">" +

					"<div id=\"FGeneratedBy\">" +
						// Deliberately hard coded (as opposed to using Locale) so it stays consistent and we can find users of any
						// language by putting it into a search engine.  If they don't want it in their docs they can set #FGeneratedBy 
						// to display: none.
						"<a href=\"http://www.naturaldocs.org\" target=\"_blank\">Generated by Natural Docs</a>" +
					"</div>");

					if (timestampHTML != null)
						{
						content.Append(
							"<div id=\"FTimestamp\">" +
								timestampHTML +
							"</div>");
						}

					if (copyrightHTML != null)
						{
						content.Append(
							"<div id=\"FCopyright\">" +
								copyrightHTML +
							"</div>");
						}

			Context context = new Context(Target);
			Components.Page pageBuilder = new Components.Page(context);

			pageBuilder.Build(Target.OutputFolder + "/index.html", rawPageTitle, content.ToString(), PageType.Frame);


			// other/home.html, the default welcome page

			content.Remove(0, content.Length);

			string titleHTML, subtitleHTML;

			if (Target.ProjectInfo.Title != null)
				{
				titleHTML = headerTitleHTML;

				if (Target.ProjectInfo.Subtitle != null)
					{  subtitleHTML = headerSubtitleHTML;  }
				else
					{  subtitleHTML = Locale.Get("NaturalDocs.Engine", "HTML.DefaultHomeSubtitleIfTitleExists").ToHTML();  }
				}
			else
				{
				titleHTML = Locale.Get("NaturalDocs.Engine", "HTML.DefaultHomeTitle").ToHTML();
				subtitleHTML = null;
				}

			content.Append(
				"\r\n\r\n" +
				"<div class=\"HFrame\">" +
					"<div class=\"HContent\">" + 
						"<div class=\"HTitle\">" +
							titleHTML + 
						"</div>");

						if (subtitleHTML != null)
							{
							content.Append(
								"<div class=\"HSubtitle\">" +
									subtitleHTML + 
								"</div>");
							}


					content.Append(
						"\r\n\r\n" +
						"<div class=\"HFooter\">" +

						"<div class=\"HGeneratedBy\">" +
							"<a href=\"http://www.naturaldocs.org\" target=\"_blank\">Generated by Natural Docs</a>" +
						"</div>");

						if (timestampHTML != null)
							{
							content.Append(
								"<div class=\"HTimestamp\">" +
									timestampHTML +
								"</div>");
							}

						if (copyrightHTML != null)
							{
							content.Append(
								"<div class=\"HCopyright\">" +
									copyrightHTML +
								"</div>");
							}

					content.Append(
					"</div>" + 
				"</div>" + 
			"</div>");

			pageBuilder.Build(Target.OutputFolder + "/other/home.html", rawPageTitle, content.ToString(), PageType.Home);
			}


		/* Function: BuildSourceFile
		 * Builds an output file based on a source file.  The accessor should NOT hold a lock on the database.  This will also
		 * build the metadata files.
		 */
		protected void BuildSourceFile (int fileID, CodeDB.Accessor accessor, CancelDelegate cancelDelegate)
			{
			#if DEBUG
			if (accessor.LockHeld != CodeDB.Accessor.LockType.None)
				{  throw new Exception ("Shouldn't call BuildSourceFile() when the accessor already holds a database lock.");  }
			#endif

			var file = EngineInstance.Files.FromID(fileID);

			// Quit early if the file source was deleted since that will cause a lot of problems like not being able to build paths.
			// The output files associated with it will have been purged already so we don't need to worry about them.
			if (file.Deleted && EngineInstance.Files.FileSourceOf(file) == null)
				{  
				if (Target.BuildState.RemoveSourceFileWithContent(fileID) == true)
					{  Target.UnprocessedChanges.AddMenu();  }

				return;
				}

			var context = new Context(Target, fileID);
			var topicPage = new Components.TopicPage(context);

			bool hasTopics = topicPage.BuildDataFiles(context, accessor, cancelDelegate);

			if (cancelDelegate())
				{  return;  }

			if (hasTopics)
				{
				if (Target.BuildState.AddSourceFileWithContent(fileID) == true)
					{  Target.UnprocessedChanges.AddMenu();  }
				}
			else
				{
				DeleteOutputFileIfExists(context.OutputFile);
				DeleteOutputFileIfExists(context.ToolTipsFile);
				DeleteOutputFileIfExists(context.SummaryFile);
				DeleteOutputFileIfExists(context.SummaryToolTipsFile);

				if (Target.BuildState.RemoveSourceFileWithContent(fileID) == true)
					{  Target.UnprocessedChanges.AddMenu();  }
				}
			}


		/* Function: BuildClassFile
		 * Builds an output file based on a class.  The accessor should NOT hold a lock on the database.  This will also
		 * build the metadata files.
		 */
		protected void BuildClassFile (int classID, CodeDB.Accessor accessor, CancelDelegate cancelDelegate)
			{
			#if DEBUG
			if (accessor.LockHeld != CodeDB.Accessor.LockType.None)
				{  throw new Exception ("Shouldn't call BuildClassFile() when the accessor already holds a database lock.");  }
			#endif

			Context context;
			bool hasTopics = false;

			accessor.GetReadOnlyLock();

			try
				{  
				ClassString classString = accessor.GetClassByID(classID);

				context = new Context(Target, classID, classString);
				var topicPage = new Components.TopicPage(context);

				hasTopics = topicPage.BuildDataFiles(context, accessor, cancelDelegate, releaseExistingLocks: true);
				}
			finally
				{  
				if (accessor.LockHeld != CodeDB.Accessor.LockType.None)
					{  accessor.ReleaseLock();  }
				}

			if (cancelDelegate())
				{  return;  }


			if (hasTopics)
				{
				if (Target.BuildState.AddClassWithContent(classID) == true)
					{  Target.UnprocessedChanges.AddMenu();  }
				}
			else
				{
				DeleteOutputFileIfExists(context.OutputFile);
				DeleteOutputFileIfExists(context.ToolTipsFile);
				DeleteOutputFileIfExists(context.SummaryFile);
				DeleteOutputFileIfExists(context.SummaryToolTipsFile);

				if (Target.BuildState.RemoveClassWithContent(classID) == true)
					{  Target.UnprocessedChanges.AddMenu();  }
				}
			}


		/* Function: BuildImageFile
		 * 
		 * Copies an image to the output folder if it is used, or deletes it if it is not.  The accessor should NOT hold a lock on the 
		 * database.
		 * 
		 * Overwrite is set to true by default, which means the file will always be copied.  If set to false the image file will only be
		 * copied if the output file doesn't already exist.  This does NOT check if the files are different, just whether a file already
		 * exists.
		 */
		protected void BuildImageFile (int imageFileID, CodeDB.Accessor accessor, CancelDelegate cancelDelegate, bool overwrite = true)
			{
			#if DEBUG
			if (accessor.LockHeld != CodeDB.Accessor.LockType.None)
				{  throw new Exception ("Shouldn't call BuildImageFile() when the accessor already holds a database lock.");  }
			#endif

			var imageFile = EngineInstance.Files.FromID(imageFileID);
			var fileSource = EngineInstance.Files.FileSourceOf(imageFile);

			// Quit early if the file source was deleted since that will cause a lot of problems like not being able to build paths.
			// The output files associated with it will have been purged already so we don't need to worry about them.
			if (imageFile.Deleted && fileSource == null)
				{  
				Target.BuildState.RemoveUsedImageFile(imageFile.ID);
				return;
				}

			var relativePath = fileSource.MakeRelative(imageFile.Name);

			Path outputPath = Paths.Image.OutputFile(Target.OutputFolder, fileSource.Number, fileSource.Type, relativePath);

			if (imageFile.Deleted)
				{
				DeleteOutputFileIfExists(outputPath);
				Target.BuildState.RemoveUsedImageFile(imageFile.ID);
				}
			else
				{
				bool imageFileIsUsed;
				accessor.GetReadOnlyLock();

				try
					{
					imageFileIsUsed = accessor.IsTargetOfImageLink(imageFileID);
					}
				finally
					{
					if (accessor.LockHeld != CodeDB.Accessor.LockType.None)
						{  accessor.ReleaseLock();  }
					}

				if (!imageFileIsUsed)
					{  
					DeleteOutputFileIfExists(outputPath);
					Target.BuildState.RemoveUsedImageFile(imageFile.ID);
					}
				else
					{
					// Creates all subdirectories needed.  Does nothing if it already exists.
					System.IO.Directory.CreateDirectory(outputPath.ParentFolder);

					if (overwrite || !System.IO.File.Exists(outputPath))
						{  System.IO.File.Copy(imageFile.FileName, outputPath, true);  }

					Target.BuildState.AddUsedImageFile(imageFile.ID);
					}
				}
			}


		/* Function: BuildMenuFiles
		 */
		protected void BuildMenuFiles (CodeDB.Accessor accessor, CancelDelegate cancelDelegate)
			{
			Context context = new Context(Target);
			Components.Menu menu = new Components.Menu(context);

			Target.BuildState.Lock();
			try
				{

				// Build the file menu

				foreach (int fileID in Target.BuildState.sourceFilesWithContent)
					{
					menu.AddFile(EngineInstance.Files.FromID(fileID));

					if (cancelDelegate())
						{  return;  }
					}


				// Build the class and database menus

				List<ClassString> classStrings = null;

				accessor.GetReadOnlyLock();
				try
					{  classStrings = accessor.GetClassesByID(Target.BuildState.classesWithContent, cancelDelegate);  }
				finally
					{  accessor.ReleaseLock();  }

				foreach (var classString in classStrings)
					{
					menu.AddClass(classString);

					if (cancelDelegate())
						{  return;  }
					}

				}
			finally
				{  Target.BuildState.Unlock();  }


			// Condense, sort, and build

			menu.Condense();
			
			if (cancelDelegate())
				{  return;  }

			menu.Sort();

			if (cancelDelegate())
				{  return;  }

			Components.JSONMenu jsonMenu = new Components.JSONMenu(context);
			jsonMenu.ConvertToJSON(menu);

			if (cancelDelegate())
				{  return;  }

			// Don't check cancelDelegate after this because we'll be committed to replacing BuildState.UsedMenuDataFiles and
			// cleaning up the difference.  Otherwise things will be in an inconsistent state.

			NumberSetTable<Hierarchy> newMenuDataFiles = jsonMenu.BuildDataFiles();


			// Clear out any old menu files that are no longer in use.

			Target.BuildState.Lock();
			try
				{
				foreach (var hierarchyMenuDataFiles in Target.BuildState.usedMenuDataFiles)
					{

					// Compare the old and new numbers to determine which ones to remove

					var hierarchy = hierarchyMenuDataFiles.Key;
					var oldFileNumbers = hierarchyMenuDataFiles.Value;

					NumberSet newFileNumbers = newMenuDataFiles[hierarchy];
					NumberSet removedFileNumbers;

					if (newFileNumbers != null)
						{  
						removedFileNumbers = oldFileNumbers.Duplicate();
						removedFileNumbers.Remove(newFileNumbers);  
						}
					else
						{  removedFileNumbers = oldFileNumbers;  }


					// Remove the data files associated with them

					foreach (int removedFileNumber in removedFileNumbers)
						{
						DeleteOutputFileIfExists(Paths.Menu.OutputFile(Target.OutputFolder, hierarchy, removedFileNumber));
						}
					}

				Target.BuildState.usedMenuDataFiles = newMenuDataFiles;
				}

			finally
				{  Target.BuildState.Unlock();  }
			}


		/* Function: BuildMainSearchFiles
		 */
		protected void BuildMainSearchFiles (CodeDB.Accessor accessor, CancelDelegate cancelDelegate)
			{
			Context context = new Context(Target);
			Components.JSONSearchIndex searchData = new Components.JSONSearchIndex(context);
			searchData.BuildIndexDataFile();
			}


		/* Function: BuildSearchPrefixFile
		 */
		protected void BuildSearchPrefixFile (string prefix, CodeDB.Accessor accessor, CancelDelegate cancelDelegate)
			{
			Context context = new Context(Target);
			Components.JSONSearchIndex searchData = new Components.JSONSearchIndex(context);
			searchData.BuildPrefixDataFile(prefix, accessor, cancelDelegate);
			}


		/* Function: BuildMainStyleFiles
		 * Builds main.css and main.js.
		 */
		protected void BuildMainStyleFiles (CancelDelegate cancelDelegate)
			{

			// main.css

			StringBuilder cssOutput = new StringBuilder();

			foreach (var style in Target.StylesWithInheritance)
				{
				if (style.Links != null)
					{
					foreach (var link in style.Links)
						{
						// We don't care about filters for CSS files.
						if (link.File.Extension.ToLower() == "css")
							{
							Path relativeLinkPath = style.MakeRelative(link.File);
							Path outputPath = Paths.Style.OutputFile(Target.OutputFolder, style.Name, relativeLinkPath);
							Path relativeOutputPath = outputPath.MakeRelativeTo(Paths.Style.OutputFolder(Target.OutputFolder));
							cssOutput.Append("@import URL(\"" + relativeOutputPath.ToURL() + "\");");
							}
						}
					}
				}

			// There's nothing to condense so just write it directly to a file.
			HTML.Component.WriteTextFile(Paths.Style.OutputFolder(Target.OutputFolder) + "/main.css", cssOutput.ToString());


			// main.js

			StringBuilder[] jsLinks = new StringBuilder[ PageTypes.Count ];
			StringBuilder[] jsOnLoads = new StringBuilder[ PageTypes.Count ];

			foreach (var style in Target.StylesWithInheritance)
				{
				if (style.Links != null)
					{
					foreach (var link in style.Links)
						{
						string extension = link.File.Extension.ToLower();

						if (extension == "js" || extension == "json")
							{
							if (jsLinks[(int)link.Type] == null)
								{  jsLinks[(int)link.Type] = new StringBuilder();  }
							else
								{  jsLinks[(int)link.Type].Append(", ");  }

							Path relativeLinkPath = style.MakeRelative(link.File);
							Path outputPath = Paths.Style.OutputFile(Target.OutputFolder, style.Name, relativeLinkPath);
							Path relativeOutputPath = outputPath.MakeRelativeTo(Paths.Style.OutputFolder(Target.OutputFolder));
							jsLinks[(int)link.Type].Append("\"" + relativeOutputPath.ToURL() + "\"");
							}
						}
					}

				if (style.OnLoad != null)
					{
					foreach (var onLoadStatement in style.OnLoad)
						{
						StringBuilder onLoadStatementsForType = jsOnLoads[(int)onLoadStatement.Type];

						if (onLoadStatementsForType == null)
							{  
							onLoadStatementsForType = new StringBuilder();
							jsOnLoads[(int)onLoadStatement.Type] = onLoadStatementsForType;  
							}

						onLoadStatementsForType.Append("      ");
						onLoadStatementsForType.Append(onLoadStatement.Statement);

						if (onLoadStatementsForType[ onLoadStatementsForType.Length - 1 ] != ';')
							{  onLoadStatementsForType.Append(';');  }

						onLoadStatementsForType.Append('\n');
						}
					}
				}

			StringBuilder jsOutput = new System.Text.StringBuilder(
				"\"use strict\";\n" +
				"\n" +
				"var NDLoader = new function ()\n" +
				"   {\n");
				
			for (int i = 0; i < PageTypes.Count; i++)
				{
				jsOutput.Append("   this.JSLinks_" + PageTypes.AllNames[i] + " = [ ");

				if (jsLinks[i] != null)
					{  jsOutput.Append( jsLinks[i].ToString() );  }

				jsOutput.Append(" ];\n");
				}


			jsOutput.Append(
				"\n" +
				"   this.LoadJS = function (pageType, relativePrefix)\n" +
				"      {\n" +
				"      this.LoadJSArray(this.JSLinks_All, relativePrefix);\n" +
				"      this.LoadJSArray(this['JSLinks_' + pageType], relativePrefix);\n" +
				"      };\n" +
				"\n" +
				"   this.LoadJSArray = function (links, relativePrefix)\n" +
				"      {\n" +

					// WebKit, and I'm guessing KHTML just to be safe, doesn't import scripts included the other way in time
					// for their functions to be accessible to body.OnLoad().

				"      if (navigator.userAgent.indexOf('KHTML') != -1)\n" +
				"         {\n" +
				"         for (var i = 0; i < links.length; i++)\n" +
				"            {\n" +
				"            document.write('<script type=\"text/javascript\" src=\"' + relativePrefix + links[i] + '\"></script>');\n" +
				"            }\n" +
				"         }\n" +

					// The proper way.

				"      else\n" +
				"         {\n" +
				"         var head = document.getElementsByTagName('head')[0];\n" +
				"         \n" +
				"         for (var i = 0; i < links.length; i++)\n" +
				"            {\n" +
				"            var script = document.createElement('script');\n" +
				"            script.src = relativePrefix + links[i];\n" +
				"            script.type = 'text/javascript';\n" +
				"            \n" +
				"            head.appendChild(script);\n" +
				"            }\n" +
				"         }\n" +
				"      };\n" +
				"\n" +
				"   this.OnLoad = function (pageType)\n" +
				"      {\n" +
				"      this.OnLoad_All();\n" +
				"      this['OnLoad_' + pageType]();\n" +
				"      };\n");


			for (int i = 0; i < PageTypes.Count; i++)
				{
				jsOutput.Append(
				"\n" +
				"   this.OnLoad_" + PageTypes.AllNames[i] + " = function ()\n" +
				"      {\n");

				if (jsOnLoads[i] != null)
					{  jsOutput.Append( jsOnLoads[i].ToString() );  }

				jsOutput.Append(
				"      };\n");
				}

			jsOutput.Append(
				"   };\n");

			string jsOutputString = jsOutput.ToString();

			if (EngineInstance.Config.ShrinkFiles)
				{
				ResourceProcessors.JavaScript jsProcessor = new ResourceProcessors.JavaScript();
				jsOutputString = jsProcessor.Process(jsOutputString, true);
				}

			HTML.Component.WriteTextFile(Paths.Style.OutputFolder(Target.OutputFolder) + "/main.js", jsOutputString);
			}


		protected void BuildStyleFile (int fileID, CancelDelegate cancelled)
			{
			File file = EngineInstance.Files.FromID(fileID);
			Path outputFile = null;
			
			foreach (var style in Target.StylesWithInheritance)
				{
				if (style.Contains(file.FileName))
					{
					Path relativeStylePath = style.MakeRelative(file.FileName);
					outputFile = Paths.Style.OutputFile(Target.OutputFolder, style.Name, relativeStylePath);

					break;
					}
				}

			if (outputFile == null)
				{  return;  }

			if (file.Deleted)
				{  DeleteOutputFileIfExists(outputFile);  }
			else // file new or changed
				{
				// Creates all subdirectories needed.  Does nothing if it already exists.
				System.IO.Directory.CreateDirectory(outputFile.ParentFolder);

				string extension = outputFile.Extension.ToLower();

				if (extension == "js" || extension == "json")
					{
					ResourceProcessors.JavaScript jsProcessor = new ResourceProcessors.JavaScript();
					string output = jsProcessor.Process(System.IO.File.ReadAllText(file.FileName), EngineInstance.Config.ShrinkFiles);
					HTML.Component.WriteTextFile(outputFile, output);
					}
				else if (extension == "css")
					{
					ResourceProcessors.CSS cssProcessor = new ResourceProcessors.CSS();
					string output = cssProcessor.Process(System.IO.File.ReadAllText(file.FileName), EngineInstance.Config.ShrinkFiles);
					HTML.Component.WriteTextFile(outputFile, output);
					}
				else
					{
					System.IO.File.Copy(file.FileName, outputFile, true);  
					}
				}
			}


		/* Function: DeleteOutputFileIfExists
		 * If the passed file exists, deletes it and adds its parent folder to <UnprocessedChanges.FoldersToCheckForDeletion>.
		 * It's okay for the output path to be null.
		 */
		public void DeleteOutputFileIfExists (Path outputFile)
			{
			if (outputFile != null && System.IO.File.Exists(outputFile))
				{  
				System.IO.File.Delete(outputFile);
				Target.UnprocessedChanges.AddPossiblyEmptyFolder(outputFile.ParentFolder);
				}
			}


		/* Function: DeleteEmptyFolders
		 * Deletes the passed folder if it's empty.  If so it also tries the parent folder, continuing up the tree until it finds a
		 * non-empty folder or reaches the root output folder.
		 */
		protected void DeleteEmptyFolders (Path folder)
			{
			while (Target.OutputFolder.Contains(folder))
				{
				try
					{
					// If the folder isn't empty this will throw an exception.  We have to rely on that because .NET doesn't otherwise
					// provide an efficient way to detect if a folder is empty.  All its functions enumerate all its contents and return it
					// as an array; there's nothing that returns the first item and lets you stop there.
					System.IO.Directory.Delete(folder);
					}
				catch (Exception e)
					{
					if (e is System.IO.IOException || e is System.IO.DirectoryNotFoundException)
						{  break;  }
					else
						{  throw;  }
					}

				folder = folder.ParentFolder;
				}
			}



		// Group: Properties
		// __________________________________________________________________________


		/* Property: Target
		 * The <HTML.Target> associated with this builder.
		 */
		new public HTML.Target Target
			{
			get
				{  return (HTML.Target)target;  }
			}

		

		// Group: Variables
		// __________________________________________________________________________
		

		/* var: workInProgress
		 * 
		 * A numeric score of work currently in progress generated from the <UnprocessedChanges.Cost Constants>.
		 * 
		 * Thread Safety:
		 * 
		 *		You must hold <accessLock> in order to use this variable.
		 */
		protected long workInProgress;


		/* var: accessLock
		 * 
		 * An object used for a monitor that prevents more than one thread from accessing any of the variables at a
		 * time.
		 */
		protected object accessLock;



		// Group: Constants
		// __________________________________________________________________________


		/* Constants: Cost Constants
		 * 
		 * The values to use for each element when calculating a number for <GetStatus()> to return.  These are very rough 
		 * estimates, but they allow the more difficult to build files to weigh on the status more than the easier ones.
		 * 
		 *		SourceFileCost - How much building a single source file costs.
		 *		ClassCost - How much building a single class file costs.
		 *		ImageFileCost - How much building a single image file costs.
		 *		UnchangedImageFileUseCheckCost - How much checking if an image file is used and rebuilding it if that's 
		 *															  changed costs.
		 *		StyleFileCost - How much building a single style file costs.
		 *		MainStyleFilesCost - How much building the main style files costs.
		 *		SearchPrefixCost - How much building a single search prefix costs.
		 *		MainSearchFilesCost - How much building the main search files costs.
		 *		FramePageCost - How much building the frame page costs.
		 *		MenuCost - How much building the menu costs.
		 *		PossiblyEmptyFolderCost - How much checking a single folder for files costs.
		 */
		public const long SourceFileCost = 10;
		public const long ClassCost = 10;
		public const long ImageFileCost = 2;
		public const long UnchangedImageFileUseCheckCost = 1;
		public const long StyleFileCost = 2;
		public const long MainStyleFilesCost = 1;
		public const long SearchPrefixCost = 4;
		public const long MainSearchFilesCost = 1;
		public const long FramePageCost = 1;
		public const long MenuCost = 15;
		public const long PossiblyEmptyFolderCost = 1;

		}
	}