/*
 * Class: CodeClear.NaturalDocs.Engine.Output.HTML.Target
 * ____________________________________________________________________________
 *
 * A HTML output target.
 *
 *
 * Multithreading: Thread Safety Notes
 *
 *		> Accessor's database Lock -> accessLock
 *
 *		Externally, this class is thread safe as functions use <accessLock> to control access to internal variables.
 *
 *		Interally, if code needs both a database lock and <accessLock> it must acquire the database lock first.  It also
 *		must not upgrade the database lock from read/possible write to read/write while holding <accessLock>, as there
 *		may be a thread with a read-only accessor waiting for <accessLock>.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2025 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using CodeClear.NaturalDocs.Engine.Styles;


namespace CodeClear.NaturalDocs.Engine.Output.HTML
	{
	public partial class Target : Output.Target, CodeDB.IChangeWatcher, Files.IChangeWatcher, SearchIndex.IChangeWatcher,
											IStartupWatcher, IDisposable
		{

		// Group: Functions
		// __________________________________________________________________________


		public Target (Output.Manager manager, Config.Targets.HTMLOutputFolder config) : base (manager)
			{
			accessLock = new object();

			buildState = null;
			unprocessedChanges = null;

			this.config = config;
			style = null;
			stylesWithInheritance = null;
			searchIndex = null;
			}


		/* Function: Dispose
		 */
		override protected void Dispose (bool strictRulesApply)
			{
			if (!strictRulesApply)
				{
				try
					{
					if (searchIndex != null)
						{  searchIndex.Dispose();  }

					if (buildState != null && started)
						{
						ConfigFiles.BinaryBuildStateParser buildStateParser = new ConfigFiles.BinaryBuildStateParser();
						buildStateParser.Save(WorkingDataFolder + "/BuildState.nd", buildState, unprocessedChanges);
						}
					}
				catch
					{  }
				}
			}


		/* Function: CreateBuilderProcess
		 * Creates a <TargetBuilder> capable of building the output for this target.
		 */
		override public Output.TargetBuilder CreateBuilderProcess ()
			{
			return new HTML.TargetBuilder(this);
			}


		/* Function: GetStatus
		 * Returns a numeric value representing the total changes yet to be processed.  It is the sum of everything in
		 * this class weighted by the <TargetBuilder.Cost Constants> which estimate how hard they are to perform.  The value
		 * of the total is meaningless other than to track progress as it works its way towards zero.
		 */
		override public void GetStatus (out long workRemaining)
			{
			unprocessedChanges.GetStatus(out workRemaining);
			}


		/* Function: MakeRelativeURL
		 * Creates a relative URL between the two absolute filesystem paths.  Make sure the From parameter is a *file* and not
		 * a folder.
		 */
		public string MakeRelativeURL (Path fromFile, Path toFile)
			{
			return toFile.MakeRelativeTo(fromFile.ParentFolder).ToURL();
			}



		// Group: Purging Functions
		// __________________________________________________________________________


		/* Function: PurgeFolder
		 * Deletes an output folder and all its contents if it exists.  Pass a reference to a bool that tracks whether we've registered a
		 * PossiblyLongOperation for purging with <Engine.Instance>.  If it's set to false and there's a folder to purge it will call
		 * <Engine.Instance.StartPossiblyLongOperation()> and set it to true.  Call <FinishedPurging()> after all purging calls to end
		 * it.
		 */
		protected void PurgeFolder (Path folder, ref bool inPurgingOperation)
			{
			if (System.IO.Directory.Exists(folder))
				{
				if (!inPurgingOperation)
					{
					EngineInstance.StartPossiblyLongOperation("PurgingOutputFiles");
					inPurgingOperation = true;
					}

				try
					{  System.IO.Directory.Delete(folder, true);  }
				catch (Exception e)
					{
					if (!(e is System.IO.IOException || e is System.IO.DirectoryNotFoundException))
						{  throw;  }
					}
				}
			}


		/* Function: PurgeStyleFolder
		 * Deletes an output folder for a style and all its contents if it exists.  Pass a reference to a bool that tracks whether we've
		 * registered a PossiblyLongOperation for purging with <Engine.Instance>.  Call <FinishedPurging()> after all purging calls.
		 */
		protected void PurgeStyleFolder (string styleName, ref bool inPurgingOperation)
			{
			PurgeFolder( Paths.Style.OutputFolder(this.OutputFolder, styleName), ref inPurgingOperation );
			}


		/* Function: PurgeAllStyleFolders
		 * Deletes all the style output folders and their contents.  Pass a reference to a bool that tracks whether we've registered a
		 * PossiblyLongOperation for purging with <Engine.Instance>.  Call <FinishedPurging()> after all purging calls.
		 */
		protected void PurgeAllStyleFolders (ref bool inPurgingOperation)
			{
			PurgeFolder( Paths.Style.OutputFolder(this.OutputFolder), ref inPurgingOperation );
			}


		/* Function: PurgeAllSourceAndImageFolders
		 * Deletes all the source and image output folders and their contents.  Pass a reference to a bool that tracks whether
		 * we've registered a PossiblyLongOperation for purging with <Engine.Instance>.  Call <FinishedPurging()> after all
		 * purging calls.
		 */
		protected void PurgeAllSourceAndImageFolders (ref bool inPurgingOperation)
			{
			string[] outputFolders = System.IO.Directory.GetDirectories(OutputFolder);

			foreach (string outputFolder in outputFolders)
				{
				if (IsSourceOrImageOutputFolderRegex().IsMatch(outputFolder))
					{
					PurgeFolder(outputFolder, ref inPurgingOperation);
					}
				}
			}


		/* Function: PurgeAllClassFolders
		 * Deletes all the class output folders and their contents.  Pass a reference to a bool that tracks whether we've registered a
		 * PossiblyLongOperation for purging with <Engine.Instance>.  Call <FinishedPurging()> after all purging calls.
		 */
		protected void PurgeAllClassFolders (ref bool inPurgingOperation)
			{
			foreach (var hierarchy in EngineInstance.Hierarchies.AllHierarchies)
				{
				PurgeFolder( Paths.Class.OutputFolder(this.OutputFolder, hierarchy), ref inPurgingOperation );
				}
			}


		/* Function: PurgeAllMenuFolders
		 * Deletes all the menu output folders and their contents.  Pass a reference to a bool that tracks whether we've registered a
		 * PossiblyLongOperation for purging with <Engine.Instance>.  Call <FinishedPurging()> after all purging calls.
		 */
		protected void PurgeAllMenuFolders (ref bool inPurgingOperation)
			{
			PurgeFolder( Paths.Menu.OutputFolder(this.OutputFolder), ref inPurgingOperation );
			}


		/* Function: PurgeAllSearchIndexFolders
		 * Deletes all the search index output folders and their contents.  Pass a reference to a bool that tracks whether we've registered
		 * a PossiblyLongOperation for purging with <Engine.Instance>.  Call <FinishedPurging()> after all purging calls.
		 */
		protected void PurgeAllSearchIndexFolders (ref bool inPurgingOperation)
			{
			PurgeFolder( Paths.SearchIndex.OutputFolder(this.OutputFolder), ref inPurgingOperation );
			}


		/* Function: FinishedPurging
		 * Call this after you're done calling all the other purging functions.  Pass the reference to the bool that tracked whether
		 * we've registered a PossiblyLongOperation for purging with <Engine.Instance>.  If it's set to true it will call
		 * <Engine.Instance.EndPossiblyLongOperation()> and set it to false.
		 */
		protected void FinishedPurging (ref bool inPurgingOperation)
			{
			if (inPurgingOperation)
				{
				EngineInstance.EndPossiblyLongOperation();
				inPurgingOperation = false;
				}
			}



		// Group: Properties
		// __________________________________________________________________________


		/* Property: BuildState
		 */
		public BuildState BuildState
			{
			get
				{  return buildState;  }
			}


		/* Property: UnprocessedChanges
		 */
		public UnprocessedChanges UnprocessedChanges
			{
			get
				{  return unprocessedChanges;  }
			}


		/* Property: OutputFolder
		 * The root output folder of the entire build target.
		 */
		public Path OutputFolder
			{
			get
				{  return config.Folder;  }
			}


		/* Property: WorkingDataFolder
		 * The working data folder specifically for this build target.
		 */
		public Path WorkingDataFolder
			{
			get
				{  return EngineInstance.Config.OutputWorkingDataFolderOf(config.Number);  }
			}


		/* Property: Config
		 */
		public Config.Targets.HTMLOutputFolder Config
			{
			get
				{  return config;  }
			}


		/* Property: Style
		 * The <Style> that applies to this target, or null if none.
		 */
		override public Style Style
			{
			get
				{  return style;  }
			}


		/* Property: StylesWithInheritance
		 * A list which includes <Style> and all its inherited members in the order in which they should be applied.
		 */
		public List<Style> StylesWithInheritance
			{
			get
				{  return stylesWithInheritance;  }
			}


		/* Property: SearchIndex
		 * The <SearchIndex.Manager> associated with this build target.
		 */
		public SearchIndex.Manager SearchIndex
			{
			get
				{  return searchIndex;  }
			}



		// Group: Variables
		// __________________________________________________________________________


		/* var: accessLock
		 * A monitor used for accessing any of the variables in this class.
		 */
		protected object accessLock;

		/* var: buildState
		 * The current build state for the HTML target.
		 */
		protected BuildState buildState;

		/* var: unprocessedChanges
		 */
		protected UnprocessedChanges unprocessedChanges;

		/* var: config
		 */
		protected Config.Targets.HTMLOutputFolder config;

		/* var: style
		 * The <Style> which applies to this output target.
		 */
		protected Style style;

		/* var: stylesWithInheritance
		 * A list which includes <style> and all its inherited members in the order in which they should be applied.
		 */
		protected List<Style> stylesWithInheritance;

		/* var: searchIndex
		 * The <SearchIndex.Manager> for this output target.
		 */
		protected SearchIndex.Manager searchIndex;



		// Group: Regular Expressions
		// __________________________________________________________________________


		/* Regex: IsSourceOrImageOutputFolderRegex
		 * Will match if the string appears to be the path of a source or image output folder, such as one ending with "/images2".
		 * You can use it with a complete path.
		 */
		[GeneratedRegex("""(?:^|[/\\])(?:files|images)[0-9]{0,9}$""",
								  RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
		static private partial Regex IsSourceOrImageOutputFolderRegex();

		}

	}
