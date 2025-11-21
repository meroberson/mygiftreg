# Stack Overflow Debug TODO

## Investigation and Fix Tasks
- [x] Examine EventsWebController.Index() method
- [x] Examine EventService.GetAllEventsAsync() method
- [x] Examine EventRepository.GetAllAsync() method
- [x] Identify circular dependency or infinite recursion source
- [x] Fix the recursion issue in the problematic code
- [ ] Test the fix by running the application
- [ ] Verify no more stack overflow occurs

## Technical Analysis
- [x] Check for circular dependency injection
- [x] Verify repository pattern implementation
- [x] Ensure async/await patterns are correct
- [x] Review service layer dependencies

## Root Cause Found
**PROBLEM**: In EventRepository.GetAllAsync() method:
```csharp
public async Task<IList<Event>> GetAllAsync()
{
    var allEvents = await GetAllAsync();  // <-- INFINITE RECURSION!
    return allEvents.Where(e => e.PartitionKey == "").ToList();
}
```
The method calls itself instead of calling the base class method `base.GetAllAsync()`.
