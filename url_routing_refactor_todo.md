# URL Routing Refactor Todo

## Task: Remove Event actions Create, Edit, Delete from URL path and use query parameters

### Analysis Complete ✅
- Current routing uses path-based actions: /Events/Create, /Events/Edit/EventName, /Events/Delete/EventName
- Views use Url.Action helpers for links
- Need to refactor to query parameter approach

### Implementation Steps:
- [x] Modify EventsController to handle all actions via query parameters in Index action
- [x] Remove separate Create, Edit, Delete action methods
- [x] Update Index.cshtml to use new query parameter URLs
- [x] Update Details.cshtml to use new query parameter URLs
- [x] Test the implementation

### New URL Structure:
- /Events (shows event list)
- /Events?action=create (show create form)
- /Events?action=edit&id=EventName (show edit form)
- /Events?action=delete&id=EventName (execute delete and redirect)

### Current URL Structure:
- /Events (Index)
- /Events/Create (Create)
- /Events/Edit/EventName (Edit)
- /Events/Delete/EventName (Delete)

### Controller Changes ✅
- Modified Index action to handle action and id query parameters
- Added HandleCreate, HandleEdit, HandleDelete private methods
- Preserved Details action with clean path-based URL
- All form submissions now POST to Index with action parameter

### View Changes ✅
- Updated Index.cshtml create links to use ?action=create
- Updated Details.cshtml edit links to use ?action=edit&id=EventName
- Updated Details.cshtml delete form to POST to ?action=delete&id=EventName
- Preserved all other functionality and styling

### Testing Results ✅
- Build completed successfully with no warnings or errors
- Application runs successfully on http://localhost:5261
- New routing structure is working correctly
