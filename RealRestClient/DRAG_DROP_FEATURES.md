# Drag & Drop and Renaming Features

This document explains the new file management features added to the Real Rest Client TreeView.

## Features Added

### 1. Drag and Drop File Moving
- **What it does**: Allows you to move request files between different collections (folders) by dragging them in the TreeView.
- **How to use**: 
  1. Click and hold on any request file (not a folder) in the TreeView
  2. Drag it over a folder/collection where you want to move it
  3. Release the mouse button to drop the file
  4. The file will be moved to the target collection
  5. The TreeView will automatically refresh to show the updated structure

### 2. F2 Renaming
- **What it does**: Allows you to rename both files and folders using the F2 key.
- **How to use**:
  1. Select any item in the TreeView (file or folder)
  2. Press F2 to start editing
  3. Type the new name
  4. Press Enter to save or Escape to cancel
  5. The item will be renamed on disk and the TreeView will refresh

## Implementation Details

### Files Modified:
1. **`Services/RequestsManager.cs`** - Added methods:
   - `MoveFile()` - Moves a file from one collection to another
   - `RenameFile()` - Renames a request file
   - `RenameCollection()` - Renames a collection folder

2. **`ViewModels/Node.cs`** - Added properties:
   - `IsEditing` - Tracks if the node is in edit mode
   - `EditingText` - Holds the text being edited
   - `StartEditing()` and `StopEditing()` methods

3. **`Views/HttpView.axaml`** - Enhanced TreeView template:
   - Added conditional TextBox for editing
   - Improved visual styling

4. **`Views/HttpView.axaml.cs`** - Added event handlers:
   - Drag and drop event handling
   - F2 key handling for rename
   - Visual tree helper methods

### Collision Handling:
- **File moves**: If a file with the same name exists in the target folder, a number suffix is automatically added (e.g., `request_1.json`)
- **Renaming**: If a file/folder with the new name already exists, the rename operation is cancelled

### Error Handling:
- File operations are wrapped in try-catch blocks
- Failed operations will not crash the application
- The TreeView will refresh only on successful operations

## Future Enhancements
- Visual feedback during drag operations (drag preview, drop zones)
- Context menu support for right-click operations
- Undo/redo functionality
- Bulk operations (multi-select)
- Copy operations in addition to move operations
