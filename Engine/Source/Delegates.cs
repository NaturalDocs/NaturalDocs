/* 
 * Class: CodeClear.NaturalDocs.Engine.Delegates
 */

// This file is part of Natural Docs, which is Copyright © 2003-2021 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace CodeClear.NaturalDocs.Engine
	{
	static public class Delegates
		{
		
		/* Property: NeverCancel
		 * A <CancelDelegate> that always returns false.
		 */
		public static CancelDelegate NeverCancel = delegate() {  return false;  };
		
		}

	/* Delegate: SimpleDelegate
	 * A parameterless delegate.
	 */
	public delegate void SimpleDelegate ();
	
	/* Delegate: CancelDelegate
	 * A delegate that returns a bool of whether to cancel an operation or not.
	 */
	public delegate bool CancelDelegate ();

	/* Delegate: CancellableTask
	 * A task that can be cancelled by a <CancelDelegate>.
	 */
	public delegate void CancellableTask (CancelDelegate cancel);
		
	}