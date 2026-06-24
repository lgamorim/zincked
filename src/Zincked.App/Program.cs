using System.IO.Abstractions;
using Zincked.App;
using Zincked.Core;

var fileSystem = new FileSystem();
var synchronizer = new FolderSynchronizer(fileSystem);
var application = new Application(synchronizer, fileSystem, Console.Out, Console.Error);

return application.Run(args);
