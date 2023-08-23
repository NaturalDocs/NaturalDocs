/*
 * Class: CodeClear.NaturalDocs.Engine.Errors.ErrorList
 * ____________________________________________________________________________
 *
 * A list of <Error> objects.
 *
 *
 * Topic: Usage
 *
 *		- Create the list and use <Add()> to put error messages on it as necessary.
 *
 *		- Use <Count> to determine if anything was added.
 *
 *		- If you want to add any errors as comments to text configuration files, call <ConfigFile.TryToAnnotateWithErrors()>.
 *		  If you're also presenting the errors a different way such as with console output you *must* call this beforehand
 *		  because it will change the line numbers.
 *
 *		- Use the various access functions and properties to pull out the data and report on it.
 *
 *
 * Multithreading: Not Thread Safe, Doesn't Support Reader/Writer
 *
 *		Since this class is only intended for engine initialization, it is not thread safe at all.  Also, it does NOT support the
 *		reader/writer model with an external lock because it maintains its internal sort on demand, and thus a read could
 *		possibly change the state of the list.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2023 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.IO;


namespace CodeClear.NaturalDocs.Engine.Errors
	{
	public class ErrorList : IEnumerable<Error>
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: ErrorList
		 */
		public ErrorList ()
			{
			list = new List<Error>();
			sorted = true;
			}


		/* Function: Add
		 * Adds an error occurring in a particular file to the list.
		 */
		public void Add (string message, Path file = default(Path), int lineNumber = 0, Config.PropertySource configSource = Config.PropertySource.NotDefined,
							  string property = null)
			{
			list.Add ( new Error(message, file, lineNumber, configSource, property) );
			sorted = false;
			}


		/* Function: Add
		 * Adds an error occurring in a particular file to the list.
		 */
		public void Add (string message, Config.PropertyLocation propertyLocation, string property = null)
			{
			Add(message, propertyLocation.FileName, propertyLocation.LineNumber, propertyLocation.Source, property);
			}


		/* Function: FromFile
		 * Returns a list of the <Errors> in a particular file.  If there are none it will return an empty list.
		 */
		public IList<Error> FromFile (Path file)
			{
			List<Error> result = new List<Error>();

			CheckSort();

			foreach (Error error in list)
				{
				if (error.File == file)
					{  result.Add(error);  }
				}

			return result;
			}



		// Group: Protected Functions
		// __________________________________________________________________________


		/* Function: CheckSort
		 * Makes sure the list is sorted.  It stores whether it has changed since the last sort so this can be called repeatedly
		 * without being expensive.
		 */
		protected void CheckSort ()
			{
			if (sorted == false)
				{
				list.Sort();
				sorted = true;
				}
			}




		// Group: Properties
		// __________________________________________________________________________


		/* Property: Count
		 * The number of errors on the list.
		 */
		public int Count
			{
			get
				{  return list.Count;  }
			}


		/* Property: this
		 * An index operator to access a particular error in the list.
		 */
		public Error this [int index]
			{
			get
				{
				CheckSort();
				return list[index];
				}
			}


		/* Function: ConfigFiles
		 * A list of all the text-based configuration files that contain errors.  If there are none it will return an empty list.
		 */
		public IList<Path> ConfigFiles
			{
			get
				{
				List<Path> result = new List<Path>();
				Path lastFile = null;

				CheckSort();

				foreach (Error error in list)
					{
					if (error.File != null && error.File != lastFile)
						{
						result.Add(error.File);
						lastFile = error.File;
						}
					}

				return result;
				}
			}




		// Group: IEnumerable Functions
		// __________________________________________________________________________


		IEnumerator<Error> IEnumerable<Error>.GetEnumerator ()
			{
			CheckSort();
			return list.GetEnumerator();
			}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator ()
			{
			CheckSort();
			return list.GetEnumerator();
			}



		// Group: Variables
		// __________________________________________________________________________


		/* Var: list
		 * The actual list of <Errors>.
		 */
		protected List<Error> list;

		/* Var: sorted
		 * Whether <list> is known to be sorted.
		 */
		protected bool sorted;

		}
	}
