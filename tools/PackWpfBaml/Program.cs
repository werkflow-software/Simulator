using System.Resources;

if (args.Length < 2)
{
    Console.Error.WriteLine("Usage: PackWpfBaml <output.g.resources> <projectDirectory>");
    return 1;
}

var outputPath = args[0];
var projectDir = args[1];

var bamlRelativePaths = new[]
{
    "app.baml",
    "mainwindow.baml",
    "Styles/appstyles.baml",
    "Views/eventsview.baml",
    "Views/faultscenariosview.baml",
    "Views/jobsview.baml",
    "Views/logview.baml",
    "Views/machinesview.baml",
    "Views/manualcontrolwindow.baml",
    "Views/nodesview.baml",
    "Views/overviewview.baml",
    "Views/physicalsignalsview.baml",
    "Views/settingsview.baml",
};

Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

using var writer = new ResourceWriter(outputPath);
foreach (var relativePath in bamlRelativePaths)
{
    var fullPath = Path.Combine(projectDir, relativePath);
    if (!File.Exists(fullPath))
    {
        Console.Error.WriteLine($"Missing BAML file: {fullPath}");
        return 1;
    }

    var resourceKey = relativePath.Replace('\\', '/').ToLowerInvariant();
    writer.AddResource(resourceKey, new MemoryStream(File.ReadAllBytes(fullPath)));
}

writer.Generate();
Console.WriteLine($"Packed {bamlRelativePaths.Length} BAML files into {outputPath}");
return 0;
