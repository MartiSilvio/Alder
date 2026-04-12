await Task.CompletedTask;
var cts = new System.Threading.CancellationTokenSource();
cts.Cancel();
return cts.Token.IsCancellationRequested;
