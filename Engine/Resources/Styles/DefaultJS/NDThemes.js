/*
	Include in output:

	This file is part of Natural Docs, which is Copyright © 2003-2022 Code Clear LLC.
	Natural Docs is licensed under version 3 of the GNU Affero General Public
	License (AGPL).  Refer to License.txt or www.naturaldocs.org for the
	complete details.

	This file may be distributed with documentation files generated by Natural Docs.
	Such documentation is not covered by Natural Docs' copyright and licensing,
	and may have its own copyright and distribution terms as decided by its author.

*/

"use strict";


// Theme Members

	$Theme_Name = 0;
	$Theme_ID = 1;

// Keycodes

	$KeyCode_Escape = 27;




/* Class: NDThemes
	_____________________________________________________________________________

    A class to manage and apply the available themes.

*/
var NDThemes = new function ()
	{

	// Group: Functions
	// ________________________________________________________________________


	/* Function: Apply
		Applies the passed theme ID, which includes changing the CSS class of the root html element.
	*/
	this.Apply = function (themeID)
		{

		var newThemeID = themeID;
		var newEffectiveThemeID = themeID;
		var currentThemeID = this.userSelectedThemeID;
		var currentEffectiveThemeID = this.effectiveThemeID;

		
		// Determine if the effective theme ID is different

//		if (theme.StartsWith("Auto-"))
//			{
//			var systemTheme = this.GetSystemTheme();
//
//			if (systemTheme == "Light")
//				{  this.effectiveTheme = "Light";  }
//			else
//				{
//				if (theme == "Auto-Light/Dark")
//					{  this.effectiveTheme = "Dark";  }
//				else
//					{  this.effectiveTheme = "Black";  }
//				}
//			}

		
		// Replace the CSS class

		if (newEffectiveThemeID != currentEffectiveThemeID)
			{
			if (currentEffectiveThemeID != undefined)
				{  document.documentElement.classList.remove(currentEffectiveThemeID + "Theme");  }

			// Note that the embedded script in each page may have already set the CSS class early.  That should be fine 
			// and this line will have no effect if that's the case.
			if (newEffectiveThemeID != undefined)
				{  document.documentElement.classList.add(newEffectiveThemeID + "Theme");  }
			}


		// Update the theme state

		this.userSelectedThemeID = newThemeID;
		this.effectiveThemeID = newEffectiveThemeID;
		};

	
	/* Function: SetThemes

		Sets the list of themes the documentation supports.  The parameter is an array of themes, and each theme entry
		is itself an array, the first value being its display name and the second value its ID.
		
		The ID should only contain characters that are valid for CSS class names.  When applied, it will be added to the
		root html element as a CSS class with "Theme" appended, so "Light" would be added as "LightTheme".

		Example:

			--- Code ---
			NDThemes.Set([
			   [ "Light Theme", "Light" ],
			   [ "Dark Theme", "Dark" ],
			   [ "Black Theme", "Black" ]
			]);
			--------------
	*/
	this.SetThemes = function (themes)
		{
		this.availableThemes = themes;
		};




	// Group: Support Functions
	// ________________________________________________________________________


	/* Function: GetSystemTheme
		Returns the operating system theme as the string "Light" or "Dark".  It defaults to "Light" if this isn't supported.
	*/
	this.GetSystemTheme = function ()
		{
		if (window.matchMedia && 
			window.matchMedia('(prefers-color-scheme: dark)').matches)
			{  return "Dark";  }
		else
			{  return "Light";  }
		};

	
	/* Function: AddSystemThemeChangeWatcher
		Sets a function to be called whenever the system theme changes.  The function will receive one parameter, the
		string "Light" or "Dark".
	*/
	this.AddSystemThemeChangeWatcher = function (changeWatcher)
		{
		if (window.matchMedia)
			{
			window.matchMedia('(prefers-color-scheme: dark)').addEventListener(
				'change',
				function (event)
					{
					var theme = event.matches ? "Dark" : "Light";
					changeWatcher(theme);
					}
				);
			}
		};

	
	
	// Group: Variables
	// ________________________________________________________________________


	/* var: availableThemes

		An array of all the themes the documentation supports.  Each theme entry is itself an array, with the first
		value being its display name and the second value its ID.  The array will be undefined if none have been set.
		
		The ID should only contain characters that are valid for CSS class names.  When applied, it will be added to the
		root html element as a CSS class with "Theme" appended, so "Light" would be added as "LightTheme".

		Example:

			--- Code ---
			[
			   [ "Light Theme", "Light" ],
			   [ "Dark Theme", "Dark" ],
			   [ "Black Theme", "Black" ]
			]
			-------------
		*/
	// this.availableThemes = undefined;


	/* var: userSelectedThemeID
		The ID of the user-selected theme, which includes the auto values.  It will be undefined if one hasn't been set.
	*/
	// this.userSelectedThemeID = undefined;

	
	/* var: effectiveThemeID
		The ID of the effective theme, which translates any auto values from <userSelectedThemeID> to their result based 
		on the system theme.  Otherwise it will be the same as <userSelectedThemeID>.  It will be undefined if a theme 
		hasn't been set.
	*/
	// this.effectiveThemeID = undefined;

	};



