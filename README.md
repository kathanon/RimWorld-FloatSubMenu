# FloatSubMenu
Adds support for having sub-menus in float menus in RimWorld.

This is intended as a library mod, allowing any mod developer to add sub-menus to their float menus.

# Usage
Add an instance of the `FloatSubMenu` class in the list of `FloatMenuOption`s when creating a `FloatMenu`, and that option will open up a sub-menu when hovered or clicked. They can also be nested, by adding it to the list of options for another `FloatSubMenu`.

# Compatibility
Currently does not play nice with Vanilla UI Expanded, but I am working on adressing that.
