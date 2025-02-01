
// This file is part of Natural Docs, which is Copyright © 2003-2025 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace CodeClear.NaturalDocs.Engine.Links
	{

	/* Enum: LinkType
	 *
	 * The type of link it is.
	 *
	 * NaturalDocs - When someone writes "<Class.Member>" in a Natural Docs comment.
	 * ClassParent - When a class defines a parent, such as "class Child : ParentClass".
	 * Type - For linking types appearing in function and variable prototypes back to their definition.
	 */
	public enum LinkType : byte
		{
		NaturalDocs = 1,
		ClassParent = 2,
		Type = 3
		}

	}
