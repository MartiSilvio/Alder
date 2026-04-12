// §15.15: non-generic Task.WhenAll awaits completion of all tasks
await Task.WhenAll(Task.CompletedTask, Task.CompletedTask);
return 42;