/* _____________________________________________________________________________

	Class: NDThemeSwitcher
	_____________________________________________________________________________

    A class to manage the HTML theme switcher, which allows the end user to choose their own theme at runtime.

*/
var NDThemeSwitcher = new function ()
	{

	// Group: Functions
	// ________________________________________________________________________


	/* Function: Start

		Sets up the theme switcher.  It expects there to be an empty HTML element with ID NDThemeSwitcher
		in the document.  You can pass a function to it to be called whenever the theme changes.
	 */
	this.Start = function (onThemeChange)
		{

		// Create event handlers

		this.switcherClickEventHandler = NDThemeSwitcher.OnSwitcherClick.bind(NDThemeSwitcher);
		this.keyDownEventHandler = NDThemeSwitcher.OnKeyDown.bind(NDThemeSwitcher);


		// Add the passed external event handler

		this.onThemeChange = onThemeChange;


		// Prepare the switcher HTML

		this.domSwitcher = document.getElementById("NDThemeSwitcher");

		var domSwitcherLink = document.createElement("a");
		domSwitcherLink.addEventListener("click", this.switcherClickEventHandler);

		this.domSwitcher.appendChild(domSwitcherLink);


		// Prepare the pop-up menu holder HTML

		this.domMenu = document.createElement("div");
		this.domMenu.id = "NDThemeSwitcherMenu";
		this.domMenu.style.display = "none";
		this.domMenu.style.position = "fixed";

		document.body.appendChild(this.domMenu);
		};


	/* Function: IsNeeded
		Returns whether the theme switcher is necessary.  This will only return true if there are multiple themes
		defined in <NDThemes>.
	*/
	this.IsNeeded = function ()
		{
		return (NDThemes.availableThemes != undefined &&
				   NDThemes.availableThemes.length > 1);
		};


	/* Function: UpdateVisibility

		Creates or hides the theme switcher HTML elements depending on the results of <IsNeeded()>.  It returns whether
		the visibility changed.
		
		This should be called once to create it while setting up the page and again whenever the list of themes in <NDThemes>
		changes.
	*/
	this.UpdateVisibility = function ()
		{
		var themeSwitcher = document.getElementById("NDThemeSwitcher");

		var wasVisible = (themeSwitcher.style.display == "block");
		var shouldBeVisible = this.IsNeeded();

		if (!wasVisible && shouldBeVisible)
			{
			themeSwitcher.style.display = "block";
			return true;
			}
		else if (wasVisible && !shouldBeVisible)
			{
			themeSwitcher.style.display = "none";
			return true;
			}
		else
			{  return false;  }
		};



	// Group: Menu Functions
	// ________________________________________________________________________


	/* Function: OpenMenu
		Creates the pop-up menu, positions it, and makes it visible.
	*/
	this.OpenMenu = function ()
		{
		if (!this.MenuIsOpen())
			{
			this.BuildMenu();

			this.domMenu.style.visibility = "hidden";
			this.domMenu.style.display = "block";
			this.PositionMenu();
			this.domMenu.style.visibility = "visible";

			this.domSwitcher.classList.add("Active");

			window.addEventListener("keydown", this.keyDownEventHandler);
			}
		};


	/* Function: CloseMenu
		Closes the pop-up menu if it was visible.
	*/
	this.CloseMenu = function ()
		{
		if (this.MenuIsOpen())
			{  
			this.domMenu.style.display = "none";
			this.domSwitcher.classList.remove("Active");

			window.removeEventListener("keydown", this.keyDownEventHandler);
			}
		};


	/* Function: MenuIsOpen
	*/
	this.MenuIsOpen = function ()
		{
		return (this.domMenu != undefined && this.domMenu.style.display == "block");
		};



	// Group: Menu Support Functions
	// ________________________________________________________________________


	/* Function: BuildMenu
		Creates the HTML pop-up menu from <NDThemes.availableThemes> and applies it to <domMenu>.  It does not
		affect its visibility or position.
	*/
	this.BuildMenu = function ()
		{
		var html = "";

		if (NDThemes.availableThemes != undefined)
			{
			for (var i = 0; i < NDThemes.availableThemes.length; i++)
				{
				var theme = NDThemes.availableThemes[i];

				html += "<a class=\"TSEntry TSEntry_" + theme[$Theme_ID] + "Theme\"";

				if (theme[$Theme_ID] == NDThemes.userSelectedThemeID)
					{  html += " id=\"TSSelectedEntry\"";  }

				html += " href=\"javascript:NDThemeSwitcher.OnMenuEntryClick('" + theme[$Theme_ID] + "');\">" +
					"<div class=\"TSEntryIcon\"></div>" +
					"<div class=\"TSEntryName\">" + theme[$Theme_Name] + "</div>" +
				"</a>";
				}
			}

		this.domMenu.innerHTML = html;
		};

	
	/* Function: PositionMenu
		Moves the pop-up menu into position relative to the button.
	*/
	this.PositionMenu = function ()
		{
		// First position it under the switcher

		var x = this.domSwitcher.offsetLeft;
		var y = this.domSwitcher.offsetTop + this.domSwitcher.offsetHeight + 5;


		// Now shift it over left enough so that the icons line up

		var entryIcons = this.domMenu.getElementsByClassName("TSEntryIcon");

		if (entryIcons != undefined && entryIcons.length >= 1)
			{
			var entryIcon = entryIcons[0];

			// offsetLeft is the icon offset relative to the parent menu, clientLeft is the menu's border width
			x -= entryIcon.offsetLeft + this.domMenu.clientLeft;
			}

		
		// Apply the position

		this.domMenu.style.left = x + "px";
		this.domMenu.style.top = y + "px";
		};



	// Group: Event Handlers
	// ________________________________________________________________________


	/* Function: OnSwitcherClick
	*/
	this.OnSwitcherClick = function (event)
		{
		if (this.MenuIsOpen())
			{  this.CloseMenu();  }
		else
			{  this.OpenMenu();  }

		event.preventDefault();
		};


	/* Function: OnMenuEntryClick
	*/
	this.OnMenuEntryClick = function (themeID)
		{
		if (themeID != NDThemes.userSelectedThemeID)
			{
			NDThemes.Apply(themeID);

			if (this.onThemeChange != undefined)
				{  this.onThemeChange();  }
			}

		this.CloseMenu();
		};


	/* Function: OnKeyDown
	*/
	this.OnKeyDown = function (event)
		{
		if (event.keyCode == $KeyCode_Escape)
			{
			if (this.MenuIsOpen())
				{  
				this.CloseMenu();
				event.preventDefault();
				}
			}
		};


	/* Function: OnUpdateLayout
	*/
	this.OnUpdateLayout = function ()
		{
		// Check for undefined because this may be called before Start().
		if (this.domMenu != undefined)
			{
			this.PositionMenu();
			}
		};



	// Group: Event Handler Variables
	// ________________________________________________________________________

	/* var: switcherClickEventHandler
		A bound function to call <OnSwitcherClick()> with NDThemeSwitcher always as "this".
	*/

	/* var: keyDownEventHandler
		A bound function to call <OnKeyDown()> with NDThemeSwitcher always as "this".
	*/


	// Group: Variables
	// ________________________________________________________________________


	/* var: domSwitcher
		The DOM element of the theme switcher.
	*/
	// var domSwitcher = undefined;

	/* var: domMenu
		The DOM element of the pop-up theme menu.
	*/
	// var domMenu = undefined;

	/* var: onThemeChange
		An event handler that will be called whenever the theme changes.
	*/
	// var onThemeChange = undefined;

	};